using BH_CarLog.Api.Model;

namespace BH_CarLog.Api.Interface
{
    /// <summary>
    /// 교환주기 - 소모품별 교환 필요 여부. 전용 서버 API 는 없고
    /// 소모품 목록 + 교체이력 + 주유/유지보수의 최근 주행거리를 합쳐 클라이언트에서 계산한다.
    /// </summary>
    public interface ITermManager
    {
        /// <summary>선택한 차의 소모품별 교환주기 상태. 조회 실패는 ApiException 으로 알린다.</summary>
        Task<List<TermStatusInfo>> GetListAsync(int carNum);
    }
}
