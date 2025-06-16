using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Global.Helper;
using Windows.Helper;
using NetLock_RMM_Agent_Comm;

namespace Windows.Initialization.Health
{
    internal class Handler
    {
        // Check if the directories are in place
        public static void Check_Directories()
        {
            try
            {
                // C Temp
                if (!Directory.Exists(Application_Paths.c_temp))
                    Directory.CreateDirectory(Application_Paths.c_temp);

                // Microsoft Defender Antivirus
                if (!Directory.Exists(Application_Paths.program_data_microsoft_defender_antivirus_scan_jobs))
                    Directory.CreateDirectory(Application_Paths.program_data_microsoft_defender_antivirus_scan_jobs);
            }
            catch (Exception ex)
            {
                Logging.Error("Initialization.Health.Check_Directories", "", ex.ToString());
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
                Logging.Error("Initialization.Health.Check_Registry", "", ex.ToString());
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

        public static void Clean_Service_Restart()
        {
            Logging.Debug("Initialization.Health.Clean_Service_Restart", "Starting.", "");

            Process cmd_process = new Process();
            cmd_process.StartInfo.UseShellExecute = true;
            cmd_process.StartInfo.CreateNoWindow = true;
            cmd_process.StartInfo.FileName = "cmd.exe";
            cmd_process.StartInfo.Arguments = "/c powershell" + " Stop-Service 'NetLock_RMM_Comm_Agent_Windows'; Remove-Item 'C:\\ProgramData\\0x101 Cyber Security\\NetLock RMM\\Comm Agent\\policy.nlock'; Remove-Item 'C:\\ProgramData\\0x101 Cyber Security\\NetLock RMM\\Comm Agent\\events.nlock'; Start-Service 'NetLock_RMM_Comm_Agent_Windows'";
            cmd_process.Start();
            cmd_process.WaitForExit();

            Logging.Error("Initialization.Health.Clean_Service_Restart", "Stopping.", "");
        }

        public static void User_Process()
        {
            // Delete old NetLock RMM User Agent from the registry, if it exists
            Registry.HKLM_Delete_Value(Application_Paths.hklm_run_directory_reg_path, "NetLock RMM User Process");

            // Write the NetLock RMM User Process to the registry, if it does not exist
            Logging.Debug("Initialization.Health.User_Process", "Write to registry", "NetLock RMM User Agent");
            Registry.HKLM_Write_Value(Application_Paths.hklm_run_directory_reg_path, "NetLock RMM User Agent", Application_Paths.netlock_user_process_uac_exe);
        }

        // Software Secure Attention Sequence
        public static void SaS()
        {
            // Write the NetLock RMM SAS to the registry, if it does not exist
            Logging.Debug("Initialization.Health.SAS", "Write to registry", "SoftwareSASGeneration");
            Registry.HKLM_Write_Dword_Value(Application_Paths.hklm_sas_reg_path, "SoftwareSASGeneration", 1);
        }
    }
}
