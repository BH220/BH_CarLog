using System.Windows;
using BH_CarLog.ViewModels.Car;
using BH_CarLog.ViewModels.Common;
using BH_CarLog.ViewModels.Consumable;
using BH_CarLog.ViewModels.Fuel;
using BH_CarLog.ViewModels.Maintenance;
using BH_CarLog.ViewModels.NewMessage;
using BH_CarLog.ViewModels.Shop;
using BH_CarLog.Views.Car;
using BH_CarLog.Views.Common;
using BH_CarLog.Views.Consumable;
using BH_CarLog.Views.Fuel;
using BH_CarLog.Views.Maintenance;
using BH_CarLog.Views.NewMessage;
using BH_CarLog.Views.Shop;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;

namespace BH_CarLog.Services
{
    public class DialogService : IDialogService
    {
        /// <summary>ViewModel → Window 매핑</summary>
        private static readonly Dictionary<Type, Type> WindowMap = new()
        {
            [typeof(MessageViewModel)] = typeof(MessageWindow),
            [typeof(ImageViewerViewModel)] = typeof(ImageViewerWindow),
            [typeof(ServerSettingViewModel)] = typeof(ServerSettingWindow),
            [typeof(ShopSelectViewModel)] = typeof(ShopSelectWindow),
            [typeof(FuelEditViewModel)] = typeof(FuelEditWindow),
            [typeof(MaintenanceEditViewModel)] = typeof(MaintenanceEditWindow),
            [typeof(ConsumableEditViewModel)] = typeof(ConsumableEditWindow),
            [typeof(ConsumableHistoryViewModel)] = typeof(ConsumableHistoryWindow),
            [typeof(NewMessageDetailViewModel)] = typeof(NewMessageDetailWindow),
            [typeof(CarEditViewModel)] = typeof(CarEditWindow),
            [typeof(CarShopEditViewModel)] = typeof(CarShopEditWindow),
        };

        public void ShowInfo(string message, string title = "알림")
            => ShowMessage(message, title, MessageKind.Info, MessageButtons.Ok);

        public void ShowWarning(string message, string title = "경고")
            => ShowMessage(message, title, MessageKind.Warning, MessageButtons.Ok);

        public void ShowError(string message, string title = "오류")
            => ShowMessage(message, title, MessageKind.Error, MessageButtons.Ok);

        public bool Confirm(string message, string title = "확인")
            => ShowMessage(message, title, MessageKind.Question, MessageButtons.YesNo) == MessageResult.Yes;

        public bool? ConfirmWithCancel(string message, string title = "확인")
        {
            return ShowMessage(message, title, MessageKind.Question, MessageButtons.YesNoCancel) switch
            {
                MessageResult.Yes => true,
                MessageResult.No => false,
                _ => null,
            };
        }

        public bool? ShowDialog(ObservableObject viewModel) => CreateWindow(viewModel).ShowDialog();

        /// <summary>
        /// 모달 아님. WPF 는 소유자(Owner)가 닫히면 소유된 창을 함께 닫으므로, 활성 창을 소유자로 두는 것만으로
        /// "부모 창이 닫히면 같이 닫힌다" 가 된다. 모달 창 위에서 열어도 그 모달 창이 닫힐 때 같이 닫힌다.
        /// 창 닫기는 DialogBehavior 가 DialogResult 설정 실패 시 Close() 로 처리한다.
        /// </summary>
        public void Show(ObservableObject viewModel) => CreateWindow(viewModel).Show();

        private static Window CreateWindow(ObservableObject viewModel)
        {
            if (WindowMap.TryGetValue(viewModel.GetType(), out var windowType) == false)
                throw new InvalidOperationException($"{viewModel.GetType().Name} 에 매핑된 Window 가 없습니다.");

            var window = (Window)Activator.CreateInstance(windowType)!;
            window.DataContext = viewModel;
            window.Owner = GetActiveWindow();
            window.WindowStartupLocation = window.Owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
            return window;
        }

        public string? OpenFile(string filter, string title = "파일 선택")
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                Multiselect = false,
            };
            return dialog.ShowDialog(GetActiveWindow()) == true ? dialog.FileName : null;
        }

        private MessageResult ShowMessage(string message, string title, MessageKind kind, MessageButtons buttons)
        {
            var vm = new MessageViewModel(title, message, kind, buttons);
            ShowDialog(vm);
            return vm.Result;
        }

        private static Window? GetActiveWindow()
        {
            var windows = Application.Current.Windows.OfType<Window>().ToList();
            return windows.FirstOrDefault(x => x.IsActive) ?? Application.Current.MainWindow;
        }
    }

    public class ClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            try
            {
                Clipboard.SetText(text ?? "");
            }
            catch
            {
                // 다른 프로세스가 클립보드를 점유 중인 경우 무시
            }
        }
    }
}
