namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/maintenance/detail { car_maintenance_num }</summary>
    public class ResMaintenanceDetail : ResBase
    {
        public MaintenanceInfo? detail { get; set; }
    }
}
