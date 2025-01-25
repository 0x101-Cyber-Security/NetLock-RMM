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
                return ex.Message;
            }
        }

        public static void Delete_File(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                Logging.Debug("Helper.IO.Delete_File", "Deleted file.", path);
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.IO.Delete_File", "Delete file failed: " + path, ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.IO.Delete_File] -> Delete file failed: " + ex.Message);
            }
        }

        public static void Delete_Directory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);

                Logging.Debug("Helper.IO.Delete_Directory", "Deleted directory.", path);
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.IO.Delete_Directory", "Delete directory failed: " + path, ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.IO.Delete_Directory] -> Delete directory failed: " + ex.Message);
            }
        }
    }
}
