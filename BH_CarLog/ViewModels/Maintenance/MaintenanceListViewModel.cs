using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Messages;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BH_CarLog.ViewModels.Maintenance
{
    /// <summary>
    /// 유지보수 목록. 기존 WinForms 의 정비·구매·세차·공기압 화면을 유형(code:124) 한 컬럼으로 합쳤다.
    /// 서버 테이블이 하나이기 때문이다.
    /// </summary>
    public partial class MaintenanceListViewModel : ListViewModelBase<MaintenanceInfo>, IRecipient<CarChangedMessage>
    {
        private readonly IMaintenanceManager _manager;
        private readonly ICarShopManager _shops;
        private readonly ICarContext _carContext;
        private readonly IServiceProvider _services;

        private int _totalAmount;

        public override Menus MenuId => Menus.유지보수;

        public MaintenanceListViewModel(IMaintenanceManager manager, ICarShopManager shops, ICarContext carContext,
            IDialogService dialog, IMessenger messenger, IServiceProvider services)
            : base(dialog, messenger)
        {
            _manager = manager;
            _shops = shops;
            _carContext = carContext;
            _services = services;
            Title = "유지보수 기록";
            Messenger.Register(this);
        }

        public void Receive(CarChangedMessage message) => _ = RefreshAsync();

        protected override async Task<List<MaintenanceInfo>> LoadItemsAsync()
        {
            int carNum = _carContext.SelectedCarNum;
            if (carNum <= 0)
            {
                _totalAmount = 0;
                return new List<MaintenanceInfo>();
            }

            // 조회는 모두 선택된 차량 기준이다. (서버가 car_num 을 필수로 받는다)
            var all = await _manager.GetListAsync(carNum);
            var shops = (await _shops.GetListAsync()).ToDictionary(x => x.car_shop_num, x => x.name);

            // 유지보수일(mataintenance_at) 오름차순, 같은 날은 주행거리·번호순. 최신 자료가 맨 아래로 간다. (주유 목록과 같은 규칙)
            var list = all.Where(x => x.car_num == carNum)
                          .OrderBy(x => x.mataintenance_at)
                          .ThenBy(x => x.mileage)
                          .ThenBy(x => x.car_maintenance_num)
                          .ToList();

            foreach (var item in list)
            {
                item.CarName = _carContext.GetCarName(item.car_num);
                item.ShopName = shops.TryGetValue(item.car_shop_num, out var name) ? name : "";
            }

            _totalAmount = list.Sum(x => x.amount);
            return list;
        }

        protected override string GetStatusText()
            => $"{Title} - {Items.Count}건, 합계 {_totalAmount:#,0}원";

        protected override string GetSearchText(MaintenanceInfo item)
            => $"{item.mataintenance_at:yyyy-MM-dd} {item.mataintenance_type} {item.ShopName} {item.mileage} {item.amount} {item.note}";

        protected override Task InsertAsync() => OpenEditAsync(null);

        protected override Task UpdateAsync(MaintenanceInfo item) => OpenEditAsync(item.car_maintenance_num);

        protected override async Task DeleteAsync(MaintenanceInfo item)
        {
            var (success, message) = await _manager.DeleteAsync(item.car_maintenance_num);
            if (success)
                await RefreshAsync();
            else
                Dialog.ShowError(message);
        }

        private async Task OpenEditAsync(int? maintenanceNum)
        {
            if (_carContext.SelectedCarNum <= 0)
            {
                Dialog.ShowWarning("먼저 상단에서 차량을 선택하세요.");
                return;
            }

            var vm = _services.GetRequiredService<MaintenanceEditViewModel>();
            if (await vm.LoadAsync(maintenanceNum) == false)
                return;
            if (Dialog.ShowDialog(vm) == true)
                await RefreshAsync();
        }
    }
}
