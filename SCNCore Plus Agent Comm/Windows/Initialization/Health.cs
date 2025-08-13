using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Global.Helper;
using Windows.Helper;
using SCNCore_Plus_Agent_Comm;

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
                // Check if scncore key exists
                if (!Registry.HKLM_Key_Exists(Application_Paths.scncore_reg_path))
                    Registry.HKLM_Create_Key(Application_Paths.scncore_reg_path);

                // Check if msdav key exists
                if (!Registry.HKLM_Key_Exists(Application_Paths.scncore_microsoft_defender_antivirus_reg_path))
                    Registry.HKLM_Create_Key(Application_Paths.scncore_microsoft_defender_antivirus_reg_path);
            }
            catch (Exception ex)
            {
                Logging.Error("Initialization.Health.Check_Registry", "", ex.ToString());
            }
        }

        // Check if the firewall rules are in place
        public static void Check_Firewall()
        {
            Microsoft_Defender_Firewall.Handler.SCNCore_Plus_Comm_Agent_Rule_Inbound();
            Microsoft_Defender_Firewall.Handler.SCNCore_Plus_Comm_Agent_Rule_Outbound();
            Microsoft_Defender_Firewall.Handler.SCNCore_Plus_Health_Service_Rule();
            Microsoft_Defender_Firewall.Handler.SCNCore_Installer_Rule();
            Microsoft_Defender_Firewall.Handler.SCNCore_Uninstaller_Rule();
        }

        public static void Clean_Service_Restart()
        {
            Logging.Debug("Initialization.Health.Clean_Service_Restart", "Starting.", "");

            Process cmd_process = new Process();
            cmd_process.StartInfo.UseShellExecute = true;
            cmd_process.StartInfo.CreateNoWindow = true;
            cmd_process.StartInfo.FileName = "cmd.exe";
            cmd_process.StartInfo.Arguments = "/c powershell" + " Stop-Service 'SCNCore_Plus_Comm_Agent_Windows'; Remove-Item 'C:\\ProgramData\\0x101 Cyber Security\\SCNCore Plus\\Comm Agent\\policy.nlock'; Remove-Item 'C:\\ProgramData\\0x101 Cyber Security\\SCNCore Plus\\Comm Agent\\events.nlock'; Start-Service 'SCNCore_Plus_Comm_Agent_Windows'";
            cmd_process.Start();
            cmd_process.WaitForExit();

            Logging.Error("Initialization.Health.Clean_Service_Restart", "Stopping.", "");
        }

        public static void User_Process()
        {
            // Delete old SCNCore Plus User Agent from the registry, if it exists
            Registry.HKLM_Delete_Value(Application_Paths.hklm_run_directory_reg_path, "SCNCore Plus User Process");

            // Write the SCNCore Plus User Process to the registry, if it does not exist
            Logging.Debug("Initialization.Health.User_Process", "Write to registry", "SCNCore Plus User Agent");
            Registry.HKLM_Write_Value(Application_Paths.hklm_run_directory_reg_path, "SCNCore Plus User Agent", Application_Paths.scncore_user_process_uac_exe);
        }

        // Software Secure Attention Sequence
        public static void SaS()
        {
            // Write the SCNCore Plus SAS to the registry, if it does not exist
            Logging.Debug("Initialization.Health.SAS", "Write to registry", "SoftwareSASGeneration");
            Registry.HKLM_Write_Dword_Value(Application_Paths.hklm_sas_reg_path, "SoftwareSASGeneration", 1);
        }
    }
}
