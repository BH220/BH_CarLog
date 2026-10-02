using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>유지보수 첨부 이미지</summary>
    public class MaintenanceImageInfo : ImageInfo
    {
        public int car_maintenance_image_num { get; set; }

        [JsonIgnore]
        public override int LinkImageNum { get => car_maintenance_image_num; set => car_maintenance_image_num = value; }
    }
}
