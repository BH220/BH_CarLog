using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;

namespace BH_CarLog.Api.Interface
{
    /// <summary>
    /// 유지보수 - /api/carlog/maintenance/*
    /// </summary>
    public interface IMaintenanceManager
    {
        /// <summary>목록. car_num 필수 (POST /api/carlog/maintenance/list { car_num } → ResMaintenanceList)</summary>
        Task<List<MaintenanceInfo>> GetListAsync(int carNum);

        /// <summary>상세. 이미지 포함 (POST /api/carlog/maintenance/detail → ResMaintenanceDetail)</summary>
        Task<MaintenanceInfo?> GetAsync(int maintenanceNum);

        /// <summary>등록 (POST /api/carlog/maintenance/update + image/add)</summary>
        Task<(bool Success, string Message)> InsertAsync(MaintenanceInfo model);

        /// <summary>수정 (POST /api/carlog/maintenance/update + image/add, image/del)</summary>
        Task<(bool Success, string Message)> UpdateAsync(MaintenanceInfo model, List<int> deleteImageIds);

        /// <summary>삭제 - 사용불가 처리</summary>
        Task<(bool Success, string Message)> DeleteAsync(int maintenanceNum);

        /// <summary>사용상태 일괄 변경</summary>
        Task<(bool Success, string Message)> ChangeStatusAsync(IEnumerable<int> maintenanceNums, CdStatus status);
    }
}
