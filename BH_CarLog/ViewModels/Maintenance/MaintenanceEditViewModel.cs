using System.Collections.ObjectModel;
using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using BH_CarLog.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BH_CarLog.ViewModels.Maintenance
{
    /// <summary>유지보수 기록 추가/수정 (기존 frmFixedInfo / frmCarWashInfo / frmPurchaseInfo / frmAirPressureInfo 통합).</summary>
    public partial class MaintenanceEditViewModel : EditViewModelBase
    {
        private readonly IMaintenanceManager _manager;
        private readonly ICarShopManager _shops;
        private readonly ICarContext _carContext;

        /// <summary>수정 대상 car_maintenance_num. 0 이면 신규</summary>
        private int _maintenanceNum;

        /// <summary>마지막으로 저장에 성공한 car_maintenance_num. "저장 후 계속" 으로 화면이 초기화돼도 남는다. (새 메시지 전환이 realize 에 쓴다)</summary>
        public int LastSavedNum { get; private set; }

        /// <summary>마지막으로 저장에 성공한 기록의 유형. 입력 창에서 유형을 바꿔 저장했을 수 있어 전환 표시(transfer_to)에 이 값을 쓴다.</summary>
        public CdMaintenanceType LastSavedType { get; private set; }

        public ObservableCollection<CarInfo> Cars { get; } = new();
        public ObservableCollection<CodeInfo> Types { get; } = new();
        public ImageGridViewModel<MaintenanceImageInfo> Images { get; }

        /// <summary>가게 입력. 콤보가 아니라 가게명 일부 + Enter 로 찾는다 (기존 BhSearchItem).</summary>
        public ShopPickerViewModel Shop { get; }

        [ObservableProperty] private CarInfo? _selectedCar;
        [ObservableProperty] private CodeInfo? _selectedType;
        /// <summary>유지보수일 (서버 mataintenance_at). 추가 모드는 오늘로 시작한다.</summary>
        [ObservableProperty] private DateTime? _maintenanceAt;
        [ObservableProperty] private string _mileage = "";
        [ObservableProperty] private string _amount = "";
        [ObservableProperty] private string _note = "";
        [ObservableProperty] private bool _isMileageFocused;

        public MaintenanceEditViewModel(IMaintenanceManager manager, ICarShopManager shops, ICarContext carContext,
            ICodeManager codes, IDialogService dialog, IImageManager images)
            : base(dialog, codes)
        {
            _manager = manager;
            _shops = shops;
            _carContext = carContext;
            Images = new ImageGridViewModel<MaintenanceImageInfo>(dialog, images, CdImageType.영수증);
            Shop = new ShopPickerViewModel(dialog);
        }

        /// <param name="maintenanceNum">수정/상세 대상. null 또는 0 이면 추가</param>
        /// <param name="readOnly">true 면 상세 보기 전용으로 연다 (입력 불가, 저장 버튼 없음). 추가 모드에서는 무시된다.</param>
        public async Task<bool> LoadAsync(int? maintenanceNum, bool readOnly = false)
        {
            Cars.Clear();
            foreach (var car in _carContext.Cars)
                Cars.Add(car);

            // 유지보수는 어느 가게에서든 할 수 있으므로 전부 고를 수 있게 한다.
            var allShops = await _shops.GetListAsync();
            Shop.SetCandidates(allShops);

            Types.Clear();
            foreach (var code in await Codes.GetCodesAsync(CodeTypes.Maintenance))
                Types.Add(code);

            if (maintenanceNum == null || maintenanceNum <= 0)
            {
                IsNew = true;
                Title = "유지보수 추가";
                _maintenanceNum = 0;
                SelectedCar = Cars.FirstOrDefault(x => x.car_num == _carContext.SelectedCarNum);
                Shop.Set(null);
                SelectedType = Types.FirstOrDefault(x => x.SqCode == (int)CdMaintenanceType.정비);
                MaintenanceAt = DateTime.Today;
                Images.Clear();
            }
            else
            {
                var info = await LoadDetailAsync(() => _manager.GetAsync(maintenanceNum.Value), "유지보수 기록");
                if (info == null)
                    return false;
                IsNew = false;
                Title = readOnly ? "유지보수 상세" : "유지보수 수정";
                _maintenanceNum = info.car_maintenance_num;

                MaintenanceAt = info.mataintenance_at;
                Mileage = info.mileage.ToString();
                Amount = info.amount.ToString();
                Note = info.note ?? "";
                SelectedCar = Cars.FirstOrDefault(x => x.car_num == info.car_num);
                Shop.Set(allShops.FirstOrDefault(x => x.car_shop_num == info.car_shop_num));
                SelectedType = Types.FirstOrDefault(x => x.SqCode == (int)info.mataintenance_type);
                Images.Load(info.images);
            }

            // 상세 보기: 입력을 막고 첨부 이미지도 보기만 되게 한다.
            IsReadOnly = readOnly && IsNew == false;
            Images.IsReadOnly = IsReadOnly;
            Shop.IsReadOnly = IsReadOnly;
            // 원본(frmFixedInfo)과 같이 가게 입력란부터 시작한다. 가게명 → Enter → 다음 칸 순으로 친다.
            Shop.IsFocused = IsReadOnly == false;
            return true;
        }

        /// <summary>
        /// 새 메시지(웹 바로등록)에서 넘어온 값으로 채운다. <see cref="LoadAsync"/> 를 추가 모드로 부른 뒤 호출한다.
        /// 가게·금액은 웹 입력에 없어 사용자가 직접 넣는다. 날짜는 웹 입력일(input_at)을 쓰고 없으면 오늘 그대로 둔다.
        /// </summary>
        public void Prefill(int carNum, CdMaintenanceType type, DateTime? maintenanceAt, int mileage, string note)
        {
            SelectedCar = Cars.FirstOrDefault(x => x.car_num == carNum) ?? SelectedCar;
            SelectedType = Types.FirstOrDefault(x => x.SqCode == (int)type) ?? SelectedType;
            if (maintenanceAt.HasValue)
                MaintenanceAt = maintenanceAt;
            if (mileage > 0)
                Mileage = mileage.ToString();
            Note = note ?? "";
        }

        protected override bool Validate(out string error)
        {
            error = "";
            if (SelectedCar == null)
            {
                error = "차량을 선택하세요.";
                return false;
            }
            // Enter 없이 바로 저장을 눌러도 원본처럼 찾는다. 하나면 확정, 여럿이면 선택 창이 뜬다.
            if (Shop.EnsureResolved() == false)
            {
                error = "가게를 선택하세요. 가게명 일부를 입력하고 Enter 를 누르면 찾아 줍니다.";
                Shop.IsFocused = true;
                return false;
            }
            if (SelectedType == null)
            {
                error = "유지보수 유형을 선택하세요.";
                return false;
            }
            if (MaintenanceAt.HasValue == false)
            {
                error = "날짜를 입력하세요.";
                return false;
            }
            if (ParseInt(Mileage) <= 0)
            {
                error = "주행거리를 입력하세요.";
                IsMileageFocused = true;
                return false;
            }
            return true;
        }

        protected override async Task<(bool Success, string Message)> SaveAsync()
        {
            var info = new MaintenanceInfo
            {
                car_maintenance_num = IsNew ? 0 : _maintenanceNum,
                car_num = SelectedCar?.car_num ?? 0,
                car_shop_num = Shop.Selected?.car_shop_num ?? 0,
                mataintenance_type = (CdMaintenanceType)(SelectedType?.SqCode ?? 0),
                mataintenance_at = MaintenanceAt,
                mileage = ParseInt(Mileage),
                amount = ParseInt(Amount),
                note = Note.Trim(),
                status = CdStatus.사용,
                images = Images.ToList(),
            };

            var result = IsNew
                ? await _manager.InsertAsync(info)
                : await _manager.UpdateAsync(info, Images.DeletedImageIds);

            if (result.Success)
            {
                IsNew = false;
                _maintenanceNum = info.car_maintenance_num;
                LastSavedNum = info.car_maintenance_num;
                LastSavedType = info.mataintenance_type;
                Images.DeletedImageIds.Clear();
            }
            return result;
        }

        protected override void ResetForContinue()
        {
            _maintenanceNum = 0;
            MaintenanceAt = DateTime.Today;
            Mileage = "";
            Amount = "";
            Note = "";
            Images.Clear();
            Title = "유지보수 추가";
            IsMileageFocused = true;
        }

        private static int ParseInt(string? text)
            => int.TryParse((text ?? "").Replace(",", "").Trim(), out int value) ? value : 0;
    }
}
