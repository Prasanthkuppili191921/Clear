using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace AiInterviewAssistant.Security
{
    /// <summary>
    /// Portable protection layer for the OpenRouter API key.
    ///
    /// The encrypted value is stored in App.config.
    /// This class is completely standalone and can be removed later.
    /// </summary>
    public static class OpenRouterKeyProtection
    {
        // =========================================================
        // CONFIG
        // =========================================================

        private const string ConfigKey = "OpenRouterKey";

        private const string Prefix = "ENC:";

        // IMPORTANT:
        // This key is only used to make the config value unreadable.
        // It is NOT equivalent to DPAPI security.
        //
        // Do not change this after generating your encrypted value,
        // otherwise previously encrypted keys cannot be decrypted.
        private const string ProtectionPassword =
            "AiInterviewAssistant_OpenRouter_2026_SecureLayer";

        // =========================================================
        // PUBLIC
        // =========================================================

        public static string GetKey()
        {
            try
            {
                string configuredValue =
                    ConfigurationManager
                        .AppSettings[ConfigKey];

                if (string.IsNullOrWhiteSpace(configuredValue))
                    return string.Empty;

                // -------------------------------------------------
                // Backward compatibility:
                // If the value is not encrypted, return it as-is.
                // This prevents existing functionality from breaking.
                // -------------------------------------------------

                if (!configuredValue.StartsWith(
                        Prefix,
                        StringComparison.Ordinal))
                {
                    return configuredValue;
                }

                string encryptedValue =
                    configuredValue.Substring(Prefix.Length);

                return Decrypt(encryptedValue);
            }
            catch
            {
                // Never crash the application because of
                // the optional security layer.
                return string.Empty;
            }
        }

        // =========================================================
        // ENCRYPT
        // =========================================================

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] plainBytes =
                Encoding.UTF8.GetBytes(plainText);

            byte[] salt =
                Encoding.UTF8.GetBytes(
                    "AiInterviewAssistant.OpenRouter.Salt.2026");

            using (var deriveBytes =
                new Rfc2898DeriveBytes(
                    ProtectionPassword,
                    salt,
                    100000))
            {
                byte[] key =
                    deriveBytes.GetBytes(32);

                byte[] iv =
                    deriveBytes.GetBytes(16);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor =
                        aes.CreateEncryptor())
                    {
                        byte[] encrypted =
                            encryptor.TransformFinalBlock(
                                plainBytes,
                                0,
                                plainBytes.Length);

                        return Convert.ToBase64String(
                            encrypted);
                    }
                }
            }
        }

        // =========================================================
        // DECRYPT
        // =========================================================

        private static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            byte[] encryptedBytes =
                Convert.FromBase64String(encryptedText);

            byte[] salt =
                Encoding.UTF8.GetBytes(
                    "AiInterviewAssistant.OpenRouter.Salt.2026");

            using (var deriveBytes =
                new Rfc2898DeriveBytes(
                    ProtectionPassword,
                    salt,
                    100000))
            {
                byte[] key =
                    deriveBytes.GetBytes(32);

                byte[] iv =
                    deriveBytes.GetBytes(16);

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor =
                        aes.CreateDecryptor())
                    {
                        byte[] decrypted =
                            decryptor.TransformFinalBlock(
                                encryptedBytes,
                                0,
                                encryptedBytes.Length);

                        return Encoding.UTF8.GetString(
                            decrypted);
                    }
                }
            }
        }

        // =========================================================
        // HELPER FOR ONE-TIME ENCRYPTION
        // =========================================================

        public static string CreateConfigValue(
            string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return string.Empty;

            return Prefix + Encrypt(apiKey.Trim());
        }
    }
}
