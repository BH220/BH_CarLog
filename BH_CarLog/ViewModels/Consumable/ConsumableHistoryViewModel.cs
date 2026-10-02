using System.Collections.ObjectModel;
using BH_CarLog.Api;
using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using BH_CarLog.ViewModels.Maintenance;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace BH_CarLog.ViewModels.Consumable
{
    /// <summary>
    /// 소모품 교체이력 (기존 frmSelectFixedList + 교체 이력 보기).
    /// 소모품 항목과 유지보수 기록을 이어 "언제 무엇을 갈았는지" 를 남긴다.
    /// - 연결 후보에는 이 차의 유지보수 기록 중 이 소모품에 아직 이어지지 않은 것을 보여 준다.
    ///   한 번의 정비로 여러 소모품을 갈 수 있으므로 다른 소모품에 이어 둔 기록은 그대로 후보에 남긴다.
    ///   연결을 해제하면 그 기록은 다시 후보로 돌아온다.
    /// - 행 더블클릭은 유지보수 기록의 내용을 상세(읽기 전용)로 연다. (기존 frmFixedInfo.JustViewer)
    /// </summary>
    public partial class ConsumableHistoryViewModel : DialogViewModelBase
    {
        private readonly IConsumableManager _manager;
        private readonly IMaintenanceManager _maintenance;
        private readonly ICarShopManager _shops;
        private readonly IDialogService _dialog;
        private readonly IServiceProvider _services;

        private int _consumablesNum;
        private int _carNum;

        /// <summary>연결된 교체 이력</summary>
        public ObservableCollection<ConsumableHistoryInfo> Histories { get; } = new();

        /// <summary>아직 이 소모품에 연결되지 않은 이 차의 유지보수 기록 (연결 후보)</summary>
        public ObservableCollection<MaintenanceInfo> Candidates { get; } = new();

        [ObservableProperty] private string _consumableName = "";
        [ObservableProperty] private string _cycleText = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UnlinkCommand), nameof(ShowHistoryDetailCommand))]
        private ConsumableHistoryInfo? _selectedHistory;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LinkCommand), nameof(ShowCandidateDetailCommand))]
        private MaintenanceInfo? _selectedCandidate;

        public ConsumableHistoryViewModel(IConsumableManager manager, IMaintenanceManager maintenance,
            ICarShopManager shops, IDialogService dialog, IServiceProvider services)
        {
            _manager = manager;
            _maintenance = maintenance;
            _shops = shops;
            _dialog = dialog;
            _services = services;
            Title = "소모품 교체이력";
        }

        public async Task<bool> LoadAsync(int consumablesNum)
        {
            _consumablesNum = consumablesNum;
            try
            {
                var info = await _manager.GetHistoryAsync(consumablesNum);
                if (info == null)
                {
                    _dialog.ShowError("교체이력 조회에 실패했습니다.");
                    return false;
                }

                _carNum = info.car_num;
                ConsumableName = info.name;
                CycleText = $"관리 주기: {info.TermText} / {info.DistanceText}";
                Title = $"소모품 교체이력 - {info.name}";

                await FillAsync(info);
                return true;
            }
            catch (ApiException ex)
            {
                _dialog.ShowError($"교체이력 조회에 실패했습니다.\r\n{ex.Message}");
                return false;
            }
        }

        /// <summary>연결/해제 뒤 두 목록을 다시 채운다. 실패해도 창은 유지한다.</summary>
        private async Task RefreshAsync()
        {
            try
            {
                var info = await _manager.GetHistoryAsync(_consumablesNum);
                if (info == null)
                    return;
                await FillAsync(info);
            }
            catch (ApiException ex)
            {
                _dialog.ShowError($"교체이력을 다시 불러오지 못했습니다.\r\n{ex.Message}");
            }
        }

        private async Task FillAsync(ConsumableInfo info)
        {
            var shops = (await _shops.GetListAsync()).ToDictionary(x => x.car_shop_num, x => x.name);
            var all = await _maintenance.GetListAsync(_carNum);
            var maintenanceAt = all.ToDictionary(x => x.car_maintenance_num, x => x.mataintenance_at);

            // 교체이력 응답에 유지보수일(mataintenance_at)이 없으면 유지보수 목록에서 맞춰 채운다.
            foreach (var history in info.history)
            {
                if (history.mataintenance_at == null && maintenanceAt.TryGetValue(history.car_maintenance_num, out var at))
                    history.mataintenance_at = at;
            }

            // 다른 목록과 같이 유지보수일 오름차순. 최근 교체가 맨 아래로 간다.
            Histories.Clear();
            foreach (var history in info.history.OrderBy(x => x.MaintenanceAt).ThenBy(x => x.mileage).ThenBy(x => x.car_maintenance_num))
            {
                history.ShopName = shops.TryGetValue(history.car_shop_num, out var name) ? name : "";
                Histories.Add(history);
            }
            SelectedHistory = Histories.LastOrDefault();

            // 연결 후보: 같은 차의 유지보수 기록 중 이 소모품의 교체이력이 아닌 것.
            // 한 번의 정비로 여러 소모품(엔진오일 + 오일필터 등)을 갈 수 있으므로 다른 소모품에 이어 둔 기록은 빼지 않는다.
            // 연결을 해제하면 이력에서 빠지므로 다음 갱신 때 다시 후보에 나타난다.
            var linked = info.history.Select(x => x.car_maintenance_num).ToHashSet();

            Candidates.Clear();
            foreach (var item in all.Where(x => x.car_num == _carNum && linked.Contains(x.car_maintenance_num) == false)
                                    .OrderBy(x => x.mataintenance_at)
                                    .ThenBy(x => x.mileage)
                                    .ThenBy(x => x.car_maintenance_num))
            {
                item.ShopName = shops.TryGetValue(item.car_shop_num, out var name) ? name : "";
                Candidates.Add(item);
            }
            SelectedCandidate = Candidates.LastOrDefault();
        }

        private bool CanLink() => SelectedCandidate != null;

        /// <summary>선택한 유지보수 기록을 이 소모품의 교체이력으로 연결한다.</summary>
        [RelayCommand(CanExecute = nameof(CanLink))]
        private async Task LinkAsync()
        {
            if (SelectedCandidate == null)
                return;

            IsBusy = true;
            try
            {
                var (success, message) = await _manager.AddPairAsync(_consumablesNum, SelectedCandidate.car_maintenance_num);
                if (success == false)
                {
                    _dialog.ShowError(string.IsNullOrEmpty(message) ? "교체이력 연결에 실패했습니다." : message);
                    return;
                }
                await RefreshAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanUnlink() => SelectedHistory != null;

        /// <summary>잘못 이은 이력을 끊는다. (유지보수 기록 자체는 지우지 않고, 연결 후보로 되돌아간다)</summary>
        [RelayCommand(CanExecute = nameof(CanUnlink))]
        private async Task UnlinkAsync()
        {
            if (SelectedHistory == null)
                return;
            if (_dialog.Confirm("선택한 교체이력 연결을 해제하시겠습니까?\r\n유지보수 기록 자체는 남고, 다시 연결할 수 있는 목록으로 돌아갑니다.", "연결 해제") == false)
                return;

            IsBusy = true;
            try
            {
                var (success, message) = await _manager.DeletePairAsync(new[] { SelectedHistory.car_consumables_history_num });
                if (success == false)
                {
                    _dialog.ShowError(string.IsNullOrEmpty(message) ? "연결 해제에 실패했습니다." : message);
                    return;
                }
                await RefreshAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>교체 이력 행의 유지보수 기록 내용을 상세로 본다. (행 더블클릭 / 상세 버튼)</summary>
        [RelayCommand(CanExecute = nameof(CanUnlink))]
        private Task ShowHistoryDetailAsync()
            => SelectedHistory == null ? Task.CompletedTask : OpenDetailAsync(SelectedHistory.car_maintenance_num);

        /// <summary>연결 후보 행의 유지보수 기록 내용을 상세로 본다. (행 더블클릭 / 상세 버튼)</summary>
        [RelayCommand(CanExecute = nameof(CanLink))]
        private Task ShowCandidateDetailAsync()
            => SelectedCandidate == null ? Task.CompletedTask : OpenDetailAsync(SelectedCandidate.car_maintenance_num);

        /// <summary>유지보수 창을 읽기 전용(상세 보기)으로 연다. 여기서는 내용 확인만 하고 수정은 유지보수 화면에서 한다.</summary>
        private async Task OpenDetailAsync(int maintenanceNum)
        {
            var vm = _services.GetRequiredService<MaintenanceEditViewModel>();
            if (await vm.LoadAsync(maintenanceNum, readOnly: true) == false)
                return;
            _dialog.ShowDialog(vm);
        }
    }
}
