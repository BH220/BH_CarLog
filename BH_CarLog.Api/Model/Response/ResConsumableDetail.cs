namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/consumables/detail { car_consumables_num }</summary>
    public class ResConsumableDetail : ResBase
    {
        public ConsumableInfo? detail { get; set; }
    }
}
