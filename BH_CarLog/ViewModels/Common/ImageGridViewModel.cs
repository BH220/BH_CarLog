using System.Collections.ObjectModel;
using System.IO;
using BH_CarLog.Helpers;
using BH_CarLog.Api;
using BH_CarLog.Api.Manager;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BH_CarLog.Api.Interface;

namespace BH_CarLog.ViewModels.Common
{
    /// <summary>
    /// 첨부 이미지 목록 (기존 BhImageGrid). 표(구분/이름/가로/세로/크기)로 보여주고 추가/삭제/보기를 제공하며
    /// 삭제된 기존 이미지의 연결 키를 추적한다.
    /// 항목은 API 응답 모델(<see cref="ImageInfo"/> 파생: CarShopImageInfo / FuelImageInfo / MaintenanceImageInfo / NewMessageImageInfo)을 그대로 쓴다.
    /// 신규 첨부는 <see cref="ImageInfo.IsNew"/> 가 true 이고 <see cref="ImageInfo.data"/> 에 업로드할 바이너리가 담긴다.
    /// </summary>
    public partial class ImageGridViewModel<TImage> : ObservableObject where TImage : ImageInfo, new()
    {
        private const string ImageFilter = "이미지 파일|*.png;*.jpg;*.jpeg;*.bmp;*.gif|모든 파일|*.*";
        private readonly IDialogService _dialog;
        private readonly IImageManager _imageManager;

        /// <summary>이 목록이 다루는 이미지 구분(코드 114). 신규 첨부의 type 으로 쓰고 표의 "구분" 컬럼에 표시된다.</summary>
        public CdImageType ImageType { get; }

        public ObservableCollection<TImage> Images { get; } = new();

        /// <summary>삭제된 기존 이미지의 연결 키(<see cref="ImageInfo.LinkImageNum"/>) 목록. image/del 요청에 쓴다.</summary>
        public List<int> DeletedImageIds { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(DeleteCommand), nameof(ViewCommand))]
        private TImage? _selectedImage;

        [ObservableProperty]
        private bool _isReadOnly;

        [ObservableProperty]
        private string _countText = "0";

        public ImageGridViewModel(IDialogService dialog, IImageManager imageManager, CdImageType imageType)
        {
            _dialog = dialog;
            _imageManager = imageManager;
            ImageType = imageType;
            Images.CollectionChanged += (_, _) => CountText = Images.Count.ToString();
        }

        public void Load(IEnumerable<TImage> images)
        {
            Images.Clear();
            DeletedImageIds.Clear();
            foreach (var image in images)
                Images.Add(image);
            SelectedImage = Images.FirstOrDefault();
        }

        public void Clear() => Load(Enumerable.Empty<TImage>());

        /// <summary>기존 + 신규 첨부 전체. 저장 요청의 images 로 넘긴다.</summary>
        public List<TImage> ToList() => Images.ToList();

        private bool HasSelection() => SelectedImage != null;

        [RelayCommand]
        private void Add()
        {
            string? path = _dialog.OpenFile(ImageFilter, "이미지 선택");
            if (string.IsNullOrEmpty(path))
                return;

            byte[] bytes = File.ReadAllBytes(path);
            var (width, height) = ImageHelper.ReadPixelSize(bytes);
            var image = new TImage
            {
                name = Path.GetFileNameWithoutExtension(path),
                extension = Path.GetExtension(path).TrimStart('.'),
                size = bytes.LongLength,
                width = width,
                height = height,
                type = ImageType,
                data = bytes,
                status = CdStatus.사용,
                created_at = DateTime.Now,
            };
            Images.Add(image);
            SelectedImage = image;
        }

        /// <summary>
        /// 다른 기록의 첨부 이미지를 이 목록의 신규 첨부로 복사한다. (새 메시지 → 주유/유지보수 전환 때 영수증을 그대로 옮긴다)
        /// 서버 이미지는 data 가 비어 있으므로 먼저 내려받는다. 복사본은 연결 키가 없어 IsNew 이고, 저장 때 image/add 로 올라간다.
        /// </summary>
        public async Task<(bool Success, string Message)> AddCopyAsync(ImageInfo source)
        {
            var (ok, msg) = await _imageManager.LoadDataAsync(source);
            if (ok == false || source.data == null || source.data.Length == 0)
                return (false, string.IsNullOrEmpty(msg) ? "이미지를 내려받지 못했습니다." : msg);

            var image = new TImage
            {
                name = source.name,
                extension = source.extension,
                size = source.size > 0 ? source.size : source.data.LongLength,
                width = source.width,
                height = source.height,
                type = ImageType,
                data = source.data,
                status = CdStatus.사용,
                created_at = DateTime.Now,
            };
            Images.Add(image);
            SelectedImage ??= image;
            return (true, "");
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void Delete()
        {
            if (SelectedImage == null)
                return;
            if (_dialog.Confirm("선택한 이미지를 삭제하시겠습니까?", "이미지 삭제") == false)
                return;

            if (SelectedImage.IsNew == false)
                DeletedImageIds.Add(SelectedImage.LinkImageNum);
            Images.Remove(SelectedImage);
            SelectedImage = Images.FirstOrDefault();
        }

        /// <summary>
        /// 이미지 보기 팝업 (행 더블클릭 / 보기 버튼).
        /// 서버 이미지는 data 가 비어 있으므로 인증된 GET(url) 으로 원본을 받아 data 에 채운 뒤 연다. (한 번 받으면 재사용)
        /// 모달이 아니라 띄워 둔 채 입력을 계속할 수 있고, 이 목록이 있는 창이 닫히면 함께 닫힌다.
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelection))]
        private async Task ViewAsync()
        {
            var image = SelectedImage;
            if (image == null)
                return;

            if (image.data == null || image.data.Length == 0)
            {
                var (ok, msg) = await _imageManager.LoadDataAsync(image);
                if (ok == false)
                {
                    _dialog.ShowError($"이미지를 불러오지 못했습니다.\r\n{msg}");
                    return;
                }
            }
            _dialog.Show(new ImageViewerViewModel(image));
        }
    }
}
