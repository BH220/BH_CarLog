using System.Collections.ObjectModel;
using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BH_CarLog.ViewModels.Car
{
    /// <summary>차량 추가/수정 (기존 frmCarInfo).</summary>
    public partial class CarEditViewModel : EditViewModelBase
    {
        private readonly ICarManager _manager;

        /// <summary>수정 대상 car_num. 0 이면 신규</summary>
        private int _carNum;

        public ObservableCollection<CodeInfo> Fuels { get; } = new();

        [ObservableProperty] private string _carNo = "";
        [ObservableProperty] private string _carName = "";
        [ObservableProperty] private string _bornYear = "";
        [ObservableProperty] private string _vin = "";
        [ObservableProperty] private string _carType = "";
        [ObservableProperty] private CodeInfo? _selectedFuel;
        [ObservableProperty] private string _cc = "";
        [ObservableProperty] private DateTime? _buyAt;
        [ObservableProperty] private string _note = "";
        [ObservableProperty] private bool _isCarNoFocused;

        public CarEditViewModel(ICarManager manager, ICodeManager codes, IDialogService dialog)
            : base(dialog, codes)
        {
            _manager = manager;
        }

        /// <summary>carNum 이 null/0 이면 추가, 아니면 상세 API 로 불러와 수정 모드로 연다.</summary>
        public async Task<bool> LoadAsync(int? carNum)
        {
            Fuels.Clear();
            foreach (var code in await Codes.GetCodesAsync(CodeTypes.Fuel))
                Fuels.Add(code);

            if (carNum == null || carNum <= 0)
            {
                IsNew = true;
                Title = "차량정보 추가";
                _carNum = 0;
                BornYear = DateTime.Now.Year.ToString();
                BuyAt = DateTime.Today;
            }
            else
            {
                var info = await LoadDetailAsync(() => _manager.GetAsync(carNum.Value), "차량정보");
                if (info == null)
                    return false;
                IsNew = false;
                Title = "차량정보 수정";
                _carNum = info.car_num;

                CarNo = info.car_no ?? "";
                CarName = info.car_name ?? "";
                BornYear = info.born_year ?? "";
                Vin = info.vin ?? "";
                CarType = info.car_type ?? "";
                Cc = info.cc ?? "";
                BuyAt = info.buy_at;
                Note = info.note ?? "";
                SelectedFuel = Fuels.FirstOrDefault(x => x.SqCode == (int)info.fuel_type);
            }

            IsCarNoFocused = true;
            return true;
        }

        protected override bool Validate(out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(CarNo))
            {
                error = "차번호를 입력하세요.";
                IsCarNoFocused = true;
                return false;
            }
            if (string.IsNullOrWhiteSpace(CarName))
            {
                error = "차량 이름(애칭)을 입력하세요.";
                return false;
            }
            // born_year 는 char(4) 라 서버가 네 자리 연도만 받는다.
            if (BornYear.Trim().Length != 4 || int.TryParse(BornYear.Trim(), out _) == false)
            {
                error = "차량년식은 네 자리 연도로 입력하세요. (예: 2019)";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Vin))
            {
                error = "차대번호를 입력하세요.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(CarType))
            {
                error = "차종을 입력하세요.";
                return false;
            }
            if (SelectedFuel == null)
            {
                error = "연료를 선택하세요.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Cc))
            {
                error = "배기량을 입력하세요.";
                return false;
            }
            if (BuyAt.HasValue == false)
            {
                error = "구입일을 입력하세요.";
                return false;
            }
            return true;
        }

        protected override async Task<(bool Success, string Message)> SaveAsync()
        {
            var info = new CarInfo
            {
                car_num = IsNew ? 0 : _carNum,
                car_no = CarNo.Trim(),
                car_name = CarName.Trim(),
                born_year = BornYear.Trim(),
                vin = Vin.Trim(),
                car_type = CarType.Trim(),
                fuel_type = (CdFuel)(SelectedFuel?.SqCode ?? 0),
                cc = Cc.Trim(),
                buy_at = BuyAt,
                note = Note.Trim(),
            };

            var result = await _manager.SaveAsync(info);
            if (result.Success)
            {
                IsNew = false;
                _carNum = info.car_num;
            }
            return result;
        }

        protected override void ResetForContinue()
        {
            _carNum = 0;
            CarNo = "";
            CarName = "";
            BornYear = DateTime.Now.Year.ToString();
            Vin = "";
            CarType = "";
            Cc = "";
            BuyAt = DateTime.Today;
            Note = "";
            SelectedFuel = null;
            Title = "차량정보 추가";
            IsCarNoFocused = true;
        }
    }
}
