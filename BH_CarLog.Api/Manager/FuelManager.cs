using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Core.Utils;

namespace BH_CarLog.Api.Manager
{
    /// <summary>
    /// 주유기록 API (/api/carlog/fuel/*). CarShopManager 와 같은 구조.
    /// 조회 실패는 <see cref="ApiException"/> 으로 알린다.
    /// </summary>
    public class FuelManager : IFuelManager
    {
        public async Task<List<FuelInfo>> GetListAsync(int carNum)
        {
            var param = new Dictionary<string, string> { ["car_num"] = carNum.ToStringEx() };
            ResFuelList res = await CarLogApi.Instance.PostResAsync<ResFuelList>("/api/carlog/fuel/list", param);
            return res.EnsureSuccess("주유기록 목록").list.Where(x => x.status == CdStatus.사용).ToList();
        }

        public async Task<FuelInfo?> GetAsync(int fuelNum)
        {
            var param = new Dictionary<string, string> { ["car_fuel_history_num"] = fuelNum.ToStringEx() };
            ResFuelDetail res = await CarLogApi.Instance.PostResAsync<ResFuelDetail>("/api/carlog/fuel/detail", param);
            return res.EnsureSuccess("주유기록 상세").detail;
        }

        public async Task<(bool Success, string Message)> InsertAsync(FuelInfo model)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            if (model.car_fuel_history_num > 0)
                param.Add("car_fuel_history_num", model.car_fuel_history_num.ToStringEx());
            param.Add("car_num", model.car_num.ToStringEx());
            param.Add("car_shop_num", model.car_shop_num.ToStringEx());
            param.Add("refuel_at", model.refuel_at.HasValue ? model.refuel_at.Value.ToString("yyyy-MM-dd") : "");
            param.Add("mileage", model.mileage.ToStringEx());
            param.Add("amount", model.amount.ToStringEx());
            param.Add("amount_per_liter", model.amount_per_liter.ToStringEx());
            param.Add("fuel_type", ((int)model.fuel_type).ToStringEx());
            param.Add("note", model.note);

            ResFuelSave save = await CarLogApi.Instance.PostResAsync<ResFuelSave>("/api/carlog/fuel/update", param);
            if (save.result != 1)
                return (false, save.msg);

            int fuelNum = save.car_fuel_history_num > 0 ? save.car_fuel_history_num : model.car_fuel_history_num;
            model.car_fuel_history_num = fuelNum;

            if (model.images != null && model.images.Count > 0)
            {
                ResBase imgResult = await CarLogApi.Instance.AddImages<ResFuelImageAdd>("/api/carlog/fuel/image/add", "car_fuel_history_num", fuelNum, model.images);
                return (imgResult.result == 1, imgResult.msg);
            }
            return (true, save.msg);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(FuelInfo model, List<int> deleteImageIds)
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
                param.Add("car_fuel_history_image_num", nums);
                ResImageDelete res = await CarLogApi.Instance.PostResAsync<ResImageDelete>("/api/carlog/fuel/image/del", param);
                if (res.result != 1)
                    errors.Add($"이미지 삭제({nums}): {res.msg}");
            }
            return (true, errors.Count == 0 ? "" : string.Join("\r\n", errors));
        }

        public Task<(bool Success, string Message)> DeleteAsync(int fuelNum)
            => ChangeStatusAsync(new[] { fuelNum }, CdStatus.사용불가);

        public async Task<(bool Success, string Message)> ChangeStatusAsync(IEnumerable<int> fuelNums, CdStatus status)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("status", ((int)status).ToStringEx());
            param.Add("car_fuel_history_num_list", string.Join(",", fuelNums));
            ResStatusChange res = await CarLogApi.Instance.PostResAsync<ResStatusChange>("/api/carlog/fuel/status", param);
            return (res.result == 1, res.msg);
        }
    }
}
