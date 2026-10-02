using CommunityToolkit.Mvvm.ComponentModel;

namespace BH_CarLog.ViewModels.Base
{
    /// <summary>
    /// 상단 기능 버튼(추가/수정/삭제/...)의 활성화·표시 상태.
    /// 각 목록 화면(IFunctionHost)이 소유하고 MainViewModel 이 바인딩한다.
    /// </summary>
    public partial class FunctionButtonState : ObservableObject
    {
        [ObservableProperty] private bool _insertEnabled;
        [ObservableProperty] private bool _updateEnabled;
        [ObservableProperty] private bool _deleteEnabled;
        [ObservableProperty] private bool _copyEnabled;
        [ObservableProperty] private bool _refreshEnabled;
        [ObservableProperty] private bool _searchEnabled;
        [ObservableProperty] private bool _excelEnabled;
        [ObservableProperty] private bool _printEnabled;
        [ObservableProperty] private bool _closeEnabled;

        /// <summary>화면별 추가 기능 1 (F1) 버튼 표시 여부. 예: 소모품 교체이력</summary>
        [ObservableProperty] private bool _custom1Visible;

        /// <summary>F1 버튼에 표시할 문구</summary>
        [ObservableProperty] private string _custom1Text = "";

        /// <summary>화면별 추가 기능 2 (F2) 버튼 표시 여부</summary>
        [ObservableProperty] private bool _custom2Visible;

        /// <summary>F2 버튼에 표시할 문구</summary>
        [ObservableProperty] private string _custom2Text = "";

        public void SetAll(bool enabled)
        {
            InsertEnabled = UpdateEnabled = DeleteEnabled = CopyEnabled = RefreshEnabled =
                SearchEnabled = ExcelEnabled = PrintEnabled = CloseEnabled = enabled;
        }
    }
}
