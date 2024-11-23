using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NetLock_RMM_Agent_Uninstaller_Windows.Helper
{
    internal class IO
    {
        public static void Delete_File(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);

                Logging.Handler.Debug("Mode: Fix", "Deleted file.", path);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Mode: Fix", "Delete file failed: " + path, ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Mode: Fix] -> Delete file failed: " + ex.Message);
            }
        }

        public static void Delete_Directory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);

                Logging.Handler.Debug("Mode: Fix", "Deleted directory.", path);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Mode: Fix", "Delete directory failed: " + path, ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Mode: Fix] -> Delete directory failed: " + ex.Message);
            }
        }
    }
}
