using BH_CarLog.Core;
using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// 소모품 항목 한 건. "무엇을 얼마 주기로 관리하는가" 만 담는다.
    /// 실제 교체 사실은 유지보수 기록이 담고, 둘을 변경 이력으로 잇는다.
    /// </summary>
    public class ConsumableInfo
    {
        public int car_consumables_num { get; set; }
        public int car_num { get; set; }

        /// <summary>항목명 (최대 50자)</summary>
        public string name { get; set; } = "";

        /// <summary>관리 주기(일). null 이면 "주기 없음"</summary>
        public int? term { get; set; }

        /// <summary>관리 주기(거리 km). null 이면 "주기 없음"</summary>
        public int? distance { get; set; }

        public CdStatus status { get; set; }

        /// <summary>변경 이력 (history API 에서만 채워진다)</summary>
        public List<ConsumableHistoryInfo> history { get; set; } = new List<ConsumableHistoryInfo>();

        #region 화면 표시용 (서버 응답에 없음)
        [JsonIgnore]
        public string CarName { get; set; } = "";

        [JsonIgnore]
        public string TermText => term.HasValue ? $"{term.Value:#,0}일" : "-";

        [JsonIgnore]
        public string DistanceText => distance.HasValue ? $"{distance.Value:#,0}㎞" : "-";
        #endregion

        public override string ToString() => name;
    }
}
