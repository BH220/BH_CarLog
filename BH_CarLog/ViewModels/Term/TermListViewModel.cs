using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model;
using BH_CarLog.Core;
using BH_CarLog.Core.Constants;
using BH_CarLog.Messages;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using BH_CarLog.ViewModels.Consumable;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BH_CarLog.ViewModels.Term
{
    /// <summary>
    /// 교환주기 (기존 frmTermList, 좌측 메뉴 "주기(T)").
    /// 상단에서 고른 차의 소모품마다 교체이력과 최근 주행거리로 남은 주기·거리를 계산해 교환 필요 여부를 보여 준다.
    /// 소모품 자체의 추가/삭제는 소모품 화면에서 하고, 여기서는 수정(F8)·교체이력(F1)으로 이력 창만 연다.
    /// </summary>
    public partial class TermListViewModel : ListViewModelBase<TermStatusInfo>, IRecipient<CarChangedMessage>
    {
        private readonly ITermManager _manager;
        private readonly ICarContext _carContext;
        private readonly IServiceProvider _services;

        private int _changeRequired;
        private int _checkRequired;

        public override Menus MenuId => Menus.교환주기;

        public TermListViewModel(ITermManager manager, ICarContext carContext,
            IDialogService dialog, IMessenger messenger, IServiceProvider services)
            : base(dialog, messenger)
        {
            _manager = manager;
            _carContext = carContext;
            _services = services;
            Title = "교환주기";
            Messenger.Register(this);
        }

        protected override void ConfigureButtons(FunctionButtonState buttons)
        {
            // 원본 frmTermList 와 같이 추가/복사/삭제/인쇄는 없다. 행(소모품)은 소모품 화면에서 관리한다.
            buttons.InsertEnabled = false;
            buttons.DeleteEnabled = false;
            buttons.PrintEnabled = false;
            buttons.Custom1Visible = true;
            buttons.Custom1Text = "교체이력 (F1)";
        }

        public void Receive(CarChangedMessage message) => _ = RefreshAsync();

        protected override async Task<List<TermStatusInfo>> LoadItemsAsync()
        {
            int carNum = _carContext.SelectedCarNum;
            if (carNum <= 0)
            {
                _changeRequired = 0;
                _checkRequired = 0;
                return new List<TermStatusInfo>();
            }

            // 소모품 화면과 같은 순서(등록순)로 두어 두 화면의 행이 서로 대응되게 한다.
            var list = (await _manager.GetListAsync(carNum))
                .OrderBy(x => x.car_consumables_num)
                .ToList();

            _changeRequired = list.Count(x => x.Status == TermStatus.교환필요);
            _checkRequired = list.Count(x => x.Status == TermStatus.확인필요);
            return list;
        }

        protected override string GetStatusText()
            => $"{Title} - {Items.Count}건, 교환필요 {_changeRequired}건, 확인필요 {_checkRequired}건";

        protected override string GetSearchText(TermStatusInfo item)
            => $"{item.name} {item.Status}";

        protected override Task InsertAsync() => Task.CompletedTask;

        /// <summary>수정(F8)은 원본과 같이 이력 관리 창을 연다.</summary>
        protected override Task UpdateAsync(TermStatusInfo item) => OpenHistoryAsync(item);

        protected override Task DeleteAsync(TermStatusInfo item) => Task.CompletedTask;

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
            await OpenHistoryAsync(SelectedItem);
        }

        private async Task OpenHistoryAsync(TermStatusInfo item)
        {
            var vm = _services.GetRequiredService<ConsumableHistoryViewModel>();
            if (await vm.LoadAsync(item.car_consumables_num) == false)
                return;
            Dialog.ShowDialog(vm);
            // 이력 연결/해제는 창 안에서 바로 저장되므로 닫힌 뒤 남은 주기·거리를 다시 계산한다.
            await RefreshAsync();
        }
    }
}
