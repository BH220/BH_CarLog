namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// POST /api/carlog/{shop|fuel|maintenance}/image/del { *_image_num } - 모든 항목 공용
    /// </summary>
    public class ResImageDelete : ResBase
    {
        public int deleted { get; set; }
        public List<int> image_num_list { get; set; } = new List<int>();
    }
}
