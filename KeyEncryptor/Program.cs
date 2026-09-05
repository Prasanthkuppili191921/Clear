using System;

namespace KeyEncryptor
{
    class Program
    {
        private const string ProtectionPassword =
            "AiInterviewAssistant_OpenRouter_2026_SecureLayer";

        private const string SaltText =
            "AiInterviewAssistant.OpenRouter.Salt.2026";

        static void Main(string[] args)
        {
            Console.WriteLine("====================================");
            Console.WriteLine(" OpenRouter Key Encryptor");
            Console.WriteLine("====================================");
            Console.WriteLine();

            Console.Write("Enter OpenRouter API Key: ");

            string apiKey = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine();
                Console.WriteLine("API key cannot be empty.");
                Console.ReadLine();
                return;
            }

            string encrypted =
                Encrypt(apiKey.Trim());

            Console.WriteLine();
            Console.WriteLine("Encrypted App.config value:");
            Console.WriteLine();
            Console.WriteLine("ENC:" + encrypted);
            Console.WriteLine();
            Console.WriteLine("Copy the complete ENC:... value");
            Console.WriteLine("into App.config.");
            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit.");

            Console.ReadLine();
        }

        private static string Encrypt(string plainText)
        {
            byte[] plainBytes =
                System.Text.Encoding.UTF8.GetBytes(plainText);

            byte[] salt =
                System.Text.Encoding.UTF8.GetBytes(SaltText);

            using (var deriveBytes =
                new System.Security.Cryptography.Rfc2898DeriveBytes(
                    ProtectionPassword,
                    salt,
                    100000))
            {
                byte[] key =
                    deriveBytes.GetBytes(32);

                byte[] iv =
                    deriveBytes.GetBytes(16);

                using (var aes =
                    System.Security.Cryptography.Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode =
                        System.Security.Cryptography.CipherMode.CBC;

                    aes.Padding =
                        System.Security.Cryptography.PaddingMode.PKCS7;

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
    }
}