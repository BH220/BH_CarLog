using BH_CarLog.Api;
using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Core.Constants;
using BH_CarLog.Messages;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using BH_CarLog.ViewModels.Common;
using BH_CarLog.ViewModels.Fuel;
using BH_CarLog.ViewModels.Maintenance;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BH_CarLog.ViewModels.NewMessage
{
    /// <summary>
    /// 새 메시지 목록 (기존 frmNewMessageList).
    /// 웹 바로등록(/direct/carlog/{token}) 으로 쌓인 임시 데이터를 보여주고 실데이터로 전환한다.
    /// - 수정(F8)·더블클릭: 상세 창 (내용·사진 확인, 거기서도 전환 가능)
    /// - 실데이터 전환(F1): 유형에 맞는 주유/유지보수 입력 창에 값과 사진을 채워 띄우고, 저장되면 realize 로 전환 표시
    /// 데이터가 직접 만들어지는 화면이 아니라 추가·복사는 없다.
    /// </summary>
    public partial class NewMessageListViewModel : ListViewModelBase<NewMessageInfo>, IRecipient<CarChangedMessage>
    {
        private readonly INewMessageManager _manager;
        private readonly ICarContext _carContext;
        private readonly IServiceProvider _services;

        public override Menus MenuId => Menus.새메시지;

        public NewMessageListViewModel(INewMessageManager manager, ICarContext carContext,
            IDialogService dialog, IMessenger messenger, IServiceProvider services)
            : base(dialog, messenger)
        {
            _manager = manager;
            _carContext = carContext;
            _services = services;
            Title = "새 메시지";
            Messenger.Register(this);
        }

        protected override void ConfigureButtons(FunctionButtonState buttons)
        {
            // 웹에서 들어오는 데이터라 이 화면에서 새로 만들지 않는다. (기존 frmNewMessageList 와 동일)
            buttons.InsertEnabled = false;
            buttons.Custom1Visible = true;
            buttons.Custom1Text = "실데이터 전환 (F1)";
        }

        public void Receive(CarChangedMessage message) => _ = RefreshAsync();

        protected override async Task<List<NewMessageInfo>> LoadItemsAsync()
        {
            int carNum = _carContext.SelectedCarNum;
            if (carNum <= 0)
                return new List<NewMessageInfo>();

            // 조회는 모두 선택된 차량 기준이다. (서버가 car_num 을 필수로 받는다)
            var all = await _manager.GetListAsync(carNum);
            // 등록일 오름차순. 최근에 들어온 메시지가 맨 아래로 간다.
            var list = all.Where(x => x.car_num == carNum)
                          .OrderBy(x => x.input_at)
                          .ThenBy(x => x.recoard_num)
                          .ToList();
            foreach (var item in list)
                item.CarName = _carContext.GetCarName(item.car_num);
            return list;
        }

        protected override string GetSearchText(NewMessageInfo item)
            => $"{item.input_at:yyyy-MM-dd} {item.recoard_type} {item.mileage} {item.note}";

        protected override Task InsertAsync() => Task.CompletedTask;

        /// <summary>수정(F8)·더블클릭은 상세 창을 연다 (기존 frmNewMessageInfo). 거기서 "실데이터 전환" 을 누르면 이어서 전환한다.</summary>
        protected override async Task UpdateAsync(NewMessageInfo item)
        {
            var vm = _services.GetRequiredService<NewMessageDetailViewModel>();
            if (await vm.LoadAsync(item.recoard_num) == false)
                return;
            if (Dialog.ShowDialog(vm) == true && vm.TransferRequested && vm.Info != null)
                await TransferItemAsync(vm.Info);
        }

        protected override async Task DeleteAsync(NewMessageInfo item)
        {
            if (_manager.IsDeleteSupported == false)
            {
                Dialog.ShowInfo("새 메시지를 버리는 기능은 아직 서버에 없습니다.\r\n(처리 상태 코드에 \"버림\" 이 없습니다) 서버 연동이 끝나면 사용할 수 있습니다.", "구현 필요");
                return;
            }

            var (success, message) = await _manager.DeleteAsync(item.recoard_num);
            if (success)
                await RefreshAsync();
            else
                Dialog.ShowError(message);
        }

        protected override Task CustomFunctionAsync(string functionId)
            => functionId == Functions.사용자기능1 ? TransferCommand.ExecuteAsync(null) : Task.CompletedTask;

        /// <summary>
        /// 선택한 임시 데이터를 실데이터로 옮긴다. 상단 F1 버튼과 우클릭 메뉴가 쓴다.
        /// 목록은 오래 열려 있을 수 있어, 다른 곳에서 이미 옮겼는지 상세(detail)를 다시 받아 확인한 뒤 진행한다.
        /// </summary>
        [RelayCommand]
        private async Task TransferAsync()
        {
            var item = SelectedItem;
            if (item == null)
            {
                Dialog.ShowWarning("전환할 자료가 선택되지 않았습니다.");
                return;
            }
            // 다른 차의 데이터를 지금 고른 차로 넣으면 기록이 뒤섞인다. (기존 동작 유지)
            if (item.car_num != _carContext.SelectedCarNum)
            {
                Dialog.ShowError("해당 데이터는 현재 선택된 차량에서 처리할 수 없는 데이터입니다.");
                return;
            }
            NotifyActivity();

            NewMessageInfo? fresh;
            try
            {
                fresh = await _manager.GetAsync(item.recoard_num);
            }
            catch (ApiException ex)
            {
                Dialog.ShowError($"새 메시지 조회에 실패했습니다.\r\n{ex.Message}");
                return;
            }
            if (fresh == null)
            {
                Dialog.ShowWarning("새 메시지를 찾을 수 없습니다. 목록을 다시 불러옵니다.");
                await RefreshAsync();
                return;
            }
            await TransferItemAsync(fresh);
        }

        /// <summary>
        /// 전환 흐름 (기존 frmNewMessageInfo.OnSelectBtnClick):
        /// 유형에 맞는 입력 창에 차량·날짜·주행거리·내용과 사진을 채워 띄운다 → 저장되면 realize 로 "어디로 옮겼는지" 를 남긴다.
        /// </summary>
        private async Task TransferItemAsync(NewMessageInfo item)
        {
            if (item.transfer_type != CdTransfer.전환대기)
            {
                Dialog.ShowWarning("이미 실데이터로 전환된 메시지입니다.");
                await RefreshAsync();
                return;
            }

            var (saved, transferTo, transferNum) = item.recoard_type == CdMaintenanceType.주유
                ? await TransferToFuelAsync(item)
                : await TransferToMaintenanceAsync(item);
            if (saved == false)
                return;

            var (success, message) = await _manager.RealizeAsync(item.recoard_num, transferTo, transferNum);
            if (success == false)
            {
                // 실데이터는 저장됐는데 전환 표시만 실패한 경우다. 목록에 남으니 사용자가 알아야 한다. (다시 전환하면 중복 저장이 된다)
                Dialog.ShowWarning($"실데이터는 저장했지만 전환 처리에 실패했습니다.\r\n{message}");
            }
            await RefreshAsync();
        }

        private async Task<(bool Saved, CdMaintenanceType TransferTo, int TransferNum)> TransferToFuelAsync(NewMessageInfo item)
        {
            var vm = _services.GetRequiredService<FuelEditViewModel>();
            if (await vm.LoadAsync(null) == false)
                return (false, default, 0);
            vm.Prefill(item.car_num, item.input_at, item.mileage, item.note);
            await CopyImagesAsync(item, vm.Images);

            if (Dialog.ShowDialog(vm) != true)
                return (false, default, 0);
            if (vm.LastSavedNum <= 0)
            {
                Dialog.ShowWarning("주유기록은 저장됐지만 기록 번호를 받지 못해 전환 표시를 하지 못했습니다.");
                return (false, default, 0);
            }
            return (true, CdMaintenanceType.주유, vm.LastSavedNum);
        }

        private async Task<(bool Saved, CdMaintenanceType TransferTo, int TransferNum)> TransferToMaintenanceAsync(NewMessageInfo item)
        {
            var vm = _services.GetRequiredService<MaintenanceEditViewModel>();
            if (await vm.LoadAsync(null) == false)
                return (false, default, 0);
            vm.Prefill(item.car_num, item.recoard_type, item.input_at, item.mileage, item.note);
            await CopyImagesAsync(item, vm.Images);

            if (Dialog.ShowDialog(vm) != true)
                return (false, default, 0);
            if (vm.LastSavedNum <= 0)
            {
                Dialog.ShowWarning("유지보수 기록은 저장됐지만 기록 번호를 받지 못해 전환 표시를 하지 못했습니다.");
                return (false, default, 0);
            }
            // 입력 창에서 유형을 바꿔 저장했을 수 있으므로 실제 저장된 유형을 남긴다.
            return (true, vm.LastSavedType, vm.LastSavedNum);
        }

        /// <summary>
        /// 웹에서 올린 영수증 사진을 실데이터의 신규 첨부로 복사한다. (기존 frmNewMessageInfo 가 SQ_IMAGES 를 넘기던 것)
        /// 내려받지 못한 사진은 건너뛰고 알린다. 사진 때문에 전환 자체를 막지는 않는다. 입력 창에서 빼거나 더할 수 있다.
        /// </summary>
        private async Task CopyImagesAsync<TImage>(NewMessageInfo item, ImageGridViewModel<TImage> target) where TImage : ImageInfo, new()
        {
            var failed = new List<string>();
            foreach (var image in item.images)
            {
                var (success, message) = await target.AddCopyAsync(image);
                if (success == false)
                    failed.Add($"{image.FileName}: {message}");
            }
            if (failed.Count > 0)
                Dialog.ShowWarning("일부 사진을 가져오지 못했습니다. 저장 전에 확인하세요.\r\n" + string.Join("\r\n", failed));
        }
    }
}
