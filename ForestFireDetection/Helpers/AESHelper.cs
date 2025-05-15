using System;
using System.Security.Cryptography;
using System.Text;

namespace ForestFireDetection.Helpers 
{
    public static class AESHelper
    {
        private static readonly byte[] AES_KEY = new byte[] {
            0x6C, 0x6F, 0x76, 0x65, 0x66, 0x6F, 0x72, 0x65,
            0x73, 0x74, 0x65, 0x73, 0x74, 0x31, 0x32, 0x33
        };

        private static readonly byte[] AES_IV = new byte[] {
            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
            0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
        };

        public static (string? json, string raw) DecryptBase64(string base64Input)
        {
            try
            {
                byte[] encrypted = Convert.FromBase64String(base64Input);

                using var aes = Aes.Create();
                aes.Key = AES_KEY;
                aes.IV = AES_IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

                string rawText = Encoding.UTF8.GetString(decrypted);
                string? jsonBlock = ExtractJsonBlock(rawText);

                return (jsonBlock, rawText);
            }
            catch (Exception ex)
            {
                return (null, $"❌ AES decryption error: {ex.Message}");
            }
        }

        private static string? ExtractJsonBlock(string raw)
        {
            int start = raw.IndexOf("{");
            int end = raw.LastIndexOf("}");

            if (start >= 0 && end > start)
            {
                string json = raw.Substring(start, end - start + 1);
                return json;
            }

            return null;
        }

    }
}
