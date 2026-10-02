using System.Collections.ObjectModel;
using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using BH_CarLog.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BH_CarLog.ViewModels.Fuel
{
    /// <summary>주유 기록 추가/수정 (기존 frmOilInfo).</summary>
    public partial class FuelEditViewModel : EditViewModelBase
    {
        private readonly IFuelManager _manager;
        private readonly ICarShopManager _shops;
        private readonly ICarContext _carContext;

        /// <summary>수정 대상 car_fuel_history_num. 0 이면 신규</summary>
        private int _fuelNum;

        /// <summary>마지막으로 저장에 성공한 car_fuel_history_num. "저장 후 계속" 으로 화면이 초기화돼도 남는다. (새 메시지 전환이 realize 에 쓴다)</summary>
        public int LastSavedNum { get; private set; }

        public ObservableCollection<CarInfo> Cars { get; } = new();
        public ObservableCollection<CodeInfo> Fuels { get; } = new();
        public ImageGridViewModel<FuelImageInfo> Images { get; }

        /// <summary>가게 입력. 콤보가 아니라 가게명 일부 + Enter 로 찾는다 (기존 BhSearchItem).</summary>
        public ShopPickerViewModel Shop { get; }

        [ObservableProperty] private CarInfo? _selectedCar;
        [ObservableProperty] private CodeInfo? _selectedFuel;
        [ObservableProperty] private DateTime? _refuelAt;

        [ObservableProperty]
        private string _mileage = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LiterText))]
        private string _amount = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LiterText))]
        private string _amountPerLiter = "";

        [ObservableProperty] private string _note = "";
        [ObservableProperty] private bool _isMileageFocused;

        /// <summary>주유량은 저장하지 않는다. 금액 ÷ 리터당 금액 으로 보여 주기만 한다.</summary>
        public string LiterText
        {
            get
            {
                int amount = ParseInt(Amount);
                int perLiter = ParseInt(AmountPerLiter);
                if (amount <= 0 || perLiter <= 0)
                    return "-";
                return $"{(decimal)amount / perLiter:#,0.##} ℓ";
            }
        }

        public FuelEditViewModel(IFuelManager manager, ICarShopManager shops, ICarContext carContext,
            ICodeManager codes, IDialogService dialog, IImageManager images)
            : base(dialog, codes)
        {
            _manager = manager;
            _shops = shops;
            _carContext = carContext;
            Images = new ImageGridViewModel<FuelImageInfo>(dialog, images, CdImageType.영수증);
            Shop = new ShopPickerViewModel(dialog);
        }

        public async Task<bool> LoadAsync(int? fuelNum)
        {
            Cars.Clear();
            foreach (var car in _carContext.Cars)
                Cars.Add(car);

            // 주유는 주유소에서 하지만 혼합 매장도 있어 둘 다 고를 수 있게 한다.
            // 저장된 가게는 유형이 바뀌었어도 보여야 하므로 전체 목록에서 찾는다.
            var allShops = await _shops.GetListAsync();
            Shop.SetCandidates(allShops.Where(x => x.shop_type == CdShopType.주유소 || x.shop_type == CdShopType.혼합));

            Fuels.Clear();
            foreach (var code in await Codes.GetCodesAsync(CodeTypes.Fuel))
                Fuels.Add(code);

            if (fuelNum == null || fuelNum <= 0)
            {
                IsNew = true;
                Title = "주유기록 추가";
                _fuelNum = 0;
                RefuelAt = DateTime.Today;
                SelectedCar = Cars.FirstOrDefault(x => x.car_num == _carContext.SelectedCarNum);
                Shop.Set(null);
                // 차에 등록된 연료를 기본값으로 잡아 준다.
                SelectedFuel = Fuels.FirstOrDefault(x => x.SqCode == (int)(SelectedCar?.fuel_type ?? 0));
                Images.Clear();
            }
            else
            {
                var info = await LoadDetailAsync(() => _manager.GetAsync(fuelNum.Value), "주유기록");
                if (info == null)
                    return false;
                IsNew = false;
                Title = "주유기록 수정";
                _fuelNum = info.car_fuel_history_num;

                RefuelAt = info.refuel_at;
                Mileage = info.mileage.ToString();
                Amount = info.amount.ToString();
                AmountPerLiter = info.amount_per_liter.ToString();
                Note = info.note ?? "";
                SelectedCar = Cars.FirstOrDefault(x => x.car_num == info.car_num);
                Shop.Set(allShops.FirstOrDefault(x => x.car_shop_num == info.car_shop_num));
                SelectedFuel = Fuels.FirstOrDefault(x => x.SqCode == (int)info.fuel_type);
                Images.Load(info.images);
            }

            // 원본(frmOilInfo)과 같이 가게 입력란부터 시작한다. 가게명 → Enter → 다음 칸 순으로 친다.
            Shop.IsFocused = true;
            return true;
        }

        /// <summary>
        /// 새 메시지(웹 바로등록)에서 넘어온 값으로 채운다. <see cref="LoadAsync"/> 를 추가 모드로 부른 뒤 호출한다.
        /// 금액·리터당 금액은 웹 입력에 없어 사용자가 직접 넣는다.
        /// </summary>
        public void Prefill(int carNum, DateTime? refuelAt, int mileage, string note)
        {
            SelectedCar = Cars.FirstOrDefault(x => x.car_num == carNum) ?? SelectedCar;
            SelectedFuel = Fuels.FirstOrDefault(x => x.SqCode == (int)(SelectedCar?.fuel_type ?? 0)) ?? SelectedFuel;
            if (refuelAt.HasValue)
                RefuelAt = refuelAt;
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
                error = "주유한 가게를 선택하세요. 가게명 일부를 입력하고 Enter 를 누르면 찾아 줍니다.";
                Shop.IsFocused = true;
                return false;
            }
            if (RefuelAt.HasValue == false)
            {
                error = "주유일을 입력하세요.";
                return false;
            }
            if (ParseInt(Mileage) <= 0)
            {
                error = "주행거리를 입력하세요.";
                IsMileageFocused = true;
                return false;
            }
            if (ParseInt(Amount) <= 0)
            {
                error = "주유금액을 입력하세요.";
                return false;
            }
            if (ParseInt(AmountPerLiter) <= 0)
            {
                error = "리터당 금액을 입력하세요.";
                return false;
            }
            if (SelectedFuel == null)
            {
                error = "연료를 선택하세요.";
                return false;
            }
            return true;
        }

        protected override async Task<(bool Success, string Message)> SaveAsync()
        {
            var info = new FuelInfo
            {
                car_fuel_history_num = IsNew ? 0 : _fuelNum,
                car_num = SelectedCar?.car_num ?? 0,
                car_shop_num = Shop.Selected?.car_shop_num ?? 0,
                refuel_at = RefuelAt,
                mileage = ParseInt(Mileage),
                amount = ParseInt(Amount),
                amount_per_liter = ParseInt(AmountPerLiter),
                fuel_type = (CdFuel)(SelectedFuel?.SqCode ?? 0),
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
                _fuelNum = info.car_fuel_history_num;
                LastSavedNum = info.car_fuel_history_num;
                Images.DeletedImageIds.Clear();
            }
            return result;
        }

        protected override void ResetForContinue()
        {
            _fuelNum = 0;
            Mileage = "";
            Amount = "";
            AmountPerLiter = "";
            Note = "";
            RefuelAt = DateTime.Today;
            Images.Clear();
            Title = "주유기록 추가";
            IsMileageFocused = true;
        }

        private static int ParseInt(string? text)
            => int.TryParse((text ?? "").Replace(",", "").Trim(), out int value) ? value : 0;
    }
}
