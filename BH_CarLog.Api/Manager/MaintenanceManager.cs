using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Core.Utils;

namespace BH_CarLog.Api.Manager
{
    /// <summary>
    /// 유지보수 API (/api/carlog/maintenance/*). 정비·구매·세차·공기압을 유형으로 구분해 한 화면에서 다룬다.
    /// 조회 실패는 <see cref="ApiException"/> 으로 알린다.
    /// </summary>
    public class MaintenanceManager : IMaintenanceManager
    {
        public async Task<List<MaintenanceInfo>> GetListAsync(int carNum)
        {
            var param = new Dictionary<string, string> { ["car_num"] = carNum.ToStringEx() };
            ResMaintenanceList res = await CarLogApi.Instance.PostResAsync<ResMaintenanceList>("/api/carlog/maintenance/list", param);
            return res.EnsureSuccess("유지보수 목록").list.Where(x => x.status == CdStatus.사용).ToList();
        }

        public async Task<MaintenanceInfo?> GetAsync(int maintenanceNum)
        {
            var param = new Dictionary<string, string> { ["car_maintenance_num"] = maintenanceNum.ToStringEx() };
            ResMaintenanceDetail res = await CarLogApi.Instance.PostResAsync<ResMaintenanceDetail>("/api/carlog/maintenance/detail", param);
            return res.EnsureSuccess("유지보수 상세").detail;
        }

        public async Task<(bool Success, string Message)> InsertAsync(MaintenanceInfo model)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            if (model.car_maintenance_num > 0)
                param.Add("car_maintenance_num", model.car_maintenance_num.ToStringEx());
            param.Add("car_num", model.car_num.ToStringEx());
            param.Add("car_shop_num", model.car_shop_num.ToStringEx());
            // 유지보수일은 서버 필수값. 주유의 refuel_at 과 같은 yyyy-MM-dd 형식으로 보낸다.
            param.Add("mataintenance_at", model.mataintenance_at.HasValue ? model.mataintenance_at.Value.ToString("yyyy-MM-dd") : "");
            param.Add("mataintenance_type", ((int)model.mataintenance_type).ToStringEx());
            param.Add("mileage", model.mileage.ToStringEx());
            param.Add("amount", model.amount.ToStringEx());
            param.Add("note", model.note);

            ResMaintenanceSave save = await CarLogApi.Instance.PostResAsync<ResMaintenanceSave>("/api/carlog/maintenance/update", param);
            if (save.result != 1)
                return (false, save.msg);

            int maintenanceNum = save.car_maintenance_num > 0 ? save.car_maintenance_num : model.car_maintenance_num;
            model.car_maintenance_num = maintenanceNum;

            if (model.images != null && model.images.Count > 0)
            {
                ResBase imgResult = await CarLogApi.Instance.AddImages<ResMaintenanceImageAdd>("/api/carlog/maintenance/image/add", "car_maintenance_num", maintenanceNum, model.images);
                return (imgResult.result == 1, imgResult.msg);
            }
            return (true, save.msg);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(MaintenanceInfo model, List<int> deleteImageIds)
        {
            var (success, message) = await InsertAsync(model);
            if (success == false)
                return (false, message);

            var errors = new List<string>();
            if (string.IsNullOrEmpty(message) == false)
                errors.Add(message);

            if (deleteImageIds != null && deleteImageIds.Count > 0)
            {
                Dictionary<string, string> param = new Dictionary<string, string>();
                string nums = string.Join(",", deleteImageIds);
                param.Add("car_maintenance_image_num", nums);
                ResImageDelete res = await CarLogApi.Instance.PostResAsync<ResImageDelete>("/api/carlog/maintenance/image/del", param);
                if (res.result != 1)
                    errors.Add($"이미지 삭제({nums}): {res.msg}");
            }
            return (true, errors.Count == 0 ? "" : string.Join("\r\n", errors));
        }

        public Task<(bool Success, string Message)> DeleteAsync(int maintenanceNum)
            => ChangeStatusAsync(new[] { maintenanceNum }, CdStatus.사용불가);

        public async Task<(bool Success, string Message)> ChangeStatusAsync(IEnumerable<int> maintenanceNums, CdStatus status)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("status", ((int)status).ToStringEx());
            param.Add("car_maintenance_num_list", string.Join(",", maintenanceNums));
            ResStatusChange res = await CarLogApi.Instance.PostResAsync<ResStatusChange>("/api/carlog/maintenance/status", param);
            return (res.result == 1, res.msg);
        }
    }
}
