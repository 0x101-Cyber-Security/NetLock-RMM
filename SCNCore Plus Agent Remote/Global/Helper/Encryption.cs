using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;
using System.Security.Cryptography;
using Global.Helper;

namespace Global.Encryption
{ 
    public static class String_Encryption
    {
        // Configuration: 16-byte salt, 16-byte IV, 32-byte AES key and 32-byte HMAC key (a total of 64 bytes of derived key material)
        private const int SaltSize = 16;
        private const int IvSize = 16;
        private const int AesKeySize = 32;
        private const int HmacKeySize = 32;
        private const int DerivedKeyLength = AesKeySize + HmacKeySize; // 64
        private const int Iterations = 100_000;

        /// <summary>
        /// Encrypts a plaintext string with the specified password.
        /// The result contains salt, IV, ciphertext and an HMAC for integrity checking.
        /// </summary>
        public static string Encrypt(string plainText, string password)
        {
            try
            {
                // Generate random salt
                byte[] salt = GenerateRandomBytes(SaltSize);

                // Derive the entire key material (64 bytes) from the password and salt
                byte[] keyMaterial = DeriveKey(password, salt, DerivedKeyLength);
                byte[] aesKey = new byte[AesKeySize];
                byte[] hmacKey = new byte[HmacKeySize];
                Buffer.BlockCopy(keyMaterial, 0, aesKey, 0, AesKeySize);
                Buffer.BlockCopy(keyMaterial, AesKeySize, hmacKey, 0, HmacKeySize);

                // Generate random initialization vector (IV)
                byte[] iv = GenerateRandomBytes(IvSize);
                byte[] cipherBytes;

                // AES encryption (CBC mode, PKCS7 padding)
                using (Aes aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (MemoryStream ms = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                        cipherBytes = ms.ToArray();
                    }
                }

                // Data on which the HMAC is calculated: Salt + IV + Ciphertext
                byte[] dataToHmac = new byte[salt.Length + iv.Length + cipherBytes.Length];
                Buffer.BlockCopy(salt, 0, dataToHmac, 0, salt.Length);
                Buffer.BlockCopy(iv, 0, dataToHmac, salt.Length, iv.Length);
                Buffer.BlockCopy(cipherBytes, 0, dataToHmac, salt.Length + iv.Length, cipherBytes.Length);

                // Calculate HMAC-SHA256
                byte[] hmac;
                using (var hmacSha256 = new HMACSHA256(hmacKey))
                {
                    hmac = hmacSha256.ComputeHash(dataToHmac);
                }

                // Total encrypted output: Salt || IV || Ciphertext || HMAC
                byte[] result = new byte[dataToHmac.Length + hmac.Length];
                Buffer.BlockCopy(dataToHmac, 0, result, 0, dataToHmac.Length);
                Buffer.BlockCopy(hmac, 0, result, dataToHmac.Length, hmac.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                Logging.Error("Encryption.String_Encryption.Encrypt", "", ex.ToString());

                // Fail-safe: In the event of an error, an exception with additional information is triggered.
                throw new Exception("Encryption failed.", ex);
            }
        }

        /// <summary>
        /// Decrypts an encrypted string (Base64-encoded) with the specified password.
        /// The integrity of the data is checked via HMAC ? an error is triggered in the event of manipulation.
        /// </summary>
        public static string Decrypt(string encryptedText, string password)
        {
            try
            {
                byte[] fullData = Convert.FromBase64String(encryptedText);

                // Check whether the data contains at least the expected lengths (Salt, IV, HMAC)
                if (fullData.Length < SaltSize + IvSize + 1 + 32)
                    throw new Exception("Invalid encrypted text.");

                // Extract Salt, IV, Ciphertext and HMAC
                byte[] salt = new byte[SaltSize];
                Buffer.BlockCopy(fullData, 0, salt, 0, SaltSize);

                byte[] iv = new byte[IvSize];
                Buffer.BlockCopy(fullData, SaltSize, iv, 0, IvSize);

                int cipherLength = fullData.Length - SaltSize - IvSize - 32; // HMAC has 32 bytes
                if (cipherLength <= 0)
                    throw new Exception("Invalid ciphertext length.");

                byte[] cipherBytes = new byte[cipherLength];
                Buffer.BlockCopy(fullData, SaltSize + IvSize, cipherBytes, 0, cipherLength);

                byte[] sentHmac = new byte[32];
                Buffer.BlockCopy(fullData, SaltSize + IvSize + cipherLength, sentHmac, 0, 32);

                // Deriving the key material
                byte[] keyMaterial = DeriveKey(password, salt, DerivedKeyLength);
                byte[] aesKey = new byte[AesKeySize];
                byte[] hmacKey = new byte[HmacKeySize];
                Buffer.BlockCopy(keyMaterial, 0, aesKey, 0, AesKeySize);
                Buffer.BlockCopy(keyMaterial, AesKeySize, hmacKey, 0, HmacKeySize);

                // HMAC calculation for testing the integrity
                byte[] dataToHmac = new byte[SaltSize + IvSize + cipherBytes.Length];
                Buffer.BlockCopy(salt, 0, dataToHmac, 0, SaltSize);
                Buffer.BlockCopy(iv, 0, dataToHmac, SaltSize, IvSize);
                Buffer.BlockCopy(cipherBytes, 0, dataToHmac, SaltSize + IvSize, cipherBytes.Length);

                byte[] computedHmac;
                using (var hmacSha256 = new HMACSHA256(hmacKey))
                {
                    computedHmac = hmacSha256.ComputeHash(dataToHmac);
                }

                // Constant time comparison (fail safe) for HMAC check
                if (!CompareBytes(sentHmac, computedHmac))
                    throw new CryptographicException("Integrity check failed. The data may have been manipulated.");

                // AES decryption
                using (Aes aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (MemoryStream ms = new MemoryStream())
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.FlushFinalBlock();
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Encryption.String_Encryption.Decrypt", "", ex.ToString());

                // Fail-safe: An exception with additional information is thrown in the event of an error
                throw new Exception("Decryption failed.", ex);
            }
        }

        // Auxiliary method: Derivation of the key material with PBKDF2 (SHA-256)
        private static byte[] DeriveKey(string password, byte[] salt, int keyLength)
        {
            try
            {
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    return pbkdf2.GetBytes(keyLength);
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Encryption.String_Encryption.DeriveKey", "", ex.ToString());
                return null;
            }
        }

        // Auxiliary method: Generates a specified number of random bytes
        private static byte[] GenerateRandomBytes(int length)
        {
            try
            {
                byte[] bytes = new byte[length];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(bytes);
                }

                return bytes;
            }
            catch (Exception ex)
            {
                Logging.Error("Encryption.String_Encryption.GenerateRandomBytes", "", ex.ToString());
                return null;
            }
        }

        // Auxiliary method: Comparison of two byte arrays in constant time (prevents timing attacks)
        private static bool CompareBytes(byte[] a, byte[] b)
        {
            try
            {
                if (a.Length != b.Length)
                    return false;
                int diff = 0;
                for (int i = 0; i < a.Length; i++)
                    diff |= a[i] ^ b[i];
                return diff == 0;
            }
            catch (Exception ex)
            {
                Logging.Error("Encryption.String_Encryption.CompareBytes", "", ex.ToString());
                return false;
            }
        }
    }
}
