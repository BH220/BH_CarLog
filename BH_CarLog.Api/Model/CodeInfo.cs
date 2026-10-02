using BH_CarLog.Core;

namespace BH_CarLog.Api.Model
{
    /// <summary>
    /// 공통코드. 연료구분/가게유형/유지보수유형 등을 표현한다.
    /// </summary>
    public class CodeInfo
    {
        public int SqCode { get; set; }
        public string NmCode { get; set; } = "";
        public string TyCode { get; set; } = "";
        public string TxtNote { get; set; } = "";

        public CodeInfo() { }

        public CodeInfo(int sqCode, string nmCode, CodeTypes type, string note = "")
        {
            SqCode = sqCode;
            NmCode = nmCode;
            TyCode = ((int)type).ToString();
            TxtNote = note;
        }

        public override string ToString() => NmCode;
    }
}
