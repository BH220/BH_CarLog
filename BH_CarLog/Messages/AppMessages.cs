using BH_CarLog.Api.Model.Response;

namespace BH_CarLog.Messages
{
    /// <summary>로그인 성공</summary>
    public sealed record LoginSucceededMessage;

    /// <summary>사용자 활동 (자동 잠금 타이머 리셋)</summary>
    public sealed record UserActivityMessage;

    /// <summary>하단 상태바 텍스트 변경</summary>
    public sealed record StatusTextMessage(string Text);

    /// <summary>확인 없이 프로그램 종료 요청 (로그인 화면의 종료 버튼 등)</summary>
    public sealed record ExitRequestedMessage;

    /// <summary>상단 차량 선택이 바뀜. 목록 화면은 선택된 차 기준으로 다시 조회한다. (기존 frmMain.carSelect)</summary>
    public sealed record CarChangedMessage(CarInfo? Car);

    /// <summary>차량 정보가 등록/수정/삭제되어 차량 목록을 다시 읽어야 함</summary>
    public sealed record CarListChangedMessage;
}
