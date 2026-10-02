using BH_CarLog.Core;
using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// 유지보수 한 건. 정비·구매·세차·공기압을 유형(code:124)으로 구분한다.
    /// 컬럼명이 mataintenance_type 으로 잘못 적혀 있으나 테이블·API 필드명이라 그대로 쓴다.
    /// </summary>
    public class MaintenanceInfo
    {
        public int car_maintenance_num { get; set; }
        public int car_num { get; set; }
        public int car_shop_num { get; set; }

        /// <summary>유지보수 유형 (서버 필드명 오타를 그대로 따른다)</summary>
        public CdMaintenanceType mataintenance_type { get; set; }

        /// <summary>주행거리(적산 거리계)</summary>
        public int mileage { get; set; }

        public int amount { get; set; }
        public string note { get; set; } = "";
        public CdStatus status { get; set; }

        /// <summary>유지보수일 (서버 필드명 오타를 그대로 따른다). 목록·상세 응답에 담기고, 등록/수정 시 yyyy-MM-dd 로 보낸다.</summary>
        public DateTime? mataintenance_at { get; set; }

        public List<MaintenanceImageInfo> images { get; set; } = new List<MaintenanceImageInfo>();

        #region 화면 표시용 (서버 응답에 없음)
        [JsonIgnore]
        public string ShopName { get; set; } = "";

        [JsonIgnore]
        public string CarName { get; set; } = "";
        #endregion
    }
}
