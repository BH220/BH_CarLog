using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Messages;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BH_CarLog.ViewModels.Fuel
{
    /// <summary>
    /// 주유 목록 (기존 frmOilList). 상단에서 선택한 차량의 기록만 보여준다.
    /// 연비는 서버가 주지 않으므로 직전 주유와의 주행거리 차이로 계산한다.
    /// </summary>
    public partial class FuelListViewModel : ListViewModelBase<FuelInfo>, IRecipient<CarChangedMessage>
    {
        private readonly IFuelManager _manager;
        private readonly ICarShopManager _shops;
        private readonly ICarContext _carContext;
        private readonly IServiceProvider _services;

        private decimal _averageEfficiency;

        public override Menus MenuId => Menus.주유;

        public FuelListViewModel(IFuelManager manager, ICarShopManager shops, ICarContext carContext,
            IDialogService dialog, IMessenger messenger, IServiceProvider services)
            : base(dialog, messenger)
        {
            _manager = manager;
            _shops = shops;
            _carContext = carContext;
            _services = services;
            Title = "주유 기록";
            Messenger.Register(this);
        }

        public void Receive(CarChangedMessage message) => _ = RefreshAsync();

        protected override async Task<List<FuelInfo>> LoadItemsAsync()
        {
            int carNum = _carContext.SelectedCarNum;
            if (carNum <= 0)
            {
                _averageEfficiency = 0m;
                return new List<FuelInfo>();
            }

            // 조회는 모두 선택된 차량 기준이다. (서버가 car_num 을 필수로 받는다)
            var all = await _manager.GetListAsync(carNum);
            var shops = (await _shops.GetListAsync()).ToDictionary(x => x.car_shop_num, x => x.name);

            // 주유일 오름차순. 직전 기록을 알아야 연비를 계산할 수 있고, 화면도 최신 자료가 맨 아래로 간다.
            var list = all.Where(x => x.car_num == carNum)
                          .OrderBy(x => x.refuel_at)
                          .ThenBy(x => x.mileage)
                          .ToList();

            FuelInfo? prev = null;
            foreach (var item in list)
            {
                item.CarName = _carContext.GetCarName(item.car_num);
                item.ShopName = shops.TryGetValue(item.car_shop_num, out var name) ? name : "";
                item.Efficiency = CalcEfficiency(prev, item);
                prev = item;
            }

            var efficiencies = list.Where(x => x.Efficiency.HasValue).Select(x => x.Efficiency!.Value).ToList();
            _averageEfficiency = efficiencies.Count > 0 ? Math.Round(efficiencies.Average(), 1) : 0m;

            return list;
        }

        /// <summary>직전 주유 대비 연비(㎞/ℓ). 거리나 주유량을 알 수 없으면 null.</summary>
        private static decimal? CalcEfficiency(FuelInfo? prev, FuelInfo current)
        {
            if (prev == null)
                return null;
            int distance = current.mileage - prev.mileage;
            if (distance <= 0 || current.Liter <= 0)
                return null;
            return Math.Round(distance / current.Liter, 1);
        }

        protected override string GetStatusText()
            => _averageEfficiency > 0
                ? $"{Title} - {Items.Count}건, 평균연비 {_averageEfficiency:#,0.#}㎞/ℓ"
                : $"{Title} - {Items.Count}건";

        protected override string GetSearchText(FuelInfo item)
            => $"{item.refuel_at:yyyy-MM-dd} {item.ShopName} {item.fuel_type} {item.mileage} {item.amount} {item.note}";

        protected override Task InsertAsync() => OpenEditAsync(null);

        protected override Task UpdateAsync(FuelInfo item) => OpenEditAsync(item.car_fuel_history_num);

        protected override async Task DeleteAsync(FuelInfo item)
        {
            var (success, message) = await _manager.DeleteAsync(item.car_fuel_history_num);
            if (success)
                await RefreshAsync();
            else
                Dialog.ShowError(message);
        }

        private async Task OpenEditAsync(int? fuelNum)
        {
            if (_carContext.SelectedCarNum <= 0)
            {
                Dialog.ShowWarning("먼저 상단에서 차량을 선택하세요.");
                return;
            }

            var vm = _services.GetRequiredService<FuelEditViewModel>();
            if (await vm.LoadAsync(fuelNum) == false)
                return;
            if (Dialog.ShowDialog(vm) == true)
                await RefreshAsync();
        }
    }
}
