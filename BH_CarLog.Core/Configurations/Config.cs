using BH_CarLog.Core.Configurations.Setting;

namespace BH_CarLog.Core.Configurations
{
    /// <summary>
    /// 프로그램 설정값을 관리하는 클래스
    /// </summary>
    public class Config
    {
        /// <summary>암호화 설정 파일(IsEncryption = true)에 사용하는 키. 현재 두 설정 모두 평문이라 쓰이지 않는다.</summary>
        public const string AesKey = "BH_CarLogs::Settings::v1";

        private static ServerSetting? _serverSetting;

        /// <summary>API 서버 설정. 최초 접근 시 JSON 파일에서 로드된다. (<see cref="ServerSetting.FilePath"/>)</summary>
        public static ServerSetting ServerSetting
        {
            get
            {
                if (_serverSetting == null)
                {
                    _serverSetting = new ServerSetting();
                    _serverSetting.Load();
                }
                return _serverSetting;
            }
        }

        private static LoginSetting? _loginSetting;

        /// <summary>로그인 화면 설정(아이디 저장). 최초 접근 시 JSON 파일에서 로드된다. (<see cref="LoginSetting.FilePath"/>)</summary>
        public static LoginSetting LoginSetting
        {
            get
            {
                if (_loginSetting == null)
                {
                    _loginSetting = new LoginSetting();
                    _loginSetting.Load();
                }
                return _loginSetting;
            }
        }
    }
}
