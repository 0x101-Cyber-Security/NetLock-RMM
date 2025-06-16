using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Agent_Remote
{
    internal class Application_Paths
    {
        public static string program_data = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Remote Agent");
        public static string program_data_logs = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Remote Agent", "Logs");

        public static string program_data_debug_txt = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Remote Agent", "debug.txt");
        public static string program_data_scripts = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Remote Agent", "Scripts");

        public static string netlock_rmm_user_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "User Agent", "NetLock_RMM_User_Process.exe");
        public static string netlock_rmm_user_agent_uac_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "User Agent", "NetLock_RMM_User_Process_UAC.exe");

        private static string GetBasePath_CommonApplicationData()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "/var";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "/Library/Application Support";
            }
            else if (OperatingSystem.IsMacOS())
            {
                return "/Library/Application Support";
            }
            else
            {
                throw new NotSupportedException("Unsupported OS");
            }
        }

        private static string GetBasePath_ProgramFiles()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "/usr";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "/Applications";
            }
            else if (OperatingSystem.IsMacOS())
            {
                return "/Applications";
            }
            else
            {
                throw new NotSupportedException("Unsupported OS");
            }
        }
    }
}
