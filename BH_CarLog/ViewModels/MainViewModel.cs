using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using BH_CarLog.Api;
using BH_CarLog.Api.Interface;
using BH_CarLog.Core.Constants;
using BH_CarLog.Core.Helper;
using BH_CarLog.Core.Session;
using BH_CarLog.Messages;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using BH_CarLog.ViewModels.Car;
using BH_CarLog.ViewModels.Consumable;
using BH_CarLog.ViewModels.Fuel;
using BH_CarLog.ViewModels.Maintenance;
using BH_CarLog.ViewModels.NewMessage;
using BH_CarLog.ViewModels.Shop;
using BH_CarLog.ViewModels.Term;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BH_CarLog.ViewModels
{
    /// <summary>
    /// 메인 화면 (기존 frmMain). 상단 차량 선택 / 좌측 메뉴 / 상단 기능 버튼 / 상태바 / 자동 잠금 타이머 / 로그인 오버레이를 관리한다.
    /// </summary>
    public partial class MainViewModel : ViewModelBase,
        IRecipient<LoginSucceededMessage>,
        IRecipient<UserActivityMessage>,
        IRecipient<StatusTextMessage>,
        IRecipient<CarListChangedMessage>,
        IRecipient<ExitRequestedMessage>
    {
        /// <summary>자동 화면잠금 대기시간(초)</summary>
        private const int LockSeconds = 600;

        private readonly IDialogService _dialog;
        private readonly IAuthManager _auth;
        private readonly IServiceProvider _services;
        private readonly IMessenger _messenger;
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _lockTimer;
        private int _leftSeconds = LockSeconds;
        private int _escClick;

        /// <summary>true 면 창 닫기 시 확인 없이 종료한다.</summary>
        public bool AllowClose { get; private set; }

        public string WindowTitle => "BH Car Logs - 차계부";

        public LoginViewModel Login { get; }
        public ICarContext CarContext { get; }
        public ObservableCollection<MenuItemViewModel> Menus { get; }

        [ObservableProperty]
        private bool _isLoggedIn;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasView))]
        private IFunctionHost? _currentView;

        public bool HasView => CurrentView != null;

        [ObservableProperty]
        private FunctionButtonState _buttons = new();

        [ObservableProperty]
        private string _statusText = "";

        [ObservableProperty]
        private string _userInfoText = "";

        [ObservableProperty]
        private string _loginUserText = "";

        [ObservableProperty]
        private string _nowText = "";

        [ObservableProperty]
        private string _continueText = "";

        public MainViewModel(IDialogService dialog, IAuthManager auth, IServiceProvider services, IMessenger messenger,
            LoginViewModel login, ICarContext carContext)
        {
            _dialog = dialog;
            _auth = auth;
            _services = services;
            _messenger = messenger;
            Login = login;
            CarContext = carContext;

            Menus = new ObservableCollection<MenuItemViewModel>
            {
                new(Core.Menus.새메시지, "새 메시지", "N", "newmessage.png"),
                new(Core.Menus.주유, "주유", "O", "fuel.png"),
                new(Core.Menus.유지보수, "유지보수", "F", "maintenance.png"),
                new(Core.Menus.소모품, "소모품", "C", "consumable.png"),
                new(Core.Menus.교환주기, "주기", "T", "term.png"),
                new(Core.Menus.차량, "차량", "M", "car.png"),
                new(Core.Menus.가게, "가게", "H", "shop.png"),
            };

            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) => NowText = DateTime.Now.ToString("yyyy-MM-dd ddd HH:mm:ss");
            _clockTimer.Start();

            _lockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _lockTimer.Tick += OnLockTimerTick;

            UpdateContinueText();
            _messenger.RegisterAll(this);

            Login.Reset(clearUserId: true);
        }

        #region 메뉴 / 기능 실행
        [RelayCommand]
        private async Task SelectMenuAsync(Core.Menus? menuId)
        {
            if (IsLoggedIn == false || menuId == null)
                return;

            _escClick = 0;
            IFunctionHost? view = menuId switch
            {
                Core.Menus.새메시지 => _services.GetRequiredService<NewMessageListViewModel>(),
                Core.Menus.주유 => _services.GetRequiredService<FuelListViewModel>(),
                Core.Menus.유지보수 => _services.GetRequiredService<MaintenanceListViewModel>(),
                Core.Menus.소모품 => _services.GetRequiredService<ConsumableListViewModel>(),
                Core.Menus.교환주기 => _services.GetRequiredService<TermListViewModel>(),
                Core.Menus.차량 => _services.GetRequiredService<CarListViewModel>(),
                Core.Menus.가게 => _services.GetRequiredService<CarShopListViewModel>(),
                _ => null,
            };

            if (view == null)
            {
                _dialog.ShowError($"[{menuId}] 메뉴는 현재 준비중 입니다.");
                return;
            }

            CloseView();
            CurrentView = view;
            Buttons = view.Buttons;
            foreach (var menu in Menus)
                menu.IsSelected = menu.MenuId == menuId;

            ResetLockTimer();
            await view.InitializeAsync();
        }

        [RelayCommand]
        private async Task RunFunctionAsync(string? functionId)
        {
            if (IsLoggedIn == false || string.IsNullOrEmpty(functionId))
                return;

            if (functionId == Functions.닫기)
            {
                if (CurrentView == null || Buttons.CloseEnabled == false)
                    return;
                // ESC 두 번 누르면 화면을 닫는다. (기존 동작 유지)
                if (_escClick > 0)
                {
                    _escClick = 0;
                    CloseView();
                }
                else
                {
                    _escClick++;
                    StatusText = "ESC 를 한 번 더 누르면 현재 화면을 닫습니다.";
                }
                return;
            }

            _escClick = 0;
            if (CurrentView == null)
                return;

            ResetLockTimer();
            await CurrentView.RunFunctionAsync(functionId);
        }

        [RelayCommand]
        private void CloseView()
        {
            // 내려가는 화면의 메시지 구독을 끊는다. (Transient 라 다음에 열면 새 인스턴스가 만들어진다)
            CurrentView?.Deactivate();
            CurrentView = null;
            Buttons = new FunctionButtonState();
            foreach (var menu in Menus)
                menu.IsSelected = false;
            StatusText = "";
        }
        #endregion

        #region 잠금 / 로그아웃 / 종료
        [RelayCommand]
        private void LockScreen()
        {
            if (IsLoggedIn == false)
                return;
            Log.Info("화면 잠금");
            CloseView();
            _lockTimer.Stop();
            IsLoggedIn = false;
            Login.Lock();
        }

        [RelayCommand]
        private void ExtendTime()
        {
            if (IsLoggedIn == false)
                return;
            ResetLockTimer();
        }

        /// <summary>로그아웃 버튼(Alt+O)</summary>
        [RelayCommand]
        public async Task LogoutAsync()
        {
            if (IsLoggedIn == false)
                return;
            if (_dialog.Confirm("로그아웃 하시겠습니까?", "로그아웃") == false)
                return;

            Log.Info($"로그아웃: {SessionManager.Instance.ID}");
            await _auth.LogoutAsync();
            CloseView();
            _lockTimer.Stop();
            IsLoggedIn = false;
            UserInfoText = "";
            LoginUserText = "";
            CarContext.Cars.Clear();
            CarContext.SelectedCar = null;
            Login.Reset(clearUserId: true);
        }

        /// <summary>프로그램 종료 버튼. 확인 → 서버 로그아웃 → 종료</summary>
        [RelayCommand]
        private async Task ExitAsync()
        {
            if (await PrepareExitAsync())
                Application.Current.Shutdown();
        }

        /// <summary>
        /// 종료 준비. (확인 →) 서버 세션 로그아웃 → <see cref="AllowClose"/> 켬.
        /// 화면 잠금 상태는 IsLoggedIn 이 false 지만 서버 세션은 살아 있으므로 세션 기준으로 로그아웃한다.
        /// </summary>
        public async Task<bool> PrepareExitAsync(bool confirm = true)
        {
            if (AllowClose)
                return true;
            if (confirm && _dialog.Confirm("프로그램을 종료하시겠습니까?", "프로그램 종료") == false)
                return false;

            if (SessionManager.Instance.IsLive)
            {
                try
                {
                    Log.Info($"종료 로그아웃: {SessionManager.Instance.ID}");
                    await _auth.LogoutAsync();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "종료 시 로그아웃 실패");
                }
            }

            _lockTimer.Stop();
            IsLoggedIn = false;
            AllowClose = true;
            return true;
        }
        #endregion

        #region 잠금 타이머
        private void OnLockTimerTick(object? sender, EventArgs e)
        {
            _leftSeconds--;
            if (_leftSeconds < 0)
            {
                _lockTimer.Stop();
                LockScreen();
            }
            else
            {
                UpdateContinueText();
            }
        }

        private void ResetLockTimer()
        {
            _leftSeconds = LockSeconds;
            UpdateContinueText();
        }

        private void UpdateContinueText()
            => ContinueText = $"{_leftSeconds / 60}:{_leftSeconds % 60:00} 연장하기";
        #endregion

        #region 메시지 수신
        public void Receive(LoginSucceededMessage message)
        {
            var session = SessionManager.Instance;
            IsLoggedIn = true;
            LoginUserText = $"{session.Name} 님 로그인 중..";
            UserInfoText = $"접속자: {session.Name} [{session.ID}]";
            StatusText = "메뉴를 선택하세요.";
            ResetLockTimer();
            _lockTimer.Start();
            _ = LoadCarsAsync();
        }

        public void Receive(UserActivityMessage message) => ResetLockTimer();

        public void Receive(StatusTextMessage message) => StatusText = message.Text;

        public void Receive(CarListChangedMessage message) => _ = ReloadCarsAsync();

        /// <summary>로그인 화면의 종료 버튼. 확인 없이 종료하되 잠금 상태의 서버 세션은 로그아웃한다.</summary>
        public void Receive(ExitRequestedMessage message) => _ = ExitFromLoginAsync();

        private async Task ExitFromLoginAsync()
        {
            await PrepareExitAsync(confirm: false);
            Application.Current.Shutdown();
        }
        #endregion

        /// <summary>
        /// 로그인 직후 차량 목록을 읽는다. 등록된 차가 없으면 차량 화면을 먼저 띄운다. (기존 frmMain.LoadCarInfo)
        /// </summary>
        private async Task LoadCarsAsync()
        {
            // 조회 자체가 실패한 경우는 "등록된 차가 없다" 와 다르다. 실패면 안내만 하고 차량 화면을 강제로 열지 않는다.
            if (await ReloadCarsAsync() == false)
                return;

            if (CarContext.Cars.Count == 0)
            {
                _dialog.ShowWarning("등록된 차량이 없습니다.\r\n차량 정보를 먼저 등록하세요.");
                await SelectMenuAsync(Core.Menus.차량);
            }
        }

        /// <summary>차량 목록을 다시 읽는다. 서버 실패는 메시지로 알리고 false 를 돌려준다. (기존 목록은 CarContext 가 유지한다)</summary>
        private async Task<bool> ReloadCarsAsync()
        {
            try
            {
                await CarContext.ReloadAsync();
                return true;
            }
            catch (ApiException ex)
            {
                if (ex.Unauthorized)
                {
                    Log.Warn("차량 목록 조회 - 세션 만료");
                    StatusText = "세션이 만료되었습니다. 다시 로그인하세요.";
                }
                else
                {
                    Log.Warn($"차량 목록 조회 실패: {ex.Message}");
                    StatusText = "차량 목록 조회 실패";
                    _dialog.ShowError($"차량 목록을 불러오지 못했습니다.\r\n{ex.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "차량 목록 조회 실패");
                StatusText = "차량 목록 조회 실패";
                _dialog.ShowError($"차량 목록을 불러오는 중 오류가 발생했습니다.\r\n{ex.Message}");
                return false;
            }
        }
    }
}
