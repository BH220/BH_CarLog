namespace BH_CarLog.Core.Utils
{
    /// <summary>
    /// API 요청 파라미터 조립에 쓰는 변환 확장. (기존 공용 라이브러리에서 실제로 쓰는 것만 남겼다)
    /// </summary>
    public static class ExtensionParser
    {
        /// <summary>null 이면 빈 문자열, 아니면 ToString(). 폼 파라미터 값으로 쓴다.</summary>
        public static string ToStringEx(this object? o) => o?.ToString() ?? "";
    }
}
