using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BackupSyncApp.Services
{
    /// Сервис для безопасного шифрования данных через Windows DPAPI
    /// Требование 1.3.6 ВКР: пароль не хранится в открытом виде

    public interface IDpapiService
    {
        byte[] Encrypt(string plainText);
        string Decrypt(byte[] encryptedData);
    }

    public class DpapiService: IDpapiService
    {
        public byte[] Encrypt(string plainText)
        {
            if(string.IsNullOrEmpty(plainText)) return Array.Empty<byte>();

            byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);

            try
            {
                // DataProtectionScope.CurrentUser — расшифровать может только этот пользователь
                byte[] encrypted = ProtectedData.Protect(
                    plainBytes,
                    null,// no additional entropy-key
                    DataProtectionScope.CurrentUser);

                return encrypted;
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException($"DPAPI encryption failed: {ex.Message}", ex);
            }
        }

        public string Decrypt(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0) return string.Empty;

            try
            {
                byte[] decrypted = ProtectedData.Unprotect(
                    encryptedData,
                    null,
                    DataProtectionScope.CurrentUser);

                return System.Text.Encoding.UTF8.GetString(decrypted);
            }
            catch (CryptographicException)
            {
                return string.Empty;
            }
        }
    }
}
