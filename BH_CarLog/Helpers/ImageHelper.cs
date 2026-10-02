using System.IO;
using System.Windows.Media.Imaging;
using BH_CarLog.Core.Helper;

namespace BH_CarLog.Helpers
{
    /// <summary>이미지 바이너리 관련 보조 함수</summary>
    public static class ImageHelper
    {
        /// <summary>픽셀 크기만 읽는다(디코딩 지연). 이미지가 아니거나 비어 있으면 (0, 0).</summary>
        public static (int Width, int Height) ReadPixelSize(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return (0, 0);
            try
            {
                using var stream = new MemoryStream(bytes);
                var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                return (frame.PixelWidth, frame.PixelHeight);
            }
            catch (Exception ex)
            {
                Log.Warn($"이미지 크기 읽기 실패: {ex.Message}");
                return (0, 0);
            }
        }
    }
}
