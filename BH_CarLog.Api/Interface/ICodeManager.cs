using BH_CarLog.Api.Model;
using BH_CarLog.Core;

namespace BH_CarLog.Api.Interface
{
    /// <summary>
    /// 공통코드. 코드 조회 API 가 없으므로 DB 와 맞춰 둔 BH_CarLog.Core.CodeEnum 의 enum 에서 만든다.
    /// </summary>
    public interface ICodeManager
    {
        Task<List<CodeInfo>> GetCodesAsync(CodeTypes codeType);
    }
}
