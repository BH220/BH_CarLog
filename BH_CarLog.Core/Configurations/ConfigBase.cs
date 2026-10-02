using BH_CarLog.Core.Common;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Reflection;

namespace BH_CarLog.Core.Configurations
{
    /// <summary>
    /// 설정 객체를 %ProgramData%\BH Soft\settings\{클래스명}.json 으로 저장/복원한다.
    /// 파일이 없거나 손상되면 [DefaultValue] 로 되돌리고 다시 저장한다.
    /// </summary>
    public class ConfigBase
    {
        /// <summary>디버그 빌드는 평문으로 저장해 파일을 바로 열어 볼 수 있게 한다.</summary>
        private static bool UseEncryption(object instance)
        {
#if DEBUG
            return false;
#else
            return instance is IConfig { IsEncryption: true };
#endif
        }

        protected virtual void LoadConfig<T>(T instance) where T : class
        {
            string fileJson = Path.Combine(FilePathHelper.SettingRoot, instance.GetType().Name + ".json");
            if (File.Exists(fileJson) == false)
            {
                ResetToDefault(instance);
                SaveConfig(instance);
                return;
            }

            try
            {
                string strJson = File.ReadAllText(fileJson);
                if (UseEncryption(instance))
                    strJson = AES.Decrypt(strJson, Config.AesKey);

                T? loaded = JsonConvert.DeserializeObject<T>(strJson);
                if (loaded == null)
                    throw new InvalidDataException("내용이 비어 있습니다.");

                foreach (PropertyInfo prop in instance.GetType().GetProperties())
                {
                    if (prop.CanRead && prop.CanWrite)
                        prop.SetValue(instance, prop.GetValue(loaded));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"손상된 {instance.GetType().Name} 파일 :: {ex.Message}");
                ResetToDefault(instance);
                SaveConfig(instance);
            }
        }

        private static void ResetToDefault<T>(T instance) where T : class
        {
            foreach (PropertyInfo prop in instance.GetType().GetProperties())
            {
                if (prop.CanWrite == false)
                    continue;
                var attr = prop.GetCustomAttribute<DefaultValueAttribute>();
                if (attr != null)
                    prop.SetValue(instance, attr.Value);
            }
        }

        protected virtual void SaveConfig<T>(T instance) where T : class
        {
            string strJson = JsonConvert.SerializeObject(instance);
            if (UseEncryption(instance))
                strJson = AES.Encrypt(strJson, Config.AesKey);

            File.WriteAllText(Path.Combine(FilePathHelper.SettingRoot, instance.GetType().Name + ".json"), strJson);
        }
    }
}
