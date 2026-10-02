namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/maintenance/update (car_maintenance_num 이 없거나 0 이면 등록)</summary>
    public class ResMaintenanceSave : ResBase
    {
        public int car_maintenance_num { get; set; }
        public bool created { get; set; }
    }
}
