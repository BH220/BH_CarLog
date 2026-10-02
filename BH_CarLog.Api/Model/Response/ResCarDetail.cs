namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/car/detail { car_num }</summary>
    public class ResCarDetail : ResBase
    {
        public CarInfo? detail { get; set; }
    }
}
