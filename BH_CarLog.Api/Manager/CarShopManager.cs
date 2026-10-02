using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Core.Utils;

namespace BH_CarLog.Api.Manager
{
    /// <summary>
    /// 가게 API (/api/carlog/shop/*).
    /// 저장 결과의 Success 가 true 인데 Message 가 있으면 "본문은 저장됐지만 일부 이미지 처리 실패" 경고다.
    /// 조회 실패는 <see cref="ApiException"/> 으로 알린다.
    /// </summary>
    public class CarShopManager : ICarShopManager
    {
        public async Task<List<CarShopInfo>> GetListAsync()
        {
            ResCarShopList res = await CarLogApi.Instance.PostResponseAsync<ResCarShopList>("/api/carlog/shop/list");
            return res.EnsureSuccess("가게 목록").list.Where(x => x.status == CdStatus.사용).ToList();
        }

        public async Task<CarShopInfo?> GetAsync(int carShopNum)
        {
            var param = new Dictionary<string, string> { ["car_shop_num"] = carShopNum.ToStringEx() };
            ResCarShopDetail res = await CarLogApi.Instance.PostResAsync<ResCarShopDetail>("/api/carlog/shop/detail", param);
            return res.EnsureSuccess("가게 상세").detail;
        }

        public async Task<(bool Success, string Message)> InsertAsync(CarShopInfo model)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            if (model.car_shop_num > 0)
                param.Add("car_shop_num", model.car_shop_num.ToStringEx());
            param.Add("name", model.name);
            param.Add("business_no", model.business_no);
            param.Add("ceo", model.ceo);
            param.Add("address", model.address);
            param.Add("tel", model.tel);
            param.Add("tel2", model.tel2);
            param.Add("shop_type", ((int)model.shop_type).ToStringEx());
            param.Add("note", model.note);

            ResCarShopSave save = await CarLogApi.Instance.PostResAsync<ResCarShopSave>("/api/carlog/shop/update", param);
            if (save.result != 1)
                return (false, save.msg);

            int carShopNum = save.car_shop_num > 0 ? save.car_shop_num : model.car_shop_num;
            model.car_shop_num = carShopNum;

            if (model.images != null && model.images.Count > 0)
            {
                ResBase imgResult = await CarLogApi.Instance.AddImages<ResCarShopImageAdd>("/api/carlog/shop/image/add", "car_shop_num", carShopNum, model.images);
                return (imgResult.result == 1, imgResult.msg);
            }
            return (true, save.msg);
        }

        public async Task<(bool Success, string Message)> UpdateAsync(CarShopInfo model, List<int> deleteImageIds)
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
                param.Add("car_shop_image_num", nums);
                ResImageDelete res = await CarLogApi.Instance.PostResAsync<ResImageDelete>("/api/carlog/shop/image/del", param);
                if (res.result != 1)
                    errors.Add($"이미지 삭제({nums}): {res.msg}");
            }
            return (true, errors.Count == 0 ? "" : string.Join("\r\n", errors));
        }

        public Task<(bool Success, string Message)> DeleteAsync(int carShopNum)
            => ChangeStatusAsync(new[] { carShopNum }, CdStatus.사용불가);

        public async Task<(bool Success, string Message)> ChangeStatusAsync(IEnumerable<int> carShopNums, CdStatus status)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param.Add("status", ((int)status).ToStringEx());
            param.Add("car_shop_num_list", string.Join(",", carShopNums));
            ResStatusChange res = await CarLogApi.Instance.PostResAsync<ResStatusChange>("/api/carlog/shop/status", param);
            return (res.result == 1, res.msg);
        }
    }
}
