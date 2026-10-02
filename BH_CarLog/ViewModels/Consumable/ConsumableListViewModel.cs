using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Core.Constants;
using BH_CarLog.Messages;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BH_CarLog.ViewModels.Consumable
{
    /// <summary>
    /// 소모품 목록 (기존 frmConsumableList + frmTermList).
    /// 항목은 차 단위라 상단에서 선택한 차량으로만 조회한다.
    /// </summary>
    public partial class ConsumableListViewModel : ListViewModelBase<ConsumableInfo>, IRecipient<CarChangedMessage>
    {
        private readonly IConsumableManager _manager;
        private readonly ICarContext _carContext;
        private readonly IServiceProvider _services;

        public override Menus MenuId => Menus.소모품;

        public ConsumableListViewModel(IConsumableManager manager, ICarContext carContext,
            IDialogService dialog, IMessenger messenger, IServiceProvider services)
            : base(dialog, messenger)
        {
            _manager = manager;
            _carContext = carContext;
            _services = services;
            Title = "소모품 관리";
            Messenger.Register(this);
        }

        protected override void ConfigureButtons(FunctionButtonState buttons)
        {
            buttons.Custom1Visible = true;
            buttons.Custom1Text = "교체이력 (F1)";
        }

        public void Receive(CarChangedMessage message) => _ = RefreshAsync();

        protected override async Task<List<ConsumableInfo>> LoadItemsAsync()
        {
            int carNum = _carContext.SelectedCarNum;
            if (carNum <= 0)
                return new List<ConsumableInfo>();

            var list = await _manager.GetListAsync(carNum);
            foreach (var item in list)
                item.CarName = _carContext.GetCarName(item.car_num);
            // 소모품에는 날짜가 없다. 키가 auto_increment 라 등록순 오름차순이면 최근 등록이 맨 아래로 간다.
            return list.OrderBy(x => x.car_consumables_num).ToList();
        }

        protected override string GetSearchText(ConsumableInfo item)
            => $"{item.name} {item.term} {item.distance}";

        protected override Task InsertAsync() => OpenEditAsync(null);

        protected override Task UpdateAsync(ConsumableInfo item) => OpenEditAsync(item.car_consumables_num);

        protected override async Task DeleteAsync(ConsumableInfo item)
        {
            var (success, message) = await _manager.DeleteAsync(item.car_consumables_num);
            if (success)
                await RefreshAsync();
            else
                Dialog.ShowError(message);
        }

        /// <summary>F1: 선택한 소모품의 교체이력 창</summary>
        protected override Task CustomFunctionAsync(string functionId)
            => functionId == Functions.사용자기능1 ? ShowHistoryCommand.ExecuteAsync(null) : Task.CompletedTask;

        /// <summary>교체이력 보기. 상단 F1 버튼과 우클릭 메뉴가 함께 쓴다.</summary>
        [RelayCommand]
        private async Task ShowHistoryAsync()
        {
            if (SelectedItem == null)
            {
                Dialog.ShowWarning("교체이력을 볼 소모품이 선택되지 않았습니다.");
                return;
            }

            NotifyActivity();
            var vm = _services.GetRequiredService<ConsumableHistoryViewModel>();
            if (await vm.LoadAsync(SelectedItem.car_consumables_num) == false)
                return;
            Dialog.ShowDialog(vm);
        }

        private async Task OpenEditAsync(int? consumablesNum)
        {
            if (_carContext.SelectedCarNum <= 0)
            {
                Dialog.ShowWarning("먼저 상단에서 차량을 선택하세요.");
                return;
            }

            var vm = _services.GetRequiredService<ConsumableEditViewModel>();
            if (await vm.LoadAsync(consumablesNum) == false)
                return;
            if (Dialog.ShowDialog(vm) == true)
                await RefreshAsync();
        }
    }
}
