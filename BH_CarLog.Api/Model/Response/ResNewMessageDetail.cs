namespace BH_CarLog.Api.Model.Response
{
    /// <summary>POST /api/carlog/direct/detail { recoard_num }</summary>
    public class ResNewMessageDetail : ResBase
    {
        public NewMessageInfo? detail { get; set; }
    }
}
