using BH_CarLog.Core;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// 차량 한 건. 목록(/api/carlog/car/list) 과 상세(/api/carlog/car/detail) 공용.
    /// 차에는 첨부 이미지가 붙지 않는다. (연결 테이블 없음)
    /// </summary>
    public class CarInfo
    {
        public int car_num { get; set; }

        /// <summary>차번호 (예: 12가3456)</summary>
        public string car_no { get; set; } = "";

        /// <summary>애칭 (최대 10자)</summary>
        public string car_name { get; set; } = "";

        /// <summary>차량년식 (네 자리 연도)</summary>
        public string born_year { get; set; } = "";

        /// <summary>차대번호</summary>
        public string vin { get; set; } = "";

        /// <summary>차종</summary>
        public string car_type { get; set; } = "";

        public CdFuel fuel_type { get; set; }

        /// <summary>배기량</summary>
        public string cc { get; set; } = "";

        public DateTime? buy_at { get; set; }
        public string note { get; set; } = "";
        public CdStatus status { get; set; }

        /// <summary>목록/콤보 표시용. "애칭 (차번호)"</summary>
        public string DisplayName => string.IsNullOrEmpty(car_name) ? car_no : $"{car_name} ({car_no})";

        public override string ToString() => DisplayName;
    }
}
