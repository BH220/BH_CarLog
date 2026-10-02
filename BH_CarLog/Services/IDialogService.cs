using CommunityToolkit.Mvvm.ComponentModel;

namespace BH_CarLog.Services
{
    public interface IDialogService
    {
        void ShowInfo(string message, string title = "알림");
        void ShowWarning(string message, string title = "경고");
        void ShowError(string message, string title = "오류");

        /// <summary>예/아니오 확인. 예=true</summary>
        bool Confirm(string message, string title = "확인");

        /// <summary>예/아니오/취소 확인. 예=true, 아니오=false, 취소=null</summary>
        bool? ConfirmWithCancel(string message, string title = "확인");

        /// <summary>ViewModel 에 매핑된 Window 를 모달로 연다.</summary>
        bool? ShowDialog(ObservableObject viewModel);

        /// <summary>
        /// ViewModel 에 매핑된 Window 를 모달 아님으로 연다. (이미지 보기처럼 열어 둔 채 원래 창을 계속 쓰는 경우)
        /// 지금 활성 창을 소유자로 두므로, 소유자가 닫히면 이 창도 함께 닫힌다.
        /// </summary>
        void Show(ObservableObject viewModel);

        /// <summary>파일 선택 대화상자. 취소 시 null</summary>
        string? OpenFile(string filter, string title = "파일 선택");
    }

    public interface IClipboardService
    {
        void SetText(string text);
    }
}
