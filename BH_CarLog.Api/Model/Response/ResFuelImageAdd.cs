using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/fuel/image/add { car_fuel_history_num, image_data(파일), image_name }</summary>
    public class ResFuelImageAdd : ResImageAddBase
    {
        public int car_fuel_history_image_num { get; set; }

        [JsonIgnore]
        public override int LinkImageNum => car_fuel_history_image_num;
    }
}
