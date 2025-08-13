using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Google.Authenticator;

namespace Randomizer
{
    public class Handler
    {
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

        public static string Generate_Password(bool special, int length)
        {
            string chars = null;

            System.Random random = new System.Random();
            if (special)
                chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!$%&/()=?*[]#-";
            else
                chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string Token(bool special, int length)
        {
            const string normalChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const string specialChars = "!§$%&/()=?*[]#-";

            string chars = special ? normalChars + specialChars : normalChars;

            var data = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(data);

            var tokenChars = new char[length];
            for (int i = 0; i < length; i++)
            {
                // % chars.Length ensures we pick a valid index
                tokenChars[i] = chars[data[i] % chars.Length];
            }

            return new string(tokenChars);
        }

        public static string Windows_Credentials(int length)
        {
            string chars = null;

            System.Random random = new System.Random();
            chars = "abcdefghijklmnopqrstuvwxyz0123456789";

            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string Number(int length)
        {
            string chars = null;

            System.Random random = new System.Random();
            chars = "0123456789";

            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static string Key(int length)
        {
            byte[] key_bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key_bytes);
            }

            string generated_key = Base32Encoding.ToString(key_bytes);

            return generated_key;
        }
    }
}