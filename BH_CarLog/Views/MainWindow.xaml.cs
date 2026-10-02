using BH_CarLog.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace BH_CarLog.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Closing += MainWindow_Closing;
        }

        /// <summary>
        /// 창 닫기(X) 도 [프로그램 종료] 와 같은 절차를 탄다. (확인 → 서버 로그아웃 → 종료)
        /// MainViewModel.PrepareExitAsync 가 AllowClose 를 켠 뒤 다시 닫아야 실제로 닫힌다.
        /// </summary>
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (DataContext is not MainViewModel vm || vm.AllowClose)
                return;

            e.Cancel = true;
            _ = CloseAsync(vm);
        }

        private async Task CloseAsync(MainViewModel vm)
        {
            if (await vm.PrepareExitAsync())
                Close();
        }
    }
}
