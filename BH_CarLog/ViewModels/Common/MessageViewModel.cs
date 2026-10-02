using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BH_CarLog.ViewModels.Common
{
    public enum MessageKind { Info, Warning, Error, Question }
    public enum MessageButtons { Ok, YesNo, YesNoCancel }
    public enum MessageResult { None, Ok, Yes, No, Cancel }

    /// <summary>
    /// 다크 테마 메시지 박스 ViewModel
    /// </summary>
    public partial class MessageViewModel : ObservableObject
    {
        public string Title { get; }
        public string Message { get; }
        public MessageKind Kind { get; }
        public MessageButtons Buttons { get; }

        public bool IsOkVisible => Buttons == MessageButtons.Ok;
        public bool IsYesNoVisible => Buttons != MessageButtons.Ok;
        public bool IsCancelVisible => Buttons == MessageButtons.YesNoCancel;

        public string IconText => Kind switch
        {
            MessageKind.Info => "i",
            MessageKind.Warning => "!",
            MessageKind.Error => "×",
            _ => "?",
        };

        public MessageResult Result { get; private set; } = MessageResult.None;

        [ObservableProperty]
        private bool? _dialogResult;

        public MessageViewModel(string title, string message, MessageKind kind, MessageButtons buttons)
        {
            Title = title;
            Message = message;
            Kind = kind;
            Buttons = buttons;
        }

        [RelayCommand]
        private void Ok()
        {
            Result = MessageResult.Ok;
            DialogResult = true;
        }

        [RelayCommand]
        private void Yes()
        {
            Result = MessageResult.Yes;
            DialogResult = true;
        }

        [RelayCommand]
        private void No()
        {
            Result = MessageResult.No;
            DialogResult = false;
        }

        [RelayCommand]
        private void Cancel()
        {
            Result = MessageResult.Cancel;
            DialogResult = false;
        }
    }
}
