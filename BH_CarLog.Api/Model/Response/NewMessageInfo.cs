using BH_CarLog.Core;
using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// 웹 바로등록으로 들어온 임시 데이터 한 건. POST /api/carlog/direct/list 의 list 요소이자 /detail 의 detail.
    /// 로그인 없이 /direct/carlog/{token} 화면에서 만들어지며, 차계부에서 실데이터로 전환한다.
    /// 컬럼명 오타(recoard)는 테이블·API 필드명이라 그대로 쓴다.
    /// </summary>
    public class NewMessageInfo
    {
        public int recoard_num { get; set; }
        public int car_num { get; set; }

        /// <summary>입력일. 웹 등록 시각의 날짜를 서버가 넣는다. 실데이터의 날짜(주유일·유지보수일)로 넘긴다.</summary>
        public DateTime? input_at { get; set; }

        /// <summary>유지보수 유형 (주유 / 정비 / 구매 / 세차 / 공기압). 주유면 주유기록으로, 그 외는 유지보수로 옮긴다.</summary>
        public CdMaintenanceType recoard_type { get; set; }

        public int mileage { get; set; }
        public string note { get; set; } = "";

        /// <summary>전환대기 / 실데이터전환</summary>
        public CdTransfer transfer_type { get; set; }

        public DateTime? created_at { get; set; }

        /// <summary>실데이터로 옮긴 유형(124xxx). 전환대기면 null</summary>
        public CdMaintenanceType? transfer_to { get; set; }

        /// <summary>옮긴 실데이터의 키 (주유: car_fuel_history_num, 그 외: car_maintenance_num). 전환대기면 null</summary>
        public int? transfer_num { get; set; }

        public DateTime? transferred_at { get; set; }

        /// <summary>첨부 이미지 (웹에서 올린 영수증 등). 목록·상세 모두 담겨 온다.</summary>
        public List<NewMessageImageInfo> images { get; set; } = new List<NewMessageImageInfo>();

        /// <summary>차량 표시명. 목록 화면이 차량 목록과 맞춰 채운다.</summary>
        [JsonIgnore]
        public string CarName { get; set; } = "";
    }
}
