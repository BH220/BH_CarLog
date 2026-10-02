namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/consumables/pairing/del { car_consumables_history_num }</summary>
    public class ResConsumablePairDelete : ResBase
    {
        public int deleted { get; set; }
    }
}
