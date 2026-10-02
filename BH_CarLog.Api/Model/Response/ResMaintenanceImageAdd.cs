using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/maintenance/image/add { car_maintenance_num, image_data(파일), image_name }</summary>
    public class ResMaintenanceImageAdd : ResImageAddBase
    {
        public int car_maintenance_image_num { get; set; }

        [JsonIgnore]
        public override int LinkImageNum => car_maintenance_image_num;
    }
}
