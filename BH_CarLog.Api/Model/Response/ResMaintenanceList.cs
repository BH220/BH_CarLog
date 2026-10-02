namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/maintenance/list</summary>
    public class ResMaintenanceList : ResBase
    {
        public List<MaintenanceInfo> list { get; set; } = new List<MaintenanceInfo>();
    }
}
