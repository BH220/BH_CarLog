using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;

namespace BH_CarLog.Api.Manager
{
    public class ImageManager : IImageManager
    {
        public async Task<(bool Success, string Message)> LoadDataAsync(ImageInfo image)
        {
            if (image.data is { Length: > 0 })
                return (true, "");
            if (image.IsNew)
                return (false, "아직 서버에 올리지 않은 이미지입니다.");

            // 응답의 url("/api/carlog/image/{image_num}") 을 우선 쓰고, 없으면 image_num 으로 만든다.
            string endpoint = string.IsNullOrWhiteSpace(image.url) ? $"/api/carlog/image/{image.image_num}" : image.url;
            var (success, message, data) = await CarLogApi.Instance.GetBytesAsync(endpoint);
            if (success == false || data == null || data.Length == 0)
                return (false, string.IsNullOrEmpty(message) ? "이미지를 내려받지 못했습니다." : message);

            image.data = data;
            return (true, "");
        }
    }
}
