namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/fuel/update (car_fuel_history_num 이 없거나 0 이면 등록)</summary>
    public class ResFuelSave : ResBase
    {
        public int car_fuel_history_num { get; set; }
        public bool created { get; set; }
    }
}
