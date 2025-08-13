using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace SCNCore_Plus_Agent_Installer
{
    internal class Application_Paths
    {
        public static string c_temp_scncore_dir = Path.Combine(GetTempPath(), "scncore rmm");
        public static string c_temp_logs_dir = Path.Combine(GetTempPath(), "scncore rmm", "installer", "logs");

        public static string comm_agent_package_url_winx64 = "/private/downloads/scncore/comm.package.win-x64.zip";
        public static string comm_agent_package_url_winarm64 = "/private/downloads/scncore/comm.package.win-arm64.zip";
        public static string comm_agent_package_url_linuxx64 = "/private/downloads/scncore/comm.package.linux-x64.zip";
        public static string comm_agent_package_url_linuxarm64 = "/private/downloads/scncore/comm.package.linux-arm64.zip";
        public static string comm_agent_package_url_osx64 = "/private/downloads/scncore/comm.package.osx-x64.zip";
        public static string comm_agent_package_url_osxarm64 = "/private/downloads/scncore/comm.package.osx-arm64.zip";

        public static string program_files_comm_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), OperatingSystem.IsWindows() ? "0x101 Cyber Security" : "0x101_Cyber_Security", OperatingSystem.IsWindows() ? "SCNCore Plus" : "SCNCore_Plus", OperatingSystem.IsWindows() ? "Comm Agent" : "Comm_Agent");
        public static string program_data_comm_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent");
        public static string program_files_comm_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "SCNCore_Plus_Agent_Comm.exe");
        public static string program_files_comm_agent_service_name_linux = "scncore-plus-agent-comm";
        public static string program_files_comm_agent_service_name_osx = "com.scncore.rmm.agentcomm";
        public static string program_files_comm_agent_service_path_unix = Path.Combine(program_files_comm_agent_dir, "SCNCore_Plus_Agent_Comm");
        public static string program_files_comm_agent_service_config_path_linux = "/etc/systemd/system/scncore-plus-agent-comm.service";
        public static string program_files_comm_agent_service_config_path_osx = $"/Library/LaunchDaemons/{program_files_comm_agent_service_name_osx}.plist";
        public static string program_files_comm_agent_service_log_path_unix = "/var/log/scncore-plus-agent-comm.log";
        
        public static string program_data_comm_agent_logs_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "Logs");
        public static string program_data_comm_agent_jobs_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "Jobs");
        public static string program_data_comm_agent_msdav_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "Microsoft Defender Antivirus");
        public static string program_data_comm_agent_scripts_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "Scripts");
        public static string program_data_comm_agent_backups_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "Backups");
        public static string program_data_comm_agent_sensors_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "Sensors");
        public static string program_data_comm_agent_dumps_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "Dumps");
        public static string program_data_comm_agent_temp_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "Temp");
        public static string program_data_comm_agent_server_config = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "server_config.json");
        public static string program_data_health_agent_server_config = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Health Agent", "server_config.json");
        public static string program_data_comm_agent_just_installed = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "just_installed.txt");

        public static string program_data_comm_agent_events_path = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "events.nlock");
        public static string program_data_comm_agent_policies_path = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "policy.nlock");
        public static string program_data_comm_agent_version_path = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Comm Agent", "version.txt");

        public static string remote_agent_package_url_winx64 = "/private/downloads/scncore/remote.package.win-x64.zip";
        public static string remote_agent_package_url_winarm64 = "/private/downloads/scncore/remote.package.win-arm64.zip";
        public static string remote_agent_package_url_linuxx64 = "/private/downloads/scncore/remote.package.linux-x64.zip";
        public static string remote_agent_package_url_linuxarm64 = "/private/downloads/scncore/remote.package.linux-arm64.zip";
        public static string remote_agent_package_url_osx64 = "/private/downloads/scncore/remote.package.osx-x64.zip";
        public static string remote_agent_package_url_osxarm64 = "/private/downloads/scncore/remote.package.osx-arm64.zip";

        public static string program_files_remote_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), OperatingSystem.IsWindows() ? "0x101 Cyber Security" : "0x101_Cyber_Security", OperatingSystem.IsWindows() ? "SCNCore Plus" : "SCNCore_Plus", OperatingSystem.IsWindows() ? "Remote Agent" : "Remote_Agent");
        public static string program_data_remote_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Remote Agent");
        public static string program_files_remote_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "SCNCore Plus", "Remote Agent", "SCNCore_Plus_Agent_Remote.exe");
        public static string program_files_remote_agent_service_name_linux = "scncore-plus-agent-remote";
        public static string program_files_remote_agent_service_name_osx = "com.scncore.rmm.agentremote";
        public static string program_files_remote_agent_service_path_unix = Path.Combine(program_files_remote_agent_dir, "SCNCore_Plus_Agent_Remote");
        public static string program_files_remote_agent_service_config_path_linux = "/etc/systemd/system/scncore-plus-agent-remote.service";
        public static string program_files_remote_agent_service_config_path_osx = $"/Library/LaunchDaemons/{program_files_remote_agent_service_name_osx}.plist";
        public static string program_files_remote_agent_service_log_path_unix= "/var/log/scncore-plus-agent-remote.log";
        
        public static string health_agent_package_url_winx64 = "/private/downloads/scncore/health.package.win-x64.zip";
        public static string health_agent_package_url_winarm64 = "/private/downloads/scncore/health.package.win-arm64.zip";
        public static string health_agent_package_url_linuxx64 = "/private/downloads/scncore/health.package.linux-x64.zip";
        public static string health_agent_package_url_linuxarm64 = "/private/downloads/scncore/health.package.linux-arm64.zip";
        public static string health_agent_package_url_osx64 = "/private/downloads/scncore/health.package.osx-x64.zip";
        public static string health_agent_package_url_osxarm64 = "/private/downloads/scncore/health.package.osx-arm64.zip";

        public static string program_files_health_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), OperatingSystem.IsWindows() ? "0x101 Cyber Security" : "0x101_Cyber_Security", OperatingSystem.IsWindows() ? "SCNCore Plus" : "SCNCore_Plus", OperatingSystem.IsWindows() ? "Health Agent" : "Health_Agent");
        public static string program_data_health_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "Health Agent");
        public static string program_files_health_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "SCNCore Plus", "Health Agent", "SCNCore_Plus_Agent_Health.exe");
        public static string program_files_health_agent_service_name_linux = "scncore-plus-agent-health";
        public static string program_files_health_agent_service_name_osx = "com.scncore.rmm.agenthealth";
        public static string program_files_health_agent_service_path_unix = Path.Combine(program_files_health_agent_dir, "SCNCore_Plus_Agent_Health");
        public static string program_files_health_agent_service_config_path_linux = "/etc/systemd/system/scncore-plus-agent-health.service";
        public static string program_files_health_agent_service_config_path_osx = $"/Library/LaunchDaemons/{program_files_health_agent_service_name_osx}.plist";
        public static string program_files_health_agent_service_log_path_unix = "/var/log/scncore-plus-agent-health.log";
        
        public static string user_process_package_url_winx64 = "/private/downloads/scncore/user.process.package.win-x64.zip";
        public static string user_process_package_url_winarm64 = "/private/downloads/scncore/user.process.package.win-arm64.zip";
        public static string user_process_package_url_linuxx64 = "/private/downloads/scncore/user.process.package.linux-x64.zip";
        public static string user_process_package_url_linuxarm64 = "/private/downloads/scncore/user.process.package.linux-arm64.zip";
        public static string user_process_package_url_osx64 = "/private/downloads/scncore/user.process.package.osx-x64.zip";
        public static string user_process_package_url_osxarm64 = "/private/downloads/scncore/user.process.package.osx-arm64.zip";

        public static string program_files_user_process_dir = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "SCNCore Plus", "User Process");
        public static string program_data_user_process_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "User Process");
        public static string program_files_user_process_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "SCNCore Plus", "User Process", "SCNCore_Plus_User_Process.exe");

        public static string program_files_user_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "SCNCore Plus", "User Agent");
        public static string program_data_user_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "SCNCore Plus", "User Agent");
        public static string program_files_user_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "SCNCore Plus", "User Agent", "SCNCore_Plus_User_Process.exe");

        public static string comm_agent_package_path = @"comm.package";
        public static string remote_agent_package_path = @"remote.package";
        public static string health_agent_package_path = @"health.package";
        public static string user_process_package_path = @"user_process.package";
        
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
