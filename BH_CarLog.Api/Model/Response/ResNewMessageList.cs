namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/direct/list</summary>
    public class ResNewMessageList : ResBase
    {
        public List<NewMessageInfo> list { get; set; } = new List<NewMessageInfo>();
    }
}
