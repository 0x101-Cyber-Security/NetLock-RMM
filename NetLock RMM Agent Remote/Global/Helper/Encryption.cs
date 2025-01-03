using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Security.Cryptography;

namespace Global.Helper
{
    public static class String_Encryption
    {
        private const int KeySize = 256; // AES-256
        private const int DerivationIterations = 10000; // Higher iterations for better security

        public static string Encrypt(string plainText, string passPhrase)
        {
            try
            {
                var saltBytes = GenerateRandomBytes(32); // 32 bytes for salt
                var ivBytes = GenerateRandomBytes(16);  // 16 bytes for IV (AES block size)
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

                using (var rfc2898 = new Rfc2898DeriveBytes(passPhrase, saltBytes, DerivationIterations))
                {
                    var keyBytes = rfc2898.GetBytes(KeySize / 8);
                    using (var aes = Aes.Create())
                    {
                        aes.KeySize = KeySize;
                        aes.BlockSize = 128;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using (var encryptor = aes.CreateEncryptor(keyBytes, ivBytes))
                        using (var memoryStream = new MemoryStream())
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                            cryptoStream.FlushFinalBlock();

                            var cipherBytes = saltBytes
                                .Concat(ivBytes)
                                .Concat(memoryStream.ToArray())
                                .ToArray();
                            return Convert.ToBase64String(cipherBytes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                GC.Collect();
            }
        }

        public static string Decrypt(string cipherText, string passPhrase)
        {
            try
            {
                var cipherBytesWithSaltAndIv = Convert.FromBase64String(cipherText);

                var saltBytes = cipherBytesWithSaltAndIv.Take(32).ToArray(); // First 32 bytes for salt
                var ivBytes = cipherBytesWithSaltAndIv.Skip(32).Take(16).ToArray(); // Next 16 bytes for IV
                var cipherBytes = cipherBytesWithSaltAndIv.Skip(48).ToArray(); // Remaining bytes for cipher text

                using (var rfc2898 = new Rfc2898DeriveBytes(passPhrase, saltBytes, DerivationIterations))
                {
                    var keyBytes = rfc2898.GetBytes(KeySize / 8);
                    using (var aes = Aes.Create())
                    {
                        aes.KeySize = KeySize;
                        aes.BlockSize = 128;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using (var decryptor = aes.CreateDecryptor(keyBytes, ivBytes))
                        using (var memoryStream = new MemoryStream(cipherBytes))
                        using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            var plainTextBytes = new byte[cipherBytes.Length];
                            var byteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                            return Encoding.UTF8.GetString(plainTextBytes, 0, byteCount);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                GC.Collect();
            }
        }

        private static byte[] GenerateRandomBytes(int size)
        {
            try
            {
                var randomBytes = new byte[size];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                return randomBytes;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
