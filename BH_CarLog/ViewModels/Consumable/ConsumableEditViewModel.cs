using System.Collections.ObjectModel;
using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BH_CarLog.ViewModels.Consumable
{
    /// <summary>소모품 추가/수정 (기존 frmConsumableInfo).</summary>
    public partial class ConsumableEditViewModel : EditViewModelBase
    {
        private readonly IConsumableManager _manager;
        private readonly ICarContext _carContext;

        /// <summary>수정 대상 car_consumables_num. 0 이면 신규</summary>
        private int _consumablesNum;

        public ObservableCollection<CarInfo> Cars { get; } = new();

        [ObservableProperty] private CarInfo? _selectedCar;
        [ObservableProperty] private string _name = "";
        [ObservableProperty] private string _term = "";
        [ObservableProperty] private string _distance = "";
        [ObservableProperty] private bool _isNameFocused;

        public ConsumableEditViewModel(IConsumableManager manager, ICarContext carContext,
            ICodeManager codes, IDialogService dialog)
            : base(dialog, codes)
        {
            _manager = manager;
            _carContext = carContext;
        }

        public Task<bool> LoadAsync(int? consumablesNum) => LoadInternalAsync(consumablesNum);

        private async Task<bool> LoadInternalAsync(int? consumablesNum)
        {
            Cars.Clear();
            foreach (var car in _carContext.Cars)
                Cars.Add(car);

            if (consumablesNum == null || consumablesNum <= 0)
            {
                IsNew = true;
                Title = "소모품 추가";
                _consumablesNum = 0;
                SelectedCar = Cars.FirstOrDefault(x => x.car_num == _carContext.SelectedCarNum);
            }
            else
            {
                var info = await LoadDetailAsync(() => _manager.GetAsync(consumablesNum.Value), "소모품 정보");
                if (info == null)
                    return false;
                IsNew = false;
                Title = "소모품 수정";
                _consumablesNum = info.car_consumables_num;

                Name = info.name ?? "";
                Term = info.term?.ToString() ?? "";
                Distance = info.distance?.ToString() ?? "";
                SelectedCar = Cars.FirstOrDefault(x => x.car_num == info.car_num);
            }

            IsNameFocused = true;
            return true;
        }

        protected override bool Validate(out string error)
        {
            error = "";
            if (SelectedCar == null)
            {
                error = "차량을 선택하세요.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Name))
            {
                error = "항목명을 입력하세요.";
                IsNameFocused = true;
                return false;
            }
            if (string.IsNullOrWhiteSpace(Term) && string.IsNullOrWhiteSpace(Distance))
            {
                error = "관리 주기(일) 또는 관리 주기(거리) 중 하나는 입력하세요.";
                return false;
            }
            return true;
        }

        protected override async Task<(bool Success, string Message)> SaveAsync()
        {
            var info = new ConsumableInfo
            {
                car_consumables_num = IsNew ? 0 : _consumablesNum,
                car_num = SelectedCar?.car_num ?? 0,
                name = Name.Trim(),
                // 빈 값은 "주기 없음"(NULL). 0 은 값이므로 기본값으로 쓰지 않는다.
                term = ParseNullableInt(Term),
                distance = ParseNullableInt(Distance),
            };

            var result = await _manager.SaveAsync(info);
            if (result.Success)
            {
                IsNew = false;
                _consumablesNum = info.car_consumables_num;
            }
            return result;
        }

        protected override void ResetForContinue()
        {
            _consumablesNum = 0;
            Name = "";
            Term = "";
            Distance = "";
            Title = "소모품 추가";
            IsNameFocused = true;
        }

        private static int? ParseNullableInt(string? text)
        {
            string value = (text ?? "").Replace(",", "").Trim();
            if (value.Length == 0)
                return null;
            return int.TryParse(value, out int parsed) ? parsed : null;
        }
    }
}
