using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace NetLock_RMM_Agent_Installer
{
    internal class Application_Paths
    {
        public static string c_temp_netlock_dir = Path.Combine(GetTempPath(), "netlock rmm");
        public static string c_temp_logs_dir = Path.Combine(GetTempPath(), "netlock rmm", "installer", "logs");

        // Agent Bundle URLs (contain all agent per platform)
        public static string agent_package_url_winx64 = "/private/downloads/netlock/agent.package.win-x64.zip";
        public static string agent_package_url_winarm64 = "/private/downloads/netlock/agent.package.win-arm64.zip";
        public static string agent_package_url_linuxx64 = "/private/downloads/netlock/agent.package.linux-x64.zip";
        public static string agent_package_url_linuxarm64 = "/private/downloads/netlock/agent.package.linux-arm64.zip";
        public static string agent_package_url_osx64 = "/private/downloads/netlock/agent.package.osx-x64.zip";
        public static string agent_package_url_osxarm64 = "/private/downloads/netlock/agent.package.osx-arm64.zip";

        public static string program_files_comm_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), OperatingSystem.IsWindows() ? "0x101 Cyber Security" : "0x101_Cyber_Security", OperatingSystem.IsWindows() ? "NetLock RMM" : "NetLock_RMM", OperatingSystem.IsWindows() ? "Comm Agent" : "Comm_Agent");
        public static string program_data_comm_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent");
        public static string program_files_comm_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "NetLock_RMM_Agent_Comm.exe");
        public static string program_files_comm_agent_service_name_linux = "netlock-rmm-agent-comm";
        public static string program_files_comm_agent_service_name_osx = "com.netlock.rmm.agentcomm";
        public static string program_files_comm_agent_service_path_unix = Path.Combine(program_files_comm_agent_dir, "NetLock_RMM_Agent_Comm");
        public static string program_files_comm_agent_service_config_path_linux = "/etc/systemd/system/netlock-rmm-agent-comm.service";
        public static string program_files_comm_agent_service_config_path_osx = $"/Library/LaunchDaemons/{program_files_comm_agent_service_name_osx}.plist";
        public static string program_files_comm_agent_service_log_path_unix = "/var/log/netlock-rmm-agent-comm.log";
        
        public static string program_data_comm_agent_logs_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Logs");
        public static string program_data_comm_agent_jobs_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Jobs");
        public static string program_data_comm_agent_msdav_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Microsoft Defender Antivirus");
        public static string program_data_comm_agent_scripts_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Scripts");
        public static string program_data_comm_agent_backups_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Backups");
        public static string program_data_comm_agent_sensors_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Sensors");
        public static string program_data_comm_agent_dumps_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Dumps");
        public static string program_data_comm_agent_temp_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "Temp");
        public static string program_data_comm_agent_server_config = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "server_config.json");
        public static string program_data_health_agent_server_config = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Health Agent", "server_config.json");
        public static string program_data_comm_agent_just_installed = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "just_installed.txt");

        public static string program_data_comm_agent_events_path = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "events.nlock");
        public static string program_data_comm_agent_policies_path = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "policy.nlock");
        public static string program_data_comm_agent_version_path = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "version.txt");

        public static string program_files_remote_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), OperatingSystem.IsWindows() ? "0x101 Cyber Security" : "0x101_Cyber_Security", OperatingSystem.IsWindows() ? "NetLock RMM" : "NetLock_RMM", OperatingSystem.IsWindows() ? "Remote Agent" : "Remote_Agent");
        public static string program_data_remote_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Remote Agent");
        public static string program_files_remote_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Remote Agent", "NetLock_RMM_Agent_Remote.exe");
        public static string program_files_remote_agent_service_name_linux = "netlock-rmm-agent-remote";
        public static string program_files_remote_agent_service_name_osx = "com.netlock.rmm.agentremote";
        public static string program_files_remote_agent_service_path_unix = Path.Combine(program_files_remote_agent_dir, "NetLock_RMM_Agent_Remote");
        public static string program_files_remote_agent_service_config_path_linux = "/etc/systemd/system/netlock-rmm-agent-remote.service";
        public static string program_files_remote_agent_service_config_path_osx = $"/Library/LaunchDaemons/{program_files_remote_agent_service_name_osx}.plist";
        public static string program_files_remote_agent_service_log_path_unix= "/var/log/netlock-rmm-agent-remote.log";
        
        public static string program_files_health_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), OperatingSystem.IsWindows() ? "0x101 Cyber Security" : "0x101_Cyber_Security", OperatingSystem.IsWindows() ? "NetLock RMM" : "NetLock_RMM", OperatingSystem.IsWindows() ? "Health Agent" : "Health_Agent");
        public static string program_data_health_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Health Agent");
        public static string program_files_health_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Health Agent", "NetLock_RMM_Agent_Health.exe");
        public static string program_files_health_agent_service_name_linux = "netlock-rmm-agent-health";
        public static string program_files_health_agent_service_name_osx = "com.netlock.rmm.agenthealth";
        public static string program_files_health_agent_service_path_unix = Path.Combine(program_files_health_agent_dir, "NetLock_RMM_Agent_Health");
        public static string program_files_health_agent_service_config_path_linux = "/etc/systemd/system/netlock-rmm-agent-health.service";
        public static string program_files_health_agent_service_config_path_osx = $"/Library/LaunchDaemons/{program_files_health_agent_service_name_osx}.plist";
        public static string program_files_health_agent_service_log_path_unix = "/var/log/netlock-rmm-agent-health.log";
        
        public static string program_files_user_process_dir = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "User Process");
        public static string program_data_user_process_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "User Process");
        public static string program_files_user_process_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "User Process", "NetLock_RMM_User_Process.exe");

        public static string program_files_user_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "User Agent");
        public static string program_data_user_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "User Agent");
        public static string program_files_user_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "User Agent", "NetLock_RMM_User_Process.exe");

        public static string program_files_tray_icon_dir =  Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Tray Icon");
        public static string program_files_tray_icon_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Tray Icon", "NetLock_RMM_Tray_Icon.exe");
        
        // Agent Bundle package paths
        public static string agent_package_path = @"agent.package";
        
        public static string program_files_0x101_cyber_security_dir = Path.Combine(GetBasePath_ProgramFiles(), OperatingSystem.IsWindows() ? "0x101 Cyber Security" : "0x101_Cyber_Security");
        public static string program_data_0x101_cyber_security_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security");

        // Windows registry
        public static string hklm_run_directory_reg_path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

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
            else if (OperatingSystem.IsMacOS())
            {
                return "/usr/local/bin";
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
