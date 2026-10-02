using System.ComponentModel;
using BH_CarLog.Core.Common;

namespace BH_CarLog.Core.Configurations.Setting
{
    /// <summary>
    /// 로그인 화면의 아이디 저장 설정. (기존 BH_CarBooks 의 LoginInfo / LoginOption.txt)
    /// 아이디만 저장한다. 비밀번호는 저장하지 않으므로 암호화할 것도 없다.
    /// </summary>
    public class LoginSetting : ConfigBase, IConfig
    {
        /// <summary>설정 파일 경로: %ProgramData%\BH Soft\settings\LoginSetting.json</summary>
        public static string FilePath => Path.Combine(FilePathHelper.SettingRoot, nameof(LoginSetting) + ".json");

        [DefaultValue("")]
        public string Id { get; set; } = "";

        /// <summary>아이디 저장. 켜면 다음 실행 때 아이디가 채워지고 비밀번호로 포커스가 간다.</summary>
        [DefaultValue(false)]
        public bool IsSaveId { get; set; }

        public bool IsEncryption => false;

        public void Load() => base.LoadConfig<LoginSetting>(this);

        public void Save() => base.SaveConfig<LoginSetting>(this);

        /// <summary>끄면 저장해 둔 아이디도 지운다.</summary>
        public void SetInfo(string id, bool saveId)
        {
            IsSaveId = saveId;
            Id = saveId ? (id ?? "").Trim() : "";
            Save();
        }
    }
}
