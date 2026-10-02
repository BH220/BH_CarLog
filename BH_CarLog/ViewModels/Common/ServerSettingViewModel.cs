using BH_CarLog.Api;
using BH_CarLog.Core.Configurations;
using BH_CarLog.Core.Configurations.Setting;
using BH_CarLog.Core.Helper;
using BH_CarLog.Services;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BH_CarLog.ViewModels.Common
{
    /// <summary>
    /// API 서버 주소 설정. 저장 시 <see cref="ServerSetting"/> JSON 파일에 기록된다.
    /// </summary>
    public partial class ServerSettingViewModel : DialogViewModelBase
    {
        private readonly IDialogService _dialog;

        [ObservableProperty] private string _address = "";
        [ObservableProperty] private string _statusMessage = "";
        [ObservableProperty] private bool _isStatusError;
        [ObservableProperty] private bool _isTesting;
        [ObservableProperty] private bool _isAddressFocused;

        public string SettingFilePath => ServerSetting.FilePath;

        public ServerSettingViewModel(IDialogService dialog)
        {
            _dialog = dialog;
            Title = "API 서버 설정";
            Address = Config.ServerSetting.Address;
            IsAddressFocused = true;
        }

        partial void OnAddressChanged(string value)
        {
            StatusMessage = "";
            IsStatusError = false;
        }

        private bool TryGetAddress(out string address)
        {
            address = ServerSetting.Normalize(Address);
            if (ServerSetting.IsValid(address, out string error) == false)
            {
                StatusMessage = error;
                IsStatusError = true;
                IsAddressFocused = true;
                return false;
            }
            return true;
        }

        /// <summary>입력한 주소로 서버 응답을 확인한다. (저장하지 않음)</summary>
        [RelayCommand]
        private async Task TestAsync()
        {
            if (TryGetAddress(out string address) == false)
                return;

            IsTesting = true;
            StatusMessage = "서버 연결 확인 중...";
            IsStatusError = false;
            try
            {
                // TODO: [API 연동] 서버 상태 확인 전용 API (예: GET /api/health) 로 대체
                bool alive = await Task.Run(() => CarLogApi.Instance.IsAlive(address));
                StatusMessage = alive
                    ? $"서버에 연결되었습니다. ({address})"
                    : "서버에 연결할 수 없습니다. 주소와 네트워크 상태를 확인하세요.";
                IsStatusError = alive == false;
            }
            finally
            {
                IsTesting = false;
            }
        }

        [RelayCommand]
        private void Save()
        {
            if (TryGetAddress(out string address) == false)
                return;

            try
            {
                Config.ServerSetting.Address = address;
                Config.ServerSetting.Save();
                CarLogApi.Instance.SetUrl(address);
                Log.Info($"API 서버 주소 변경: {address} → {ServerSetting.FilePath}");
                DialogResult = true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "ServerSetting save failed");
                _dialog.ShowError($"설정 저장 중 오류가 발생했습니다.\r\n{ex.Message}");
            }
        }
    }
}
