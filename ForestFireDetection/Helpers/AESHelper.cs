using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace ForestFireDetection.Helpers
{
    public static class AESHelper
    {
        private static readonly byte[] Key = new byte[]
        {
            0x6C, 0x6F, 0x76, 0x65, 0x66, 0x6F, 0x72, 0x65,
            0x73, 0x74, 0x65, 0x73, 0x74, 0x31, 0x32, 0x33
        };

        private static readonly byte[] IV = new byte[]
        {
            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
            0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
        };

        public static string? DecryptToRawText(string base64Input)
        {
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(base64Input);

                using Aes aes = Aes.Create();
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.None;

                using var decryptor = aes.CreateDecryptor();
                using var ms = new MemoryStream(cipherBytes);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);

                List<byte> decryptedList = new();
                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    decryptedList.AddRange(buffer[..bytesRead]);
                }

                byte[] decrypted = decryptedList.ToArray();

                // لا نحاول إزالة البادينغ، بل نسمح بفك UTF-8 حتى مع padding عشوائي
                return Encoding.UTF8.GetString(decrypted).Trim('\0', '\n', '\r', '\t');
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ AES Decryption Error (safe fallback): " + ex.Message);
                return null;
            }
        }

    }
}