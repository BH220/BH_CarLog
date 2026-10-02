using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// 이미지 등록(.../image/add) 응답 공통 항목.
    /// 항목별 연결 키(car_shop_image_num / car_fuel_history_image_num / car_maintenance_image_num)는 파생 클래스에 있고,
    /// 파생 클래스를 쓰지 않을 때는 응답에 함께 온 "*_image_num" 필드를 <see cref="LinkImageNum"/> 이 찾아준다.
    /// </summary>
    public class ResImageAddBase : ResBase
    {
        public int image_num { get; set; }
        public string name { get; set; } = "";
        public string extension { get; set; } = "";
        public long size { get; set; }
        public int width { get; set; }

        /// <summary>서버 필드명이 heigth 로 되어 있어 매핑한다.</summary>
        [JsonProperty("heigth")]
        public int height { get; set; }

        public string url { get; set; } = "";

        /// <summary>선언된 속성에 매핑되지 않은 나머지 응답 필드</summary>
        [JsonExtensionData]
        public IDictionary<string, JToken>? extra { get; set; }

        /// <summary>
        /// 항목별 연결 키. 파생 클래스가 자기 필드로 재정의한다.
        /// 기본 구현은 매핑되지 않은 필드 중 "*_image_num" 을 찾아 돌려준다.
        /// </summary>
        [JsonIgnore]
        public virtual int LinkImageNum
        {
            get
            {
                if (extra == null)
                    return 0;
                foreach (var pair in extra)
                {
                    if (pair.Key.EndsWith("_image_num", StringComparison.OrdinalIgnoreCase) && pair.Value.Type == JTokenType.Integer)
                        return pair.Value.Value<int>();
                }
                return 0;
            }
        }
    }
}
