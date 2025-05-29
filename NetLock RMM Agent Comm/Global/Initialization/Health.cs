using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Global.Helper;
using System.Diagnostics;
using NetLock_RMM_Agent_Comm;

namespace Global.Initialization
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
                Console.WriteLine(ex.ToString());
                Logging.Error("Global.Initialization.Health.Check_Directories", "", ex.ToString());
            }
        }

        // Check if the registry keys are in place
        public static void Check_Registry()
        { 
            try
            {
                // Check if netlock key exists
                if (!Windows.Helper.Registry.HKLM_Key_Exists(Application_Paths.netlock_reg_path))
                    Windows.Helper.Registry.HKLM_Create_Key(Application_Paths.netlock_reg_path);

                // Check if msdav key exists
                if (!Windows.Helper.Registry.HKLM_Key_Exists(Application_Paths.netlock_microsoft_defender_antivirus_reg_path))
                    Windows.Helper.Registry.HKLM_Create_Key(Application_Paths.netlock_microsoft_defender_antivirus_reg_path);
            }
            catch (Exception ex)
            {
                Logging.Error("Global.Initialization.Health.Check_Registry", "", ex.ToString());
            }
        }

        // Check if the firewall rules are in place
        public static void Check_Firewall()
        {
            Windows.Microsoft_Defender_Firewall.Handler.NetLock_RMM_Comm_Agent_Rule_Inbound();
            Windows.Microsoft_Defender_Firewall.Handler.NetLock_RMM_Comm_Agent_Rule_Outbound();
            Windows.Microsoft_Defender_Firewall.Handler.NetLock_RMM_Health_Service_Rule();
            Windows.Microsoft_Defender_Firewall.Handler.NetLock_Installer_Rule();
            Windows.Microsoft_Defender_Firewall.Handler.NetLock_Uninstaller_Rule();
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
            Logging.Debug("Global.Initialization.Health.Clean_Service_Restart", "Starting.", "");

            Process cmd_process = new Process();
            cmd_process.StartInfo.UseShellExecute = true;
            cmd_process.StartInfo.CreateNoWindow = true;
            cmd_process.StartInfo.FileName = "cmd.exe";
            cmd_process.StartInfo.Arguments = "/c powershell" + " Stop-Service 'NetLock_RMM_Comm_Agent_Windows'; Remove-Item 'C:\\ProgramData\\0x101 Cyber Security\\NetLock RMM\\Comm Agent\\policy.nlock'; Remove-Item 'C:\\ProgramData\\0x101 Cyber Security\\NetLock RMM\\Comm Agent\\events.nlock'; Start-Service 'NetLock_RMM_Comm_Agent_Windows'";
            cmd_process.Start();
            cmd_process.WaitForExit();

            Logging.Error("Global.Initialization.Health.Clean_Service_Restart", "Stopping.", "");
        }

        public static void Setup_Events_Virtual_Datatable()
        {
            try
            {
                Device_Worker.events_data_table.Columns.Clear();
                Device_Worker.events_data_table.Columns.Add("severity");
                Device_Worker.events_data_table.Columns.Add("reported_by");
                Device_Worker.events_data_table.Columns.Add("event");
                Device_Worker.events_data_table.Columns.Add("description");
                Device_Worker.events_data_table.Columns.Add("type");
                Device_Worker.events_data_table.Columns.Add("language");
                Device_Worker.events_data_table.Columns.Add("notification_json");

                Logging.Debug("Global.Initialization.Health.Setup_Events_Virtual_Datatable", "Create datatable", "Done.");
            }
            catch (Exception ex)
            {
                Logging.Error("Global.Initialization.Health.Setup_Events_Virtual_Datatable", "Create datatable", ex.ToString());
            }
        }
    }
}
