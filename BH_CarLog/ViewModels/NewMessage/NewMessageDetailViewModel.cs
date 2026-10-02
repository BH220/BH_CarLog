using BH_CarLog.Api;
using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using BH_CarLog.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BH_CarLog.ViewModels.NewMessage
{
    /// <summary>
    /// 새 메시지 상세 (기존 frmNewMessageInfo). 웹 바로등록 한 건의 내용과 사진을 읽기 전용으로 보여 준다.
    /// 임시 데이터는 여기서 고치지 않는다. "실데이터 전환" 을 누르면 창을 닫고, 목록 화면이 전환 흐름을 이어 간다.
    /// </summary>
    public partial class NewMessageDetailViewModel : DialogViewModelBase
    {
        private readonly INewMessageManager _manager;
        private readonly ICarContext _carContext;
        private readonly IDialogService _dialog;

        /// <summary>첨부 사진. 보기만 된다.</summary>
        public ImageGridViewModel<NewMessageImageInfo> Images { get; }

        /// <summary>불러온 메시지 (POST /api/carlog/direct/detail). 전환 흐름이 그대로 쓴다.</summary>
        public NewMessageInfo? Info { get; private set; }

        /// <summary>"실데이터 전환" 으로 닫혔는지. 닫기(Esc)면 false</summary>
        public bool TransferRequested { get; private set; }

        [ObservableProperty] private string _carName = "";
        [ObservableProperty] private string _inputAtText = "";
        [ObservableProperty] private string _typeText = "";
        [ObservableProperty] private string _mileageText = "";
        [ObservableProperty] private string _note = "";
        [ObservableProperty] private string _createdAtText = "";
        [ObservableProperty] private string _transferText = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(TransferCommand))]
        private bool _canTransfer;

        public NewMessageDetailViewModel(INewMessageManager manager, ICarContext carContext, IDialogService dialog, IImageManager images)
        {
            _manager = manager;
            _carContext = carContext;
            _dialog = dialog;
            Images = new ImageGridViewModel<NewMessageImageInfo>(dialog, images, CdImageType.영수증) { IsReadOnly = true };
            Title = "새 메시지 상세";
        }

        public async Task<bool> LoadAsync(int recoardNum)
        {
            NewMessageInfo? info;
            try
            {
                info = await _manager.GetAsync(recoardNum);
            }
            catch (ApiException ex)
            {
                _dialog.ShowError($"새 메시지 조회에 실패했습니다.\r\n{ex.Message}");
                return false;
            }
            if (info == null)
            {
                _dialog.ShowError("새 메시지 조회에 실패했습니다.");
                return false;
            }

            Info = info;
            CarName = _carContext.GetCarName(info.car_num);
            InputAtText = info.input_at.HasValue ? info.input_at.Value.ToString("yyyy-MM-dd") : "-";
            TypeText = info.recoard_type.ToString();
            MileageText = $"{info.mileage:#,0} ㎞";
            Note = info.note ?? "";
            CreatedAtText = info.created_at.HasValue ? info.created_at.Value.ToString("yyyy-MM-dd HH:mm") : "-";
            TransferText = info.transfer_type == CdTransfer.전환대기
                ? "전환 대기"
                : $"실데이터 전환 완료 ({info.transfer_to} #{info.transfer_num}, {info.transferred_at:yyyy-MM-dd HH:mm})";
            Images.Load(info.images);

            // 이미 옮긴 기록은 다시 전환하지 않는다. (서버 realize 도 거절한다)
            CanTransfer = info.transfer_type == CdTransfer.전환대기;
            return true;
        }

        /// <summary>창을 닫고 목록 화면에 전환을 맡긴다. 전환 자체는 주유/유지보수 입력 창을 거쳐야 해서 여기서 하지 않는다.</summary>
        [RelayCommand(CanExecute = nameof(CanTransfer))]
        private void Transfer()
        {
            TransferRequested = true;
            DialogResult = true;
        }
    }
}
