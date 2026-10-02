using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;

namespace BH_CarLog.Api.Interface
{
    /// <summary>
    /// 새 메시지 - 웹(바로등록)으로 들어온 임시 데이터. 서버 /api/carlog/direct/*
    /// 목록(list)·상세(detail)·실데이터 전환(realize)·처리 상태 변경(status) 이 있다.
    /// 버림(삭제)은 서버 처리 상태 코드가 전환대기·실데이터전환 둘뿐이라 아직 표현할 수 없다.
    /// </summary>
    public interface INewMessageManager
    {
        /// <summary>버림(삭제)이 가능한지. 서버에 "버림" 처리 상태 코드(125xxx)나 삭제 API 가 생기면 true 로 바꾼다.</summary>
        bool IsDeleteSupported { get; }

        /// <summary>전환대기(125001) 목록. car_num 필수 (POST /api/carlog/direct/list { car_num, transfer_type_list }). 건마다 images 포함</summary>
        Task<List<NewMessageInfo>> GetListAsync(int carNum);

        /// <summary>상세. 첨부 이미지 포함 (POST /api/carlog/direct/detail { recoard_num } → ResNewMessageDetail). 조회 실패는 ApiException</summary>
        Task<NewMessageInfo?> GetAsync(int recoardNum);

        /// <summary>
        /// 실데이터 전환 완료 표시 (POST /api/carlog/direct/realize { recoard_num, transfer_to, transfer_num }).
        /// 주유/유지보수로 저장한 뒤, 어떤 유형(124xxx)의 몇 번 기록으로 옮겼는지 남긴다. 처리 상태가 실데이터전환(125002) 이 된다.
        /// 이미 전환된 기록은 서버가 거절한다.
        /// </summary>
        Task<(bool Success, string Message)> RealizeAsync(int recoardNum, CdMaintenanceType transferTo, int transferNum);

        /// <summary>버리기. <see cref="IsDeleteSupported"/> 가 false 인 동안은 ApiNotImplementedException 을 던진다.</summary>
        Task<(bool Success, string Message)> DeleteAsync(int recoardNum);
    }
}
