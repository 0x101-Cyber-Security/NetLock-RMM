using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Agent_Uninstaller_Windows
{
    internal class Application_Paths
    {
        public static string c_temp_logs = @"C:\temp\netlock rmm\uninstaller\logs";

        public static string program_files_comm_agent_dir = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\Comm Agent";
        public static string program_data_comm_agent_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent";
        public static string program_data_comm_agent_logs_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\Logs";
        public static string program_data_comm_agent_jobs_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\Jobs";
        public static string program_data_comm_agent_msdav_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\Microsoft Defender Antivirus";
        public static string program_data_comm_agent_scripts_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\Scripts";
        public static string program_data_comm_agent_backups_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\Backups";
        public static string program_data_comm_agent_sensors_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\Sensors";
        public static string program_data_comm_agent_dumps_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\Dumps";
        public static string program_data_comm_agent_temp_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\Temp";
        
        public static string program_data_comm_agent_events_path = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\events.nlock";
        public static string program_data_comm_agent_policies_path = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\policy.nlock";
        public static string program_data_comm_agent_server_config_json_path = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\server_config.json";
        public static string program_data_comm_agent_version_path = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Comm Agent\version.txt";

        public static string program_files_remote_agent_dir = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\Remote Agent";
        public static string program_data_remote_agent_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Remote Agent";
        public static string program_data_remote_agent_logs_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Remote Agent\Logs";
        public static string program_data_remote_agent_scripts_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Remote Agent\Scripts";

        public static string program_files_health_agent_dir = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\Health Agent";
        public static string program_data_health_agent_dir = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\Health Agent";
        public static string program_data_health_agent_logs_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Health Agent\Logs";

        public static string program_files_user_process_dir = @"C:\Program Files\0x101 Cyber Security\NetLock RMM\User Process";
        public static string program_data_user_process_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\User Process";
        public static string program_data_user_process_logs_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\User Process\Logs";

        public static string program_files_0x101_cyber_security_dir = @"C:\Program Files\0x101 Cyber Security";
        public static string program_data_0x101_cyber_security_dir = @"C:\ProgramData\0x101 Cyber Security";
    }
}
