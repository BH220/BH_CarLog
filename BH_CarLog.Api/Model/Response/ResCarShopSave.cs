namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/shop/update (car_shop_num 이 없거나 0 이면 등록)</summary>
    public class ResCarShopSave : ResBase
    {
        public int car_shop_num { get; set; }
        public bool created { get; set; }
    }
}
