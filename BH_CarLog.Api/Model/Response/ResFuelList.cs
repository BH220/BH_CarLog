namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/fuel/list</summary>
    public class ResFuelList : ResBase
    {
        public List<FuelInfo> list { get; set; } = new List<FuelInfo>();
    }
}
