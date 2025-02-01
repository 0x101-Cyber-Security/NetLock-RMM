using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Agent_Comm
{
    internal class Application_Paths
    {
        // Paths
        public static string c_temp = GetTempPath();
        public static string c_temp_netlock_dir = Path.Combine(GetTempPath(), "netlock rmm");
        public static string c_temp_installer_dir = Path.Combine(GetTempPath(), "netlock rmm", "installer");
        public static string c_temp_installer_path = Path.Combine(c_temp_installer_dir, "NetLock_RMM_Agent_Installer.exe");

        // NetLock Paths
        public static string netlock_service_exe = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "NetLock RMM Comm Agent Windows", "NetLock_RMM_Comm_Agent_Windows.exe");
        public static string netlock_health_service_exe = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Health", "NetLock_RMM_Health_Agent.exe");
        public static string netlock_installer_exe = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Installer", "Installer.exe");
        public static string netlock_uninstaller_exe = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Uninstaller", "Uninstaller.exe");
        public static string netlock_user_process_exe = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "User Process", "NetLock_RMM_User_Process.exe");

        public static string program_data = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent");
        public static string program_data_logs = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Logs");
        public static string program_data_installer = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Installer");
        public static string program_data_updates = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Updates");
        public static string program_data_temp = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Temp");
        public static string program_data_updates_service_package = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Updates", "comm_agent.package");
        public static string program_data_server_config_json = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "server_config.json");
        public static string program_data_debug_txt = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "debug.txt");
        public static string program_data_scripts = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Scripts");
        public static string program_data_sensors = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Sensors");
        public static string program_data_microsoft_defender_antivirus_eventlog_backup = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\Microsoft Defender Antivirus\Microsoft-Windows-Windows Defender Operational.bak";
        public static string program_data_microsoft_defender_antivirus_scan_jobs = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\Microsoft Defender Antivirus\Scan Jobs";
        public static string program_data_jobs = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Jobs");

        public static string program_data_netlock_policy_database = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "policy.nlock");
        public static string program_data_netlock_events_database = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "events.nlock");

        // Installer
        public static string installer_package_url_winx64 = "/private/downloads/netlock/installer.package.win-x64.zip";
        public static string installer_package_url_winarm64 = "/private/downloads/netlock/installer.package.win-arm64.zip";
        public static string installer_package_url_linuxx64 = "/private/downloads/netlock/installer.package.linux-x64.zip";
        public static string installer_package_url_linuxarm64 = "/private/downloads/netlock/installer.package.linux-arm64.zip";
        public static string installer_package_url_osx64 = "/private/downloads/netlock/installer.package.osx-x64.zip";
        public static string installer_package_url_osxarm64 = "/private/downloads/netlock/installer.package.osx-arm64.zip";

        public static string installer_package_path = @"installer.package";

        // Reg Keys
        public static string netlock_reg_path = @"SOFTWARE\WOW6432Node\NetLock RMM\Comm Agent";
        public static string netlock_microsoft_defender_antivirus_reg_path = @"SOFTWARE\WOW6432Node\NetLock RMM\Comm Agent\Microsoft Defender Antivirus";
        public static string netlock_yara_reg_path = @"SOFTWARE\WOW6432Node\NetLock RMM\Comm Agent\YARA";
        public static string netlock_sensors_reg_path = @"SOFTWARE\WOW6432Node\NetLock RMM\Comm Agent\Sensors";
        public static string netlock_sensor_management_reg_path = @"SOFTWARE\WOW6432Node\NetLock RMM\Comm Agent\Sensor_Management";
        public static string netlock_log_connector_reg_path = @"SOFTWARE\WOW6432Node\NetLock RMM\Comm Agent\Log_Connector";
        public static string netlock_rustdesk_reg_path = @"SOFTWARE\WOW6432Node\NetLock RMM\Comm Agent\RustDesk";
        public static string netlock_support_mode_reg_path = @"SOFTWARE\WOW6432Node\NetLock RMM\Comm Agent\Support_Mode";
        public static string hklm_run_directory_reg_path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        // Other
        public static string just_installed = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "just_installed.txt");

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
