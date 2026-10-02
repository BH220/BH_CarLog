using BH_CarLog.Core;
using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// 소모품 변경 이력 한 건 (유지보수 값 포함).
    /// POST /api/carlog/consumables/history 응답의 history 배열 요소.
    /// </summary>
    public class ConsumableHistoryInfo
    {
        public int car_consumables_history_num { get; set; }
        public int car_maintenance_num { get; set; }
        public int car_shop_num { get; set; }
        public CdMaintenanceType mataintenance_type { get; set; }
        public int mileage { get; set; }
        public int amount { get; set; }
        public string note { get; set; } = "";
        public CdStatus maintenance_status { get; set; }

        /// <summary>
        /// 유지보수일 (서버 필드명 오타를 그대로 따른다).
        /// history 응답에 아직 없으면 클라이언트가 같은 차의 유지보수 목록에서 car_maintenance_num 으로 맞춰 채운다.
        /// </summary>
        public DateTime? mataintenance_at { get; set; }

        /// <summary>유지보수 기록 등록일시</summary>
        public DateTime? maintenance_created_at { get; set; }

        /// <summary>이력 연결 일시</summary>
        public DateTime? created_at { get; set; }

        /// <summary>화면·계산에 쓰는 유지보수일. 유지보수일을 끝내 못 채우면 등록일시로 대신한다.</summary>
        [JsonIgnore]
        public DateTime? MaintenanceAt => mataintenance_at ?? maintenance_created_at;

        [JsonIgnore]
        public string ShopName { get; set; } = "";
    }
}
