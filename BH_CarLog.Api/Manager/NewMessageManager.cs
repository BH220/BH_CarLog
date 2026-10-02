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
        /// 버릴 때 넣는 처리 상태 = 삭제(125003, 서버 코드표). 목록은 전환대기만 조회하므로 이 값이 되면 화면에서 사라진다.
        /// 행을 지우지는 않아 서버에서 125003 으로 조회하면 버린 내역을 볼 수 있다.
        /// </summary>
        private const CdTransfer DiscardedTransferType = CdTransfer.삭제;

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

        public async Task<(bool Success, string Message)> DeleteAsync(int recoardNum)
        {
            var param = new Dictionary<string, string>
            {
                ["transfer_type"] = ((int)DiscardedTransferType).ToStringEx(),
                ["recoard_num_list"] = recoardNum.ToStringEx(),
            };
            ResNewMessageStatus res = await CarLogApi.Instance.PostResAsync<ResNewMessageStatus>("/api/carlog/direct/status", param);
            return (res.result == 1, res.msg);
        }
    }
}
