using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>가게 첨부 이미지</summary>
    public class CarShopImageInfo : ImageInfo
    {
        public int car_shop_image_num { get; set; }

        /// <summary>image/del 에 쓰는 연결 키</summary>
        [JsonIgnore]
        public override int LinkImageNum { get => car_shop_image_num; set => car_shop_image_num = value; }
    }
}
