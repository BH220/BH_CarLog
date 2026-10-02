using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Core.Utils;

namespace BH_CarLog.Api.Manager
{
    /// <summary>
    /// 차량 API (/api/carlog/car/*).
    /// 차에는 첨부 이미지가 없고 삭제 API 도 없다. 삭제는 사용상태를 사용불가로 바꾸는 것이다.
    /// 조회 실패는 <see cref="ApiException"/> 으로 알린다. 빈 목록과 "조회 실패"를 화면이 구분해야 하기 때문이다.
    /// </summary>
    public class CarManager : ICarManager
    {
        public async Task<List<CarInfo>> GetListAsync()
        {
            ResCarList res = await CarLogApi.Instance.PostResponseAsync<ResCarList>("/api/carlog/car/list");
            return res.EnsureSuccess("차량 목록").list.Where(x => x.status == CdStatus.사용).ToList();
        }

        public async Task<CarInfo?> GetAsync(int carNum)
        {
            var param = new Dictionary<string, string> { ["car_num"] = carNum.ToStringEx() };
            ResCarDetail res = await CarLogApi.Instance.PostResAsync<ResCarDetail>("/api/carlog/car/detail", param);
            return res.EnsureSuccess("차량 상세").detail;
        }

        public async Task<(bool Success, string Message)> SaveAsync(CarInfo model)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            if (model.car_num > 0)
                param.Add("car_num", model.car_num.ToStringEx());
            param.Add("car_no", model.car_no);
            param.Add("car_name", model.car_name);
            param.Add("born_year", model.born_year);
            param.Add("vin", model.vin);
            param.Add("car_type", model.car_type);
            param.Add("fuel_type", ((int)model.fuel_type).ToStringEx());
            param.Add("cc", model.cc);
            param.Add("buy_at", model.buy_at.HasValue ? model.buy_at.Value.ToString("yyyy-MM-dd") : "");
            param.Add("note", model.note);

            ResCarSave res = await CarLogApi.Instance.PostResAsync<ResCarSave>("/api/carlog/car/update", param);
            if (res.result == 1 && res.car_num > 0)
                model.car_num = res.car_num;
            return (res.result == 1, res.msg);
        }

        public Task<(bool Success, string Message)> DeleteAsync(int carNum)
            => ChangeStatusAsync(new[] { carNum }, CdStatus.사용불가);

        public async Task<(bool Success, string Message)> ChangeStatusAsync(IEnumerable<int> carNums, CdStatus status)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("status", ((int)status).ToStringEx());
            param.Add("car_num_list", string.Join(",", carNums));
            ResStatusChange res = await CarLogApi.Instance.PostResAsync<ResStatusChange>("/api/carlog/car/status", param);
            return (res.result == 1, res.msg);
        }
    }
}
