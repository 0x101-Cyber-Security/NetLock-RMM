using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Global.Helper
{
    internal class IO
    {
        public static string Get_SHA512(string FilePath)
        {
            try
            {
                using (FileStream stream = File.OpenRead(FilePath))
                {
                    SHA512Managed sha512 = new SHA512Managed();
                    byte[] checksum_sha512 = sha512.ComputeHash(stream);
                    string hash = BitConverter.ToString(checksum_sha512).Replace("-", String.Empty);

                    Logging.Debug("Helper.IO.Get_SHA512", "hash", hash);

                    return hash;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.IO.Get_SHA512", "General error", ex.ToString());
                return ex.ToString();
            }
        }

    }
}
