using CommunityToolkit.Mvvm.ComponentModel;

namespace BH_CarLog.ViewModels.Base
{
    public abstract partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = "";
    }

    /// <summary>
    /// 모달 창으로 표시되는 ViewModel. DialogResult 를 설정하면 창이 닫힌다. (DialogBehavior 참고)
    /// </summary>
    public abstract partial class DialogViewModelBase : ViewModelBase
    {
        [ObservableProperty]
        private bool? _dialogResult;

        [CommunityToolkit.Mvvm.Input.RelayCommand]
        private void Close() => OnClose();

        protected virtual void OnClose() => DialogResult = false;
    }
}
