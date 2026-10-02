namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/shop/detail { car_shop_num }</summary>
    public class ResCarShopDetail : ResBase
    {
        public CarShopInfo? detail { get; set; }
    }
}
