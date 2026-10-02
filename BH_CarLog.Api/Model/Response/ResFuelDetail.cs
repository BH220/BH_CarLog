namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/fuel/detail { car_fuel_history_num }</summary>
    public class ResFuelDetail : ResBase
    {
        public FuelInfo? detail { get; set; }
    }
}
