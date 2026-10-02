using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model;
using BH_CarLog.Core;

namespace BH_CarLog.Api.Manager
{
    public class CodeManager : ICodeManager
    {
        private readonly Dictionary<CodeTypes, List<CodeInfo>> _cache = new();

        public Task<List<CodeInfo>> GetCodesAsync(CodeTypes codeType)
        {
            if (_cache.TryGetValue(codeType, out var cached) == false)
            {
                cached = codeType switch
                {
                    CodeTypes.Fuel => FromEnum<CdFuel>(codeType),
                    CodeTypes.ShopType => FromEnum<CdShopType>(codeType),
                    CodeTypes.Maintenance => FromEnum<CdMaintenanceType>(codeType),
                    _ => new List<CodeInfo>(),
                };
                _cache[codeType] = cached;
            }
            return Task.FromResult(cached);
        }

        /// <summary>enum 멤버(이름 = 표시명, 값 = 코드) → CodeInfo</summary>
        private static List<CodeInfo> FromEnum<TEnum>(CodeTypes codeType) where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                .Select(x => new CodeInfo(Convert.ToInt32(x), x.ToString(), codeType))
                .ToList();
        }
    }
}
