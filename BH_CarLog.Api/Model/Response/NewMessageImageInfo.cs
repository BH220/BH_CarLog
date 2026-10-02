using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>웹 바로등록 임시 데이터의 첨부 이미지</summary>
    public class NewMessageImageInfo : ImageInfo
    {
        public int recoard_image_num { get; set; }

        [JsonIgnore]
        public override int LinkImageNum { get => recoard_image_num; set => recoard_image_num = value; }
    }
}
