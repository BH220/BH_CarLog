using BH_CarLog.Core;
using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// 주유기록 한 건. 목록/상세 공용. images 는 상세에서만 채워진다.
    /// 서버는 리터를 저장하지 않는다. 금액 ÷ 리터당금액 으로 계산한다.
    /// </summary>
    public class FuelInfo
    {
        public int car_fuel_history_num { get; set; }
        public int car_num { get; set; }
        public int car_shop_num { get; set; }

        /// <summary>주유일</summary>
        public DateTime? refuel_at { get; set; }

        /// <summary>주행거리(적산 거리계)</summary>
        public int mileage { get; set; }

        /// <summary>주유금액</summary>
        public int amount { get; set; }

        /// <summary>리터당 금액</summary>
        public int amount_per_liter { get; set; }

        public string note { get; set; } = "";
        public CdFuel fuel_type { get; set; }
        public CdStatus status { get; set; }

        public List<FuelImageInfo> images { get; set; } = new List<FuelImageInfo>();

        #region 화면 표시용 (서버 응답에 없음)
        /// <summary>주유량(ℓ) = 금액 ÷ 리터당 금액</summary>
        [JsonIgnore]
        public decimal Liter => amount_per_liter > 0 ? Math.Round((decimal)amount / amount_per_liter, 2) : 0m;

        /// <summary>가게명. 목록 화면이 가게 목록과 맞춰 채운다.</summary>
        [JsonIgnore]
        public string ShopName { get; set; } = "";

        /// <summary>차량 표시명. 목록 화면이 차량 목록과 맞춰 채운다.</summary>
        [JsonIgnore]
        public string CarName { get; set; } = "";

        /// <summary>직전 주유 대비 연비(㎞/ℓ). 목록 화면이 계산해 채운다. 계산 불가면 null.</summary>
        [JsonIgnore]
        public decimal? Efficiency { get; set; }
        #endregion
    }
}
