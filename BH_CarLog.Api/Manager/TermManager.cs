using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;

namespace BH_CarLog.Api.Manager
{
    /// <summary>
    /// 교환주기 계산. (기존 frmTermList 가 쓰던 SELECT_CHANGE_PERIOD_DETAILS 의 클라이언트 구현)
    /// 소모품 수만큼 교체이력 API 를 부른다. 차 한 대의 소모품은 많지 않아 그대로 둔다.
    /// </summary>
    public class TermManager : ITermManager
    {
        private readonly IConsumableManager _consumables;
        private readonly IFuelManager _fuels;
        private readonly IMaintenanceManager _maintenance;

        public TermManager(IConsumableManager consumables, IFuelManager fuels, IMaintenanceManager maintenance)
        {
            _consumables = consumables;
            _fuels = fuels;
            _maintenance = maintenance;
        }

        public async Task<List<TermStatusInfo>> GetListAsync(int carNum)
        {
            var consumables = await _consumables.GetListAsync(carNum);
            if (consumables.Count == 0)
                return new List<TermStatusInfo>();

            // 차량의 현재 주행거리 = 주유·유지보수 기록 중 가장 큰 적산 거리 (원본: BHR_OilHistory + BHR_FixedHistory 의 MAX)
            var fuels = await _fuels.GetListAsync(carNum);
            var maintenances = await _maintenance.GetListAsync(carNum);
            int currentMileage = Math.Max(
                fuels.Count == 0 ? 0 : fuels.Max(x => x.mileage),
                maintenances.Count == 0 ? 0 : maintenances.Max(x => x.mileage));
            var maintenanceAt = maintenances.ToDictionary(x => x.car_maintenance_num, x => x.mataintenance_at);

            DateTime today = DateTime.Today;
            var result = new List<TermStatusInfo>(consumables.Count);
            foreach (var consumable in consumables)
            {
                var detail = await _consumables.GetHistoryAsync(consumable.car_consumables_num);
                // 사용불가 처리된 유지보수 기록에 이어진 이력은 뺀다. (원본: FL_DELETE = '0')
                var history = (detail?.history ?? new List<ConsumableHistoryInfo>())
                    .Where(x => x.maintenance_status != CdStatus.사용불가)
                    .ToList();
                // 최근 교환일은 유지보수일(mataintenance_at) 기준. history 응답에 없으면 유지보수 목록에서 맞춰 채운다.
                foreach (var item in history)
                {
                    if (item.mataintenance_at == null && maintenanceAt.TryGetValue(item.car_maintenance_num, out var at))
                        item.mataintenance_at = at;
                }
                result.Add(TermStatusInfo.Create(consumable, history, currentMileage, today));
            }
            return result;
        }
    }
}
