using System.Security.Cryptography;
using System.Text;

namespace BH_CarLog.Core.Common
{
    /// <summary>
    /// 설정 파일 암호화용 AES 헬퍼. (IsEncryption = true 인 설정에서 Release 빌드 시 사용)
    /// 출력 형식: Base64( IV(16) + CipherText )
    /// </summary>
    public static class AES
    {
        public static string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(plainText))
                return "";

            using var aes = Aes.Create();
            aes.Key = DeriveKey(key);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            byte[] plain = Encoding.UTF8.GetBytes(plainText);
            byte[] cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

            byte[] result = new byte[aes.IV.Length + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(cipherText))
                return "";

            byte[] data = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = DeriveKey(key);

            byte[] iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] plain = decryptor.TransformFinalBlock(data, iv.Length, data.Length - iv.Length);
            return Encoding.UTF8.GetString(plain);
        }

        private static byte[] DeriveKey(string key)
            => SHA256.HashData(Encoding.UTF8.GetBytes(key ?? ""));
    }
}
