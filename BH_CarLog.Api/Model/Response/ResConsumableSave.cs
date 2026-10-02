namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/consumables/update (car_consumables_num 이 없거나 0 이면 등록)</summary>
    public class ResConsumableSave : ResBase
    {
        public int car_consumables_num { get; set; }
        public bool created { get; set; }
    }
}
