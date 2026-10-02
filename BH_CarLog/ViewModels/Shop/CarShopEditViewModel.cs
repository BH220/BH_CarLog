using System.Collections.ObjectModel;
using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using BH_CarLog.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BH_CarLog.ViewModels.Shop
{
    /// <summary>가게 추가/수정 (기존 frmShopInfo).</summary>
    public partial class CarShopEditViewModel : EditViewModelBase
    {
        private readonly ICarShopManager _manager;

        /// <summary>수정 대상 car_shop_num. 0 이면 신규</summary>
        private int _carShopNum;

        public ObservableCollection<CodeInfo> ShopTypes { get; } = new();
        public ImageGridViewModel<CarShopImageInfo> Images { get; }

        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string _businessNo = "";
        [ObservableProperty] private CodeInfo? _selectedShopType;
        [ObservableProperty] private string _ceo = "";
        [ObservableProperty] private string _address = "";
        [ObservableProperty] private string _tel = "";
        [ObservableProperty] private string _tel2 = "";
        [ObservableProperty] private string _note = "";
        [ObservableProperty] private bool _isNameFocused;

        public CarShopEditViewModel(ICarShopManager manager, ICodeManager codes, IDialogService dialog, IImageManager images)
            : base(dialog, codes)
        {
            _manager = manager;
            Images = new ImageGridViewModel<CarShopImageInfo>(dialog, images, CdImageType.가게);
        }

        public async Task<bool> LoadAsync(int? carShopNum)
        {
            ShopTypes.Clear();
            foreach (var code in await Codes.GetCodesAsync(CodeTypes.ShopType))
                ShopTypes.Add(code);

            if (carShopNum == null || carShopNum <= 0)
            {
                IsNew = true;
                Title = "가게정보 추가";
                _carShopNum = 0;
                Images.Clear();
            }
            else
            {
                var info = await LoadDetailAsync(() => _manager.GetAsync(carShopNum.Value), "가게정보");
                if (info == null)
                    return false;
                IsNew = false;
                Title = "가게정보 수정";
                _carShopNum = info.car_shop_num;

                Name = info.name ?? "";
                BusinessNo = info.business_no ?? "";
                Ceo = info.ceo ?? "";
                Address = info.address ?? "";
                Tel = info.tel ?? "";
                Tel2 = info.tel2 ?? "";
                Note = info.note ?? "";
                SelectedShopType = ShopTypes.FirstOrDefault(x => x.SqCode == (int)info.shop_type);
                Images.Load(info.images);
            }

            IsNameFocused = true;
            return true;
        }

        protected override bool Validate(out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(Name))
            {
                error = "상호를 입력하세요.";
                IsNameFocused = true;
                return false;
            }
            if (string.IsNullOrWhiteSpace(BusinessNo))
            {
                error = "사업자번호를 입력하세요.\r\n모르는 경우 999-99-00000 형태로 임의 번호를 넣습니다.";
                return false;
            }
            if (SelectedShopType == null)
            {
                error = "가게 유형을 선택하세요.";
                return false;
            }
            return true;
        }

        protected override async Task<(bool Success, string Message)> SaveAsync()
        {
            var info = new CarShopInfo
            {
                car_shop_num = IsNew ? 0 : _carShopNum,
                name = Name.Trim(),
                business_no = BusinessNo.Trim(),
                ceo = Ceo.Trim(),
                address = Address.Trim(),
                tel = Tel.Trim(),
                tel2 = Tel2.Trim(),
                shop_type = (CdShopType)(SelectedShopType?.SqCode ?? 0),
                note = Note.Trim(),
                status = CdStatus.사용,
                images = Images.ToList(), // 기존 + 신규(IsNew, data) 첨부. 신규는 매니저가 image/add 로 올린다.
            };

            var result = IsNew
                ? await _manager.InsertAsync(info)
                : await _manager.UpdateAsync(info, Images.DeletedImageIds);

            if (result.Success)
            {
                IsNew = false;
                _carShopNum = info.car_shop_num;
                Images.DeletedImageIds.Clear();
            }
            return result;
        }

        protected override void ResetForContinue()
        {
            _carShopNum = 0;
            Name = "";
            BusinessNo = "";
            Ceo = "";
            Address = "";
            Tel = "";
            Tel2 = "";
            Note = "";
            SelectedShopType = null;
            Images.Clear();
            Title = "가게정보 추가";
            IsNameFocused = true;
        }
    }
}
