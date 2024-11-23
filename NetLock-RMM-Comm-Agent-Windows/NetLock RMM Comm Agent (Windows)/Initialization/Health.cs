using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Remoting.Messaging;
using NetLock_RMM_Comm_Agent_Windows.Helper;
using System.Diagnostics;

namespace NetLock_RMM_Comm_Agent_Windows.Initialization
{
    internal class Health
    {
        // Check if the directories are in place
        public static void Check_Directories()
        {
            try
            {
                // Program Data
                if (!Directory.Exists(Application_Paths.program_data))
                    Directory.CreateDirectory(Application_Paths.program_data);

                // Logs
                if (!Directory.Exists(Application_Paths.program_data_logs))
                    Directory.CreateDirectory(Application_Paths.program_data_logs);
                
                // Installer
                if (!Directory.Exists(Application_Paths.program_data_installer))
                    Directory.CreateDirectory(Application_Paths.program_data_installer);

                // Updates
                if (!Directory.Exists(Application_Paths.program_data_updates))
                    Directory.CreateDirectory(Application_Paths.program_data_updates);

                // NetLock Temp
                if (!Directory.Exists(Application_Paths.program_data_temp))
                    Directory.CreateDirectory(Application_Paths.program_data_temp);

                // C Temp
                if (!Directory.Exists(Application_Paths.c_temp))
                    Directory.CreateDirectory(Application_Paths.c_temp);

                // Microsoft Defender Antivirus
                if (!Directory.Exists(Application_Paths.program_data_microsoft_defender_antivirus_scan_jobs))
                    Directory.CreateDirectory(Application_Paths.program_data_microsoft_defender_antivirus_scan_jobs);

                // Jobs
                if (!Directory.Exists(Application_Paths.program_data_jobs))
                    Directory.CreateDirectory(Application_Paths.program_data_jobs);

                // Scripts
                if (!Directory.Exists(Application_Paths.program_data_scripts))
                    Directory.CreateDirectory(Application_Paths.program_data_scripts);

                // Sensors
                if (!Directory.Exists(Application_Paths.program_data_sensors))
                    Directory.CreateDirectory(Application_Paths.program_data_sensors);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Initialization.Health.Check_Directories", "", ex.Message);
            }
        }

        // Check if the registry keys are in place
        public static void Check_Registry()
        { 
            try
            {
                // Check if netlock key exists
                if (!Registry.HKLM_Key_Exists(Application_Paths.netlock_reg_path))
                    Registry.HKLM_Create_Key(Application_Paths.netlock_reg_path);

                // Check if msdav key exists
                if (!Registry.HKLM_Key_Exists(Application_Paths.netlock_microsoft_defender_antivirus_reg_path))
                    Registry.HKLM_Create_Key(Application_Paths.netlock_microsoft_defender_antivirus_reg_path);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Initialization.Health.Check_Registry", "", ex.Message);
            }
        }

        // Check if the firewall rules are in place
        public static void Check_Firewall()
        {
            Microsoft_Defender_Firewall.Handler.NetLock_RMM_Comm_Agent_Rule_Inbound();
            Microsoft_Defender_Firewall.Handler.NetLock_RMM_Comm_Agent_Rule_Outbound();
            Microsoft_Defender_Firewall.Handler.NetLock_RMM_Health_Service_Rule();
            Microsoft_Defender_Firewall.Handler.NetLock_Installer_Rule();
            Microsoft_Defender_Firewall.Handler.NetLock_Uninstaller_Rule();
        }

        // Check if the databases are in place
        public static void Check_Databases()
        {
            // Check if the databases are in place
            if (!File.Exists(Application_Paths.program_data_netlock_policy_database))
                Database.NetLock_Data_Setup();

            // Check if the events database is in place
            if (!File.Exists(Application_Paths.program_data_netlock_events_database))
                Database.NetLock_Events_Setup();
        }

        public static void Clean_Service_Restart()
        {
            Logging.Handler.Debug("Initialization.Health.Clean_Service_Restart", "Starting.", "");

            Process cmd_process = new Process();
            cmd_process.StartInfo.UseShellExecute = true;
            cmd_process.StartInfo.CreateNoWindow = true;
            cmd_process.StartInfo.FileName = "cmd.exe";
            cmd_process.StartInfo.Arguments = "/c powershell" + " Stop-Service 'NetLock_RMM_Comm_Agent_Windows'; Remove-Item 'C:\\ProgramData\\0x101 Cyber Security\\NetLock RMM\\Comm Agent\\policy.nlock'; Remove-Item 'C:\\ProgramData\\0x101 Cyber Security\\NetLock RMM\\Comm Agent\\events.nlock'; Start-Service 'NetLock_RMM_Comm_Agent_Windows'";
            cmd_process.Start();
            cmd_process.WaitForExit();

            Logging.Handler.Error("Initialization.Health.Clean_Service_Restart", "Stopping.", "");
        }

        public static void Setup_Events_Virtual_Datatable()
        {
            try
            {
                Service.events_data_table.Columns.Clear();
                Service.events_data_table.Columns.Add("severity");
                Service.events_data_table.Columns.Add("reported_by");
                Service.events_data_table.Columns.Add("event");
                Service.events_data_table.Columns.Add("description");
                Service.events_data_table.Columns.Add("type");
                Service.events_data_table.Columns.Add("language");
                Service.events_data_table.Columns.Add("notification_json");

                Logging.Handler.Debug("Initialization.Health.Setup_Events_Virtual_Datatable", "Create datatable", "Done.");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Initialization.Health.Setup_Events_Virtual_Datatable", "Create datatable", ex.ToString());
            }
        }

        public static void User_Process()
        {
            // Write the NetLock RMM User Process to the registry, if it does not exist
            Logging.Handler.Debug("Initialization.Health.User_Process", "Write to registry", "NetLock RMM User Process");
            Registry.HKLM_Write_Value(Application_Paths.hklm_run_directory_reg_path, "NetLock RMM User Process", Application_Paths.netlock_user_process_exe);
        }
    }
}
