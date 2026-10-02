using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;

namespace BH_CarLog.Api.Interface
{
    /// <summary>
    /// 소모품 - /api/carlog/consumables/*. 목록은 차 단위로만 조회한다.
    /// </summary>
    public interface IConsumableManager
    {
        /// <summary>목록 (POST /api/carlog/consumables/list { car_num })</summary>
        Task<List<ConsumableInfo>> GetListAsync(int carNum);

        /// <summary>상세 (POST /api/carlog/consumables/detail)</summary>
        Task<ConsumableInfo?> GetAsync(int consumablesNum);

        /// <summary>등록/수정 (POST /api/carlog/consumables/update)</summary>
        Task<(bool Success, string Message)> SaveAsync(ConsumableInfo model);

        /// <summary>삭제 - 사용불가 처리</summary>
        Task<(bool Success, string Message)> DeleteAsync(int consumablesNum);

        /// <summary>사용상태 일괄 변경</summary>
        Task<(bool Success, string Message)> ChangeStatusAsync(IEnumerable<int> consumablesNums, CdStatus status);

        /// <summary>변경 이력 조회 (POST /api/carlog/consumables/history)</summary>
        Task<ConsumableInfo?> GetHistoryAsync(int consumablesNum);

        /// <summary>변경 이력 등록. 소모품과 유지보수 기록을 잇는다. 한 유지보수 기록은 여러 소모품에 이어질 수 있다.</summary>
        Task<(bool Success, string Message)> AddPairAsync(int consumablesNum, int maintenanceNum);

        /// <summary>변경 이력 삭제</summary>
        Task<(bool Success, string Message)> DeletePairAsync(IEnumerable<int> historyNums);
    }
}
