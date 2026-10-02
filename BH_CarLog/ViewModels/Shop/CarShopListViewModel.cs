using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BH_CarLog.ViewModels.Shop
{
    /// <summary>가게 목록 (기존 frmShopList). 주유소·정비소·용품점을 유형으로 구분한다.</summary>
    public partial class CarShopListViewModel : ListViewModelBase<CarShopInfo>
    {
        private readonly ICarShopManager _manager;
        private readonly IServiceProvider _services;

        public override Menus MenuId => Menus.가게;

        public CarShopListViewModel(ICarShopManager manager, IDialogService dialog, IMessenger messenger, IServiceProvider services)
            : base(dialog, messenger)
        {
            _manager = manager;
            _services = services;
            Title = "가게 정보";
        }

        protected override async Task<List<CarShopInfo>> LoadItemsAsync()
        {
            // 가게는 차와 연결이 없어 차량 선택과 무관하게 전체를 보여 준다. (조회 API 도 car_num 을 받지 않는다)
            // 날짜가 없으니 키(auto_increment) 오름차순 = 등록순. 최근 등록 가게가 맨 아래로 간다.
            var list = await _manager.GetListAsync();
            return list.OrderBy(x => x.car_shop_num).ToList();
        }

        protected override string GetSearchText(CarShopInfo item)
            => $"{item.name} {item.business_no} {item.ceo} {item.address} {item.tel} {item.tel2} {item.shop_type} {item.note}";

        protected override Task InsertAsync() => OpenEditAsync(null);

        protected override Task UpdateAsync(CarShopInfo item) => OpenEditAsync(item.car_shop_num);

        protected override async Task DeleteAsync(CarShopInfo item)
        {
            var (success, message) = await _manager.DeleteAsync(item.car_shop_num);
            if (success)
                await RefreshAsync();
            else
                Dialog.ShowError(message);
        }

        private async Task OpenEditAsync(int? carShopNum)
        {
            var vm = _services.GetRequiredService<CarShopEditViewModel>();
            if (await vm.LoadAsync(carShopNum) == false)
                return;
            if (Dialog.ShowDialog(vm) == true)
                await RefreshAsync();
        }
    }
}
