namespace BH_CarLog.Core
{
    public enum UserType
    {
        SuperAdmin = 0,
        Administrator = 1,
        User = 2,
        Device = 3
    }

    public enum StatusTypes
    {
        Enable = 1,
        Disable = 0,
    }

    /// <summary>
    /// 공통코드의 카테고리 구분. 콤보 박스 구성에 쓴다.
    /// </summary>
    public enum CodeTypes
    {
        /// <summary>연료구분(122)</summary>
        Fuel = 122,

        /// <summary>가게 유형(123)</summary>
        ShopType = 123,

        /// <summary>유지보수 유형(124)</summary>
        Maintenance = 124,
    }

    /// <summary>
    /// 메인 화면의 좌측 메뉴. (기존 frmMain 의 Menus 상수)
    /// </summary>
    public enum Menus
    {
        /// <summary>웹(바로등록)으로 들어온 임시 데이터. 실데이터로 전환하거나 버린다.</summary>
        새메시지 = 100,
        주유 = 101,
        유지보수 = 102,
        소모품 = 103,
        차량 = 104,
        가게 = 105,
        /// <summary>소모품별 교환 필요 여부. (기존 frmTermList, 메뉴 "주기(T)")</summary>
        교환주기 = 106,
        시간연장 = 201,
        화면잠금 = 202,
        로그아웃 = 203,
        프로그램종료 = 204,
    }
}
