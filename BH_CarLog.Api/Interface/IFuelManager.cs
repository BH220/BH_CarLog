using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;

namespace BH_CarLog.Api.Interface
{
    /// <summary>
    /// 주유기록 - /api/carlog/fuel/*
    /// </summary>
    public interface IFuelManager
    {
        /// <summary>목록. car_num 필수 (POST /api/carlog/fuel/list { car_num } → ResFuelList)</summary>
        Task<List<FuelInfo>> GetListAsync(int carNum);

        /// <summary>상세. 이미지 포함 (POST /api/carlog/fuel/detail → ResFuelDetail)</summary>
        Task<FuelInfo?> GetAsync(int fuelNum);

        /// <summary>등록 (POST /api/carlog/fuel/update + image/add)</summary>
        Task<(bool Success, string Message)> InsertAsync(FuelInfo model);

        /// <summary>수정 (POST /api/carlog/fuel/update + image/add, image/del)</summary>
        Task<(bool Success, string Message)> UpdateAsync(FuelInfo model, List<int> deleteImageIds);

        /// <summary>삭제 - 사용불가 처리</summary>
        Task<(bool Success, string Message)> DeleteAsync(int fuelNum);

        /// <summary>사용상태 일괄 변경</summary>
        Task<(bool Success, string Message)> ChangeStatusAsync(IEnumerable<int> fuelNums, CdStatus status);
    }
}
