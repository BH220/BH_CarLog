namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/consumables/list { car_num }</summary>
    public class ResConsumableList : ResBase
    {
        public List<ConsumableInfo> list { get; set; } = new List<ConsumableInfo>();
    }
}
