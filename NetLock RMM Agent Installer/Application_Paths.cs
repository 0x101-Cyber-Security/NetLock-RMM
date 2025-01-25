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

        public static string comm_agent_package_url = "/private/downloads/netlock/comm.package";
        public static string program_files_comm_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent");
        public static string program_files_comm_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent", "NetLock RMM Comm Agent.exe");

        public static string program_data_comm_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Comm Agent");
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

        public static string remote_agent_package_url = "/private/downloads/netlock/remote.package";
        public static string program_files_remote_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Remote Agent");
        public static string program_data_remote_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Remote Agent");
        public static string program_files_remote_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Remote Agent", "NetLock RMM Remote Agent.exe");

        public static string health_agent_package_url = "/private/downloads/netlock/health.package";
        public static string program_files_health_agent_dir = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Health Agent");
        public static string program_data_health_agent_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "Health Agent");
        public static string program_files_health_agent_path = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "Health Agent", "NetLock RMM Health Agent.exe");

        public static string user_process_package_url = "/private/downloads/netlock/user_process.package";
        public static string program_files_user_process_dir = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security", "NetLock RMM", "User Agent");
        public static string program_data_user_process_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "User Agent");
        public static string program_files_user_process_path = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security", "NetLock RMM", "User Agent", "NetLock RMM User Process.exe");

        public static string comm_agent_package_path = @"\comm.package";
        public static string remote_agent_package_path = @"\remote.package";
        public static string health_agent_package_path = @"\health.package";
        public static string user_process_package_path = @"\user_process.package";
        public static string uninstaller_package_path = @"\uninstaller.package";

        public static string c_temp_server_config_backup_path = Path.Combine(GetTempPath(), "netlock rmm", "installer", "server_config.json");

        public static string program_files_0x101_cyber_security_dir = Path.Combine(GetBasePath_ProgramFiles(), "0x101 Cyber Security");
        public static string program_data_0x101_cyber_security_dir = Path.Combine(GetBasePath_CommonApplicationData(), "0x101 Cyber Security");

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
