namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/consumables/pairing/add { car_consumables_num, car_maintenance_num }</summary>
    public class ResConsumablePairAdd : ResBase
    {
        public int car_consumables_history_num { get; set; }
        public bool created { get; set; }
    }
}
