using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Agent_Health
{
    internal class Application_Paths
    {
        //public static string c_temp = @"C:\temp";
        public static string c_temp_netlock_dir = Path.Combine(GetTempPath() ,"netlock rmm");
        public static string c_temp_netlock_logs_dir = Path.Combine(GetTempPath(), "netlock rmm", "installer", "logs");
        public static string c_temp_netlock_installer_dir = Path.Combine(GetTempPath(), "netlock rmm", "installer");
        public static string c_temp_netlock_installer_path = Path.Combine(c_temp_netlock_installer_dir, OperatingSystem.IsWindows() ? "NetLock_RMM_Agent_Installer.exe" : "NetLock_RMM_Agent_Installer");

        public static string installer_package_url_winx64 = "/private/downloads/netlock/installer.package.win-x64.zip";
        public static string installer_package_url_winarm64 = "/private/downloads/netlock/installer.package.win-arm64.zip";
        public static string installer_package_url_linuxx64 = "/private/downloads/netlock/installer.package.linux-x64.zip";
        public static string installer_package_url_linuxarm64 = "/private/downloads/netlock/installer.package.linux-arm64.zip";
        public static string installer_package_url_osx64 = "/private/downloads/netlock/installer.package.osx-x64.zip";
        public static string installer_package_url_osxarm64 = "/private/downloads/netlock/installer.package.osx-arm64.zip";

        public static string installer_package_path = @"installer.package";

        public static string program_data = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Health Agent");
        public static string program_data_logs = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Health Agent", "Logs");
        public static string program_data_debug_txt = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Health Agent", "debug.txt");
        public static string program_data_server_config_json = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Health Agent", "server_config.json");

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

        public static string GetTempPath()
        {
            string basePath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                basePath = @"C:\temp";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                basePath = "/tmp";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                basePath = "/tmp";
            }
            else
            {
                throw new NotSupportedException("Unsupported OS");
            }

            return basePath;
        }
    }
}
