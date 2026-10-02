using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Core.Utils;

namespace BH_CarLog.Api.Manager
{
    /// <summary>
    /// 소모품 API (/api/carlog/consumables/*).
    /// 항목은 "무엇을 얼마 주기로 관리하는가" 만 담고, 실제 교체는 유지보수 기록과 이어 이력으로 남긴다.
    /// 조회 실패는 <see cref="ApiException"/> 으로 알린다.
    /// </summary>
    public class ConsumableManager : IConsumableManager
    {
        public async Task<List<ConsumableInfo>> GetListAsync(int carNum)
        {
            var param = new Dictionary<string, string> { ["car_num"] = carNum.ToStringEx() };
            ResConsumableList res = await CarLogApi.Instance.PostResAsync<ResConsumableList>("/api/carlog/consumables/list", param);
            return res.EnsureSuccess("소모품 목록").list.Where(x => x.status == CdStatus.사용).ToList();
        }

        public async Task<ConsumableInfo?> GetAsync(int consumablesNum)
        {
            var param = new Dictionary<string, string> { ["car_consumables_num"] = consumablesNum.ToStringEx() };
            ResConsumableDetail res = await CarLogApi.Instance.PostResAsync<ResConsumableDetail>("/api/carlog/consumables/detail", param);
            return res.EnsureSuccess("소모품 상세").detail;
        }

        public async Task<(bool Success, string Message)> SaveAsync(ConsumableInfo model)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            if (model.car_consumables_num > 0)
                param.Add("car_consumables_num", model.car_consumables_num.ToStringEx());
            param.Add("car_num", model.car_num.ToStringEx());
            param.Add("name", model.name);
            // term/distance 는 NULL 허용이라 빈 값으로 보내면 "주기 없음" 이 된다. 0 은 값이지 없음이 아니다.
            param.Add("term", model.term.HasValue ? model.term.Value.ToStringEx() : "");
            param.Add("distance", model.distance.HasValue ? model.distance.Value.ToStringEx() : "");

            ResConsumableSave res = await CarLogApi.Instance.PostResAsync<ResConsumableSave>("/api/carlog/consumables/update", param);
            if (res.result == 1 && res.car_consumables_num > 0)
                model.car_consumables_num = res.car_consumables_num;
            return (res.result == 1, res.msg);
        }

        public Task<(bool Success, string Message)> DeleteAsync(int consumablesNum)
            => ChangeStatusAsync(new[] { consumablesNum }, CdStatus.사용불가);

        public async Task<(bool Success, string Message)> ChangeStatusAsync(IEnumerable<int> consumablesNums, CdStatus status)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("status", ((int)status).ToStringEx());
            param.Add("car_consumables_num_list", string.Join(",", consumablesNums));
            ResStatusChange res = await CarLogApi.Instance.PostResAsync<ResStatusChange>("/api/carlog/consumables/status", param);
            return (res.result == 1, res.msg);
        }

        public async Task<ConsumableInfo?> GetHistoryAsync(int consumablesNum)
        {
            var param = new Dictionary<string, string> { ["car_consumables_num"] = consumablesNum.ToStringEx() };
            ResConsumableHistory res = await CarLogApi.Instance.PostResAsync<ResConsumableHistory>("/api/carlog/consumables/history", param);
            return res.EnsureSuccess("소모품 교체이력").detail;
        }

        public async Task<(bool Success, string Message)> AddPairAsync(int consumablesNum, int maintenanceNum)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("car_consumables_num", consumablesNum.ToStringEx());
            param.Add("car_maintenance_num", maintenanceNum.ToStringEx());
            ResConsumablePairAdd res = await CarLogApi.Instance.PostResAsync<ResConsumablePairAdd>("/api/carlog/consumables/pairing/add", param);
            return (res.result == 1, res.msg);
        }

        public async Task<(bool Success, string Message)> DeletePairAsync(IEnumerable<int> historyNums)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("car_consumables_history_num", string.Join(",", historyNums));
            ResConsumablePairDelete res = await CarLogApi.Instance.PostResAsync<ResConsumablePairDelete>("/api/carlog/consumables/pairing/del", param);
            return (res.result == 1, res.msg);
        }
    }
}
