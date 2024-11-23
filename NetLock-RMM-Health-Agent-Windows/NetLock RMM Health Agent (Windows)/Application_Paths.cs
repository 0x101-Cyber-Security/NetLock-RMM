using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Health_Agent_Windows
{
    internal class Application_Paths
    {
        public static string c_temp = @"C:\temp";
        public static string c_temp_netlock_dir = @"C:\temp\netlock rmm";
        public static string c_temp_netlock_logs_dir = @"C:\temp\netlock rmm\installer\logs";
        public static string c_temp_netlock_installer_dir = @"C:\temp\netlock rmm\installer";
        public static string c_temp_netlock_installer_path = @"C:\temp\netlock rmm\installer\NetLock RMM Agent Installer (Windows).exe";
        public static string installer_package_url = "/private/downloads/netlock/installer.package";
        public static string installer_package_path = @"\installer.package";

        public static string program_data = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Health Agent";
        public static string program_data_logs = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Health Agent\Logs";
        public static string program_data_debug_txt = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Health Agent\debug.txt";
        public static string program_data_server_config_json = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Health Agent\server_config.json";
    }
}
