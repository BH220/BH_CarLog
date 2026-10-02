using BH_CarLog.Core;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/direct/status { transfer_type, recoard_num_list } - 처리 상태 일괄 변경</summary>
    public class ResNewMessageStatus : ResBase
    {
        /// <summary>바뀐 건수</summary>
        public int changed { get; set; }

        public CdTransfer transfer_type { get; set; }
    }
}
