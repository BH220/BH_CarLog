using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core;
using BH_CarLog.Core.Utils;

namespace BH_CarLog.Api.Manager
{
    /// <summary>
    /// 새 메시지 API (/api/carlog/direct/*). 조회 실패는 <see cref="ApiException"/> 으로 알린다.
    /// </summary>
    public class NewMessageManager : INewMessageManager
    {
        /// <summary>
        /// 서버의 처리 상태 코드는 전환대기(125001)·실데이터전환(125002) 뿐이라 "버림" 을 표현할 수 없다.
        /// 임의 코드를 status 로 보내면 코드표에 없는 값이 남으므로, 서버에 버림 코드나 삭제 API 가 생길 때까지 막아 둔다.
        /// </summary>
        public bool IsDeleteSupported => false;

        public async Task<List<NewMessageInfo>> GetListAsync(int carNum)
        {
            // 서버는 car_num 과 transfer_type_list(125xxx, 콤마 다수) 를 모두 필수로 받는다. 빠지면 400 이 온다.
            var param = new Dictionary<string, string>
            {
                ["car_num"] = carNum.ToStringEx(),
                ["transfer_type_list"] = ((int)CdTransfer.전환대기).ToStringEx(),
            };
            ResNewMessageList res = await CarLogApi.Instance.PostResAsync<ResNewMessageList>("/api/carlog/direct/list", param);
            return res.EnsureSuccess("새 메시지 목록").list;
        }

        public async Task<NewMessageInfo?> GetAsync(int recoardNum)
        {
            var param = new Dictionary<string, string> { ["recoard_num"] = recoardNum.ToStringEx() };
            ResNewMessageDetail res = await CarLogApi.Instance.PostResAsync<ResNewMessageDetail>("/api/carlog/direct/detail", param);
            return res.EnsureSuccess("새 메시지 상세").detail;
        }

        public async Task<(bool Success, string Message)> RealizeAsync(int recoardNum, CdMaintenanceType transferTo, int transferNum)
        {
            var param = new Dictionary<string, string>
            {
                ["recoard_num"] = recoardNum.ToStringEx(),
                ["transfer_to"] = ((int)transferTo).ToStringEx(),
                ["transfer_num"] = transferNum.ToStringEx(),
            };
            ResNewMessageRealize res = await CarLogApi.Instance.PostResAsync<ResNewMessageRealize>("/api/carlog/direct/realize", param);
            return (res.result == 1, res.msg);
        }

        public Task<(bool Success, string Message)> DeleteAsync(int recoardNum)
            => throw new ApiNotImplementedException("새 메시지 버리기", "POST /api/carlog/direct/status 에 쓸 버림 처리 상태 코드(125xxx) 또는 삭제 API");
    }
}
