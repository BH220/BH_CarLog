using BH_CarLog.Api;
using BH_CarLog.Core.Helper;
using BH_CarLog.Services;
using BH_CarLog.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace BH_CarLog
{
    /// <summary>
    /// 앱 진입점. App.xaml 의 x:Class(BH_CarLog.App)와 네임스페이스가 같아야 생성된 Main 이 이 OnStartup 을 호출한다.
    /// </summary>
    public partial class App : Application
    {
        private const string MutexName = "BH_CarLogs_Wpf_SingleInstance";
        private Mutex? _mutex;

        /// <summary>이 프로세스가 뮤텍스를 소유했는지. 두 번째 인스턴스는 소유하지 못하므로 종료 때 해제하면 안 된다.</summary>
        private bool _ownsMutex;

        public IServiceProvider? Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, MutexName, out _ownsMutex);
            if (_ownsMutex == false)
            {
                MessageBox.Show("BH Car Logs 가 이미 실행 중입니다.", "BH Car Logs", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            ShowConsoleWindow();

            base.OnStartup(e);

            // 창을 직접 띄우므로 종료는 명시적으로 한다. (Window_Closed → Shutdown)
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // 로깅을 먼저 준비해야 DI 구성 중 생기는 오류도 기록된다.
            Log.Configure("BH Car Logs");
            Log.Info("[System] Program Start");

            DispatcherUnhandledException += OnDispatcherUnhandledException;

            Services = DiService.ServicesRegister();

            var view = Services.GetRequiredService<MainWindow>();
            ShowWindow(view);
        }

        private static void ShowWindow(Window window)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Closed += Window_Closed;
            window.WindowState = WindowState.Normal;
            window.Show();
        }

        private static void Window_Closed(object? sender, EventArgs e)
        {
            if (Current == null)
                return;

            // 닫는 동안 컬렉션이 바뀌므로 복사본을 돈다.
            foreach (Window win in Current.Windows.OfType<Window>().ToList())
            {
                if (ReferenceEquals(win, sender) == false)
                    win.Close();
            }

            // Environment.Exit 로 강제 종료하면 OnExit(로그 플러시, 뮤텍스 해제)가 실행되지 않는다. 정상 종료 절차를 탄다.
            Current.Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Info("[System] Program Exit");
            Log.Flush();

            if (_ownsMutex)
            {
                try
                {
                    _mutex?.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // 이미 해제된 경우
                }
            }
            _mutex?.Dispose();
            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var dialog = Services?.GetService<IDialogService>();

            // API 연동 전 기능(ApiNotImplementedException)은 오류가 아닌 "구현 필요" 안내로 보여준다.
            var notImplemented = Find<ApiNotImplementedException>(e.Exception);
            if (notImplemented != null)
            {
                Log.Warn(notImplemented.Message.Replace("\r\n", " "));
                Show(dialog, notImplemented.Message, "구현 필요", MessageBoxImage.Information);
                e.Handled = true;
                return;
            }

            // 서버가 실패로 응답한 경우. 서버 메시지를 그대로 보여 준다.
            var api = Find<ApiException>(e.Exception);
            if (api != null)
            {
                Log.Warn($"API 실패: {api.Message}");
                Show(dialog, api.Message, api.Unauthorized ? "로그인 필요" : "서버 오류", MessageBoxImage.Warning);
                e.Handled = true;
                return;
            }

            Log.Exception(e.Exception, "Unhandled exception");
            Show(dialog, $"처리되지 않은 오류가 발생했습니다.\r\n{e.Exception.Message}", "오류", MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void Show(IDialogService? dialog, string message, string title, MessageBoxImage image)
        {
            if (dialog == null)
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, image);
                return;
            }
            switch (image)
            {
                case MessageBoxImage.Error: dialog.ShowError(message, title); break;
                case MessageBoxImage.Warning: dialog.ShowWarning(message, title); break;
                default: dialog.ShowInfo(message, title); break;
            }
        }

        /// <summary>예외 사슬(Inner / Aggregate)에서 T 를 찾는다.</summary>
        private static T? Find<T>(Exception? ex) where T : Exception
        {
            while (ex != null)
            {
                if (ex is T found)
                    return found;
                if (ex is AggregateException agg && agg.InnerExceptions.Count > 0)
                    ex = agg.InnerExceptions[0];
                else
                    ex = ex.InnerException;
            }
            return null;
        }

        #region 디버그 콘솔
        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            uint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, uint hTemplateFile);

        private const int MY_CODE_PAGE = 949;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_WRITE = 0x2;
        private const uint OPEN_EXISTING = 0x3;

        /// <summary>DEBUG 이거나 실행 폴더에 CTest.dat 가 있으면 콘솔 창을 띄운다.</summary>
        private void ShowConsoleWindow()
        {
            bool openConsole = false;
#if DEBUG
            openConsole = true;
#else
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "CTest.dat"))
                openConsole = true;
#endif
            if (openConsole == false)
                return;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (AllocConsole() == false)
                return;

            IntPtr stdHandle = CreateFile("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, 0, OPEN_EXISTING, 0, 0);
            var safeFileHandle = new SafeFileHandle(stdHandle, true);
            var fileStream = new FileStream(safeFileHandle, FileAccess.Write);
            var standardOutput = new StreamWriter(fileStream, Encoding.GetEncoding(MY_CODE_PAGE)) { AutoFlush = true };
            Console.SetOut(standardOutput);
        }
        #endregion
    }
}
