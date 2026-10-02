using BH_CarLog.Core;
using Newtonsoft.Json;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// 첨부 이미지 공통 항목. 상세 응답의 images 배열 요소.
    /// 실제 파일은 GET /api/carlog/image/{image_num} (url) 로 받는다.
    /// 화면의 첨부 목록(ImageGridViewModel)도 이 클래스를 그대로 쓴다.
    /// 서버 응답에 없는 클라이언트 전용 항목은 [JsonIgnore] 로 표시했다.
    /// </summary>
    public class ImageInfo
    {
        public int image_num { get; set; }
        public string name { get; set; } = "";
        public string extension { get; set; } = "";
        public long size { get; set; }
        public int width { get; set; }

        /// <summary>서버 필드명이 heigth 로 되어 있어 매핑한다.</summary>
        [JsonProperty("heigth")]
        public int height { get; set; }

        public CdImageType type { get; set; }
        public CdStatus status { get; set; }
        public DateTime? created_at { get; set; }
        public string url { get; set; } = "";

        #region 클라이언트 전용
        /// <summary>
        /// 항목별 연결 키 (car_shop_image_num / car_fuel_history_image_num / car_maintenance_image_num).
        /// 파생 클래스가 자기 키로 연결한다. image/del 은 이 키로 지운다.
        /// </summary>
        [JsonIgnore]
        public virtual int LinkImageNum { get => image_num; set => image_num = value; }

        /// <summary>아직 서버에 올리지 않은 신규 첨부 (image/add 대상)</summary>
        [JsonIgnore]
        public bool IsNew => LinkImageNum <= 0;

        /// <summary>이미지 바이너리. 신규 첨부는 업로드할 원본, 기존 이미지는 url 로 내려받은 캐시.</summary>
        [JsonIgnore]
        public byte[]? data { get; set; }

        /// <summary>확장자를 포함한 표시용 파일명. name 에 확장자가 이미 붙어 있으면 그대로 쓴다.</summary>
        [JsonIgnore]
        public string FileName
        {
            get
            {
                string ext = string.IsNullOrEmpty(extension) ? "" : (extension.StartsWith(".") ? extension : "." + extension);
                if (ext.Length == 0 || name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    return name;
                return name + ext;
            }
        }

        /// <summary>파일 크기를 "12.3 KB" 형태로 반환</summary>
        [JsonIgnore]
        public string SizeText
        {
            get
            {
                long bytes = size > 0 ? size : (data?.LongLength ?? 0);
                if (bytes <= 0) return "";
                string[] units = { "B", "KB", "MB", "GB" };
                double value = bytes;
                int unit = 0;
                while (value >= 1024 && unit < units.Length - 1)
                {
                    value /= 1024;
                    unit++;
                }
                return $"{value:0.#} {units[unit]}";
            }
        }
        #endregion
    }
}
