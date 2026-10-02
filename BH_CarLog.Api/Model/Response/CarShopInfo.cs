using BH_CarLog.Core;

namespace BH_CarLog.Api.Model.Response
{
    /// <summary>
    /// 가게 한 건. 주유소/정비소/용품점.
    /// images 는 상세에서만 채워진다.
    /// </summary>
    public class CarShopInfo
    {
        public int car_shop_num { get; set; }

        /// <summary>상호 (UNIQUE)</summary>
        public string name { get; set; } = "";

        /// <summary>사업자번호</summary>
        public string business_no { get; set; } = "";

        /// <summary>대표자</summary>
        public string ceo { get; set; } = "";

        public string address { get; set; } = "";
        public string tel { get; set; } = "";
        public string tel2 { get; set; } = "";
        public CdShopType shop_type { get; set; }
        public string note { get; set; } = "";
        public CdStatus status { get; set; }

        /// <summary>첨부 이미지 목록 (상세에서만)</summary>
        public List<CarShopImageInfo> images { get; set; } = new List<CarShopImageInfo>();

        public override string ToString() => name;
    }
}
