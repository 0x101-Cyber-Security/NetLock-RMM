using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NetLock_RMM_Agent_Installer_Windows
{
    internal class Application_Paths
    {
        public static string c_temp_netlock_dir = @"C:\temp\netlock rmm";
        public static string c_temp_logs_dir = @"C:\temp\netlock rmm\installer\logs";

        public static string comm_agent_package_url = "/private/downloads/netlock/comm.package";
        public static string program_files_comm_agent_dir = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\Comm Agent";
        public static string program_data_comm_agent_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent";
        public static string program_files_comm_agent_path = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\Comm Agent\NetLock RMM Comm Agent (Windows).exe";
        public static string program_data_comm_agent_server_config = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\server_config.json";
        public static string program_data_health_agent_server_config = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Health Agent\server_config.json";
        public static string program_data_comm_agent_just_installed = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\just_installed.txt";

        public static string remote_agent_package_url = "/private/downloads/netlock/remote.package";
        public static string program_files_remote_agent_dir = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\Remote Agent";
        public static string program_data_remote_agent_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Remote Agent";
        public static string program_files_remote_agent_path = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\Remote Agent\NetLock RMM Remote Agent (Windows).exe";

        public static string health_agent_package_url = "/private/downloads/netlock/health.package";
        public static string program_files_health_agent_dir = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\Health Agent";
        public static string program_data_health_agent_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Health Agent";
        public static string program_files_health_agent_path = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\Health Agent\NetLock RMM Health Agent (Windows).exe";

        public static string user_process_package_url = "/private/downloads/netlock/user_process.package";
        public static string program_files_user_process_dir = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\User Process";
        public static string program_data_user_process_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\User Process";
        public static string program_files_user_process_path = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\User Process\NetLock RMM User Process.exe";

        public static string uninstaller_package_url = "/private/downloads/netlock/uninstaller.package";
        public static string program_data_uninstaller_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Uninstaller";
        public static string c_temp_uninstaller_dir = @"C:\temp\netlock rmm\uninstaller";
        public static string c_temp_uninstaller_path = c_temp_uninstaller_dir + @"\NetLock RMM Agent Uninstaller (Windows).exe";

        public static string comm_agent_package_path = @"\comm.package";
        public static string remote_agent_package_path = @"\remote.package";
        public static string health_agent_package_path = @"\health.package";
        public static string user_process_package_path = @"\user_process.package";
        public static string uninstaller_package_path = @"\uninstaller.package";

        public static string c_temp_server_config_backup_path = @"C:\temp\netlock rmm\installer\server_config.json";
    }
}
