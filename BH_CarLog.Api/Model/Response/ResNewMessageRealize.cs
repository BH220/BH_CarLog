using BH_CarLog.Core;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/direct/realize { recoard_num, transfer_to, transfer_num } - 실데이터 전환 완료 표시</summary>
    public class ResNewMessageRealize : ResBase
    {
        public int recoard_num { get; set; }

        /// <summary>처리 상태. 성공이면 실데이터전환(125002)</summary>
        public CdTransfer transfer_type { get; set; }

        /// <summary>옮긴 유형 (124xxx)</summary>
        public CdMaintenanceType transfer_to { get; set; }

        /// <summary>옮긴 실데이터의 키</summary>
        public int transfer_num { get; set; }
    }
}
