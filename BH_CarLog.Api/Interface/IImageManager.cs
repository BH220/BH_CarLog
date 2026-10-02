using BH_CarLog.Api.Model.Response;

namespace BH_CarLog.Api.Interface
{
    /// <summary>
    /// 첨부 이미지 원본 - GET /api/carlog/image/{image_num} (인증 필요, 바이너리 응답).
    /// 가게/주유/유지보수가 공용으로 쓴다.
    /// </summary>
    public interface IImageManager
    {
        /// <summary>
        /// 이미지 바이너리를 받아 ImageInfo.data 에 채운다.
        /// 이미 채워져 있으면 서버에 다시 요청하지 않는다.
        /// </summary>
        Task<(bool Success, string Message)> LoadDataAsync(ImageInfo image);
    }
}
