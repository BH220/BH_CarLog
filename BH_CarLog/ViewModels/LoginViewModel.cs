using System.Reflection;
using System.Security.Cryptography;
using BH_CarLog.Api.Interface;
using BH_CarLog.Core.Configurations;
using BH_CarLog.Core.Helper;
using BH_CarLog.Messages;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using BH_CarLog.ViewModels.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace BH_CarLog.ViewModels
{
    /// <summary>로그인 화면 (기존 frmLogin / 화면잠금 시 frmLock)</summary>
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthManager _auth;
        private readonly IDialogService _dialog;
        private readonly IMessenger _messenger;

        /// <summary>설정 파일에서 값을 채우는 동안 저장이 되돌아 실행되지 않게 막는다.</summary>
        private bool _loadingSettings;

        [ObservableProperty]
        private string _userId = "";

        [ObservableProperty]
        private string _password = "";

        [ObservableProperty]
        private string _errorMessage = "";

        [ObservableProperty]
        private bool _isIdFocused;

        [ObservableProperty]
        private bool _isPasswordFocused;

        /// <summary>아이디 저장. 비밀번호는 저장하지 않는다. (기존 chkSaveInfo 를 아이디만으로 축소)</summary>
        [ObservableProperty]
        private bool _isSaveId;

        /// <summary>현재 설정된 API 서버 주소 표시용</summary>
        [ObservableProperty]
        private string _serverAddressText = "";

        /// <summary>화면 잠금 상태에서는 아이디를 바꿀 수 없다.</summary>
        [ObservableProperty]
        private bool _readOnly;

        /// <summary>화면 잠금 중 여부. 잠금 해제는 서버를 거치지 않고 로그인 때의 비밀번호와 비교한다.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HeaderText), nameof(GuideText), nameof(LoginButtonText))]
        private bool _isLocked;

        #region 잠금 해제 비교용 비밀번호 (평문은 보관하지 않는다)
        private const int LockHashIterations = 100_000;
        private byte[]? _lockSalt;
        private byte[]? _lockHash;

        private static byte[] DeriveLockHash(string password, byte[] salt)
            => Rfc2898DeriveBytes.Pbkdf2(password, salt, LockHashIterations, HashAlgorithmName.SHA256, 32);

        /// <summary>로그인 성공 시 호출. 임의 솔트로 해시만 남긴다.</summary>
        private void RememberLockPassword(string password)
        {
            _lockSalt = RandomNumberGenerator.GetBytes(16);
            _lockHash = DeriveLockHash(password, _lockSalt);
        }

        private bool VerifyLockPassword(string password)
        {
            if (_lockSalt == null || _lockHash == null)
                return false;
            return CryptographicOperations.FixedTimeEquals(DeriveLockHash(password, _lockSalt), _lockHash);
        }

        private void ForgetLockPassword()
        {
            _lockSalt = null;
            _lockHash = null;
        }
        #endregion

        public string VersionText => $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)}";

        /// <summary>같은 화면이 로그인과 화면잠금 해제를 겸하므로 문구만 바꿔 쓴다.</summary>
        public string HeaderText => IsLocked ? "화면 잠금" : "로그인";

        public string GuideText => IsLocked
            ? "자리를 비운 사이 화면이 잠겼습니다. 비밀번호를 입력하세요."
            : "아이디와 비밀번호를 입력하세요.";

        public string LoginButtonText => IsLocked ? "잠금 해제" : "로그인";

        public LoginViewModel(IAuthManager auth, IDialogService dialog, IMessenger messenger)
        {
            _auth = auth;
            _dialog = dialog;
            _messenger = messenger;
            RefreshServerAddress();
            LoadSettings();
        }

        #region 아이디 저장 (기존 LoginInfo / LoginOption.txt)
        private void LoadSettings()
        {
            _loadingSettings = true;
            try
            {
                var setting = Config.LoginSetting;
                IsSaveId = setting.IsSaveId;
                UserId = setting.Id;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "로그인 설정 로드 실패");
            }
            finally
            {
                _loadingSettings = false;
            }
        }

        partial void OnIsSaveIdChanged(bool value)
        {
            if (_loadingSettings)
                return;
            SaveSettings();
        }

        private void SaveSettings()
        {
            if (IsLocked)
                return;
            try
            {
                Config.LoginSetting.SetInfo(UserId, IsSaveId);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "로그인 설정 저장 실패");
            }
        }
        #endregion

        /// <summary>기존 프로그램처럼 버튼은 항상 활성화하고, 빈 값은 메시지로 안내한다.</summary>
        [RelayCommand]
        private async Task LoginAsync()
        {
            if (IsBusy)
                return;

            ErrorMessage = "";
            if (string.IsNullOrWhiteSpace(UserId))
            {
                ErrorMessage = "아이디를 입력하세요.";
                IsIdFocused = true;
                return;
            }
            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "비밀번호를 입력하세요.";
                IsPasswordFocused = true;
                return;
            }

            // 화면 잠금 해제: 서버 세션은 살아 있으므로 로그인 때 비밀번호만 확인한다. (기존 frmLock)
            if (IsLocked)
            {
                if (VerifyLockPassword(Password))
                {
                    IsLocked = false;
                    Password = "";
                    Log.Info($"화면 잠금 해제: {UserId}");
                    _messenger.Send(new LoginSucceededMessage());
                }
                else
                {
                    ErrorMessage = "비밀번호가 일치하지 않습니다.";
                    IsPasswordFocused = true;
                }
                return;
            }

            IsBusy = true;
            try
            {
                string userId = UserId.Trim();
                var result = await _auth.LoginAsync(userId, Password);

                // 다른 곳에 세션이 있으면 기존 접속을 끊고 로그인할지 묻는다. (서버 409 DUPLICATE_SESSION)
                if (result.IsDuplicateSession)
                {
                    string detail = "";
                    if (result.ExistingSession != null)
                    {
                        detail = $"\r\n\r\n접속 위치: {result.ExistingSession.ip}";
                        if (result.ExistingSession.loginAt.HasValue)
                            detail += $"\r\n로그인 시각: {result.ExistingSession.loginAt:yyyy-MM-dd HH:mm:ss}";
                    }
                    bool force = _dialog.Confirm($"이미 다른 곳에서 로그인되어 있습니다.{detail}\r\n\r\n기존 접속을 종료하고 로그인하시겠습니까?", "중복 로그인");
                    if (force == false)
                    {
                        await _auth.AbortLoginAsync(userId);
                        ErrorMessage = "로그인을 취소했습니다.";
                        return;
                    }
                    result = await _auth.LoginAsync(userId, Password, force: true);
                }

                if (result.Success)
                {
                    Log.Info($"로그인 성공: {userId}");
                    RememberLockPassword(Password);
                    SaveSettings();
                    ReadOnly = true;
                    Password = "";
                    _messenger.Send(new LoginSucceededMessage());
                }
                else
                {
                    ErrorMessage = result.Message;
                    IsPasswordFocused = true;
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "Login failed");
                ErrorMessage = $"로그인 처리 중 오류가 발생했습니다.\r\n{ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>종료 버튼. 원본과 같이 확인 없이 종료한다.</summary>
        [RelayCommand]
        private void Exit() => _messenger.Send(new ExitRequestedMessage());

        /// <summary>API 서버 주소 설정</summary>
        [RelayCommand]
        private void OpenSettings()
        {
            var vm = new ServerSettingViewModel(_dialog);
            if (_dialog.ShowDialog(vm) == true)
                RefreshServerAddress();
        }

        private void RefreshServerAddress()
        {
            string address = Config.ServerSetting.Address;
            ServerAddressText = string.IsNullOrEmpty(address) ? "서버: 미설정" : $"서버: {address}";
        }

        /// <summary>화면 잠금. 아이디는 고정하고 비밀번호만 다시 받는다. (기존 frmLock)</summary>
        public void Lock()
        {
            IsLocked = true;
            ReadOnly = true;
            Password = "";
            ErrorMessage = "";
            IsPasswordFocused = true;
        }

        /// <summary>로그아웃 후 로그인 화면을 초기화한다. (clearUserId = true 면 잠금 상태도 해제)</summary>
        public void Reset(bool clearUserId)
        {
            if (clearUserId)
            {
                IsLocked = false;
                ReadOnly = false;
                ForgetLockPassword();
                // 아이디 저장이 켜져 있으면 아이디는 남겨 둔다.
                _loadingSettings = true;
                UserId = IsSaveId ? Config.LoginSetting.Id : "";
                _loadingSettings = false;
            }
            Password = "";
            ErrorMessage = "";
            SetInitialFocus();
        }

        /// <summary>아이디가 이미 채워져 있으면 비밀번호로, 비어 있으면 아이디로 포커스를 준다.</summary>
        private void SetInitialFocus()
        {
            bool hasId = string.IsNullOrWhiteSpace(UserId) == false;
            IsIdFocused = hasId == false;
            IsPasswordFocused = hasId;
        }
    }
}
