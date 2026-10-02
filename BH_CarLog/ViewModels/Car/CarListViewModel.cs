using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Messages;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BH_CarLog.ViewModels.Car
{
    /// <summary>차량 목록 (기존 frmCarList). 항목은 API 응답 <see cref="CarInfo"/> 를 그대로 쓴다.</summary>
    public partial class CarListViewModel : ListViewModelBase<CarInfo>
    {
        private readonly ICarManager _manager;
        private readonly IServiceProvider _services;

        public override Menus MenuId => Menus.차량;

        public CarListViewModel(ICarManager manager, IDialogService dialog, IMessenger messenger, IServiceProvider services)
            : base(dialog, messenger)
        {
            _manager = manager;
            _services = services;
            Title = "차량 정보";
        }

        protected override async Task<List<CarInfo>> LoadItemsAsync()
        {
            // 차량에는 날짜가 없다. 키가 auto_increment 라 등록순 오름차순이면 마지막 등록 차가 맨 아래로 간다.
            var list = await _manager.GetListAsync();
            return list.OrderBy(x => x.car_num).ToList();
        }

        protected override string GetSearchText(CarInfo item)
            => $"{item.car_no} {item.car_name} {item.car_type} {item.born_year} {item.vin} {item.cc} {item.fuel_type} {item.note}";

        protected override Task InsertAsync() => OpenEditAsync(null);

        protected override Task UpdateAsync(CarInfo item) => OpenEditAsync(item.car_num);

        protected override async Task DeleteAsync(CarInfo item)
        {
            var (success, message) = await _manager.DeleteAsync(item.car_num);
            if (success == false)
            {
                Dialog.ShowError(message);
                return;
            }
            await RefreshAsync();
            Messenger.Send(new CarListChangedMessage());
        }

        private async Task OpenEditAsync(int? carNum)
        {
            var vm = _services.GetRequiredService<CarEditViewModel>();
            if (await vm.LoadAsync(carNum) == false)
                return;
            if (Dialog.ShowDialog(vm) == true)
            {
                await RefreshAsync();
                // 상단 차량 콤보도 같이 갱신한다.
                Messenger.Send(new CarListChangedMessage());
            }
        }
    }
}
