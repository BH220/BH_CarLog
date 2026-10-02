using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>주유기록 첨부 이미지</summary>
    public class FuelImageInfo : ImageInfo
    {
        public int car_fuel_history_image_num { get; set; }

        [JsonIgnore]
        public override int LinkImageNum { get => car_fuel_history_image_num; set => car_fuel_history_image_num = value; }
    }
}
