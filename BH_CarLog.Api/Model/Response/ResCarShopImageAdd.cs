using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/shop/image/add { car_shop_num, image_data(파일), image_name }</summary>
    public class ResCarShopImageAdd : ResImageAddBase
    {
        public int car_shop_image_num { get; set; }

        [JsonIgnore]
        public override int LinkImageNum => car_shop_image_num;
    }
}
