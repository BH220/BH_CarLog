using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;

namespace BH_CarLog.Api.Interface
{
    /// <summary>
    /// 차량 - /api/carlog/car/*
    /// </summary>
    public interface ICarManager
    {
        /// <summary>목록. 차량 선택의 원천이라 car_num 을 받지 않는다. (POST /api/carlog/car/list → ResCarList)</summary>
        Task<List<CarInfo>> GetListAsync();

        /// <summary>상세 (POST /api/carlog/car/detail → ResCarDetail)</summary>
        Task<CarInfo?> GetAsync(int carNum);

        /// <summary>등록/수정 (POST /api/carlog/car/update → ResCarSave)</summary>
        Task<(bool Success, string Message)> SaveAsync(CarInfo model);

        /// <summary>삭제 - 사용불가 처리 (POST /api/carlog/car/status)</summary>
        Task<(bool Success, string Message)> DeleteAsync(int carNum);

        /// <summary>사용상태 일괄 변경 (POST /api/carlog/car/status → ResStatusChange)</summary>
        Task<(bool Success, string Message)> ChangeStatusAsync(IEnumerable<int> carNums, CdStatus status);
    }
}
