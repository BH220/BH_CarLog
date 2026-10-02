namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/shop/list</summary>
    public class ResCarShopList : ResBase
    {
        public List<CarShopInfo> list { get; set; } = new List<CarShopInfo>();
    }
}
