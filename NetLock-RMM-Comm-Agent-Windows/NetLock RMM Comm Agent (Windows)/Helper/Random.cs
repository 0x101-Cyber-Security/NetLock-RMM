using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Randomizer
{
    public class Handler
    {
        public static async Task<string> Generate_Access_Key(int length)
        {
            string ValidChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            try
            {
                using (var rng = new RNGCryptoServiceProvider())
                {
                    byte[] randomBytes = new byte[length];
                    rng.GetBytes(randomBytes);

                    StringBuilder stringBuilder = new StringBuilder(length);
                    foreach (byte b in randomBytes)
                    {
                        stringBuilder.Append(ValidChars[b % ValidChars.Length]);
                    }

                    return stringBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Randomizer.Handler.Generate_Access_Key", "", ex.Message);
                return String.Empty;
            }
        }

        public static string Standard(int length)
        {
            try
            {
                Random random = new Random();
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                string random_id = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());

                return random_id;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Randomizer.Handler.Standard", "", ex.ToString());
                return "1234567890";
            }
        }
    }
}