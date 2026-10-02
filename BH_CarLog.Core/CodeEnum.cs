namespace BH_CarLog.Core
{
    /// <summary>
    /// 사용여부(code:101)
    /// </summary>
    public enum CdStatus
    {
        사용 = 101001,
        사용불가 = 101002,
    }

    /// <summary>
    /// 연료구분(code:122)
    /// </summary>
    public enum CdFuel
    {
        경유 = 122001,
        휘발유 = 122002,
        전기 = 122003,
        LPG = 122004,
    }

    /// <summary>
    /// 가게 유형(code:123)
    /// </summary>
    public enum CdShopType
    {
        주유소 = 123001,
        정비소 = 123002,
        용품점 = 123003,
        혼합 = 123004,
    }

    /// <summary>
    /// 유지보수 유형(code:124)
    /// </summary>
    public enum CdMaintenanceType
    {
        주유 = 124001,
        정비 = 124002,
        구매 = 124003,
        세차 = 124004,
        공기압 = 124005,
    }

    /// <summary>
    /// 데이터 처리 상태(code:125). 웹 바로등록으로 쌓인 임시 데이터에 쓴다.
    /// </summary>
    public enum CdTransfer
    {
        전환대기 = 125001,
        실데이터전환 = 125002,
        /// <summary>버림. 실데이터로 만들지 않고 목록에서 뺀 것 (서버 코드표 125003)</summary>
        삭제 = 125003,
    }

    /// <summary>
    /// 이미지 유형(code:114) - 차계부에서 쓰는 값만 정의
    /// </summary>
    public enum CdImageType
    {
        영수증 = 114005,
        정비전 = 114006,
        정비후 = 114007,
        차량 = 114008,
        가게 = 114009,
        소모품 = 114010,
        구매물품 = 114011,
        웹등록 = 114012,
        기타 = 114999,
    }
}
