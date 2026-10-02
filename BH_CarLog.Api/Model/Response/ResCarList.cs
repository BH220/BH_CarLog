namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/car/list</summary>
    public class ResCarList : ResBase
    {
        public List<CarInfo> list { get; set; } = new List<CarInfo>();
    }
}
