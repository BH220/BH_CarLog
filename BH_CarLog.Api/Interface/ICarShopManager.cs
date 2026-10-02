using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;

namespace BH_CarLog.Api.Interface
{
    /// <summary>
    /// 가게 - /api/carlog/shop/*
    /// </summary>
    public interface ICarShopManager
    {
        /// <summary>목록. 가게는 차와 연결이 없어 car_num 을 받지 않는다. (POST /api/carlog/shop/list → ResCarShopList)</summary>
        Task<List<CarShopInfo>> GetListAsync();

        /// <summary>상세. 이미지 포함 (POST /api/carlog/shop/detail → ResCarShopDetail)</summary>
        Task<CarShopInfo?> GetAsync(int carShopNum);

        /// <summary>등록 (POST /api/carlog/shop/update + image/add)</summary>
        Task<(bool Success, string Message)> InsertAsync(CarShopInfo model);

        /// <summary>수정 (POST /api/carlog/shop/update + image/add, image/del)</summary>
        Task<(bool Success, string Message)> UpdateAsync(CarShopInfo model, List<int> deleteImageIds);

        /// <summary>삭제 - 사용불가 처리</summary>
        Task<(bool Success, string Message)> DeleteAsync(int carShopNum);

        /// <summary>사용상태 일괄 변경</summary>
        Task<(bool Success, string Message)> ChangeStatusAsync(IEnumerable<int> carShopNums, CdStatus status);
    }
}
