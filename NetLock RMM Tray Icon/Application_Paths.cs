using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NetLock_RMM_Agent_Comm
{
    internal class Application_Paths
    {
        // User-mode paths for cross-platform logging
        public static string UserDataPath = GetUserDataPath();
        public static string NetLockUserDir = Path.Combine(GetUserDataPath(), "NetLock RMM");
        public static string LogsDir = Path.Combine(NetLockUserDir, "Logs");
        public static string ConfigDir = Path.Combine(NetLockUserDir, "Config");
        public static string TempDir = Path.Combine(Path.GetTempPath(), "NetLock RMM");
        
        private static string GetUserDataPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, ".local", "share");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(home, "Library", "Application Support");
            }
            else
            {
                throw new NotSupportedException("Unsupported OS");
            }
        }
    }
}
