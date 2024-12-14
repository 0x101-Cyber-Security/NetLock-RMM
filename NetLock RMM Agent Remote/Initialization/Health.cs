using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using NetLock_RMM_Agent_Remote;
using Global.Helper;

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

                // Scripts
                if (!Directory.Exists(Application_Paths.program_data_scripts))
                    Directory.CreateDirectory(Application_Paths.program_data_scripts);
            }
            catch (Exception ex)
            {
                Logging.Error("Initialization.Health.Check_Directories", "", ex.ToString());
            }
        }

        // Check if the registry keys are in place
        /*public static void Check_Registry()
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
        }*/

        // Check if the firewall rules are in place
        public static void Check_Firewall()
        {
            /*Microsoft_Defender_Firewall.Handler.NetLock_RMM_Comm_Agent_Rule_Inbound();
            Microsoft_Defender_Firewall.Handler.NetLock_RMM_Comm_Agent_Rule_Outbound();
            Microsoft_Defender_Firewall.Handler.NetLock_RMM_Health_Service_Rule();
            Microsoft_Defender_Firewall.Handler.NetLock_Installer_Rule();
            Microsoft_Defender_Firewall.Handler.NetLock_Uninstaller_Rule();
            */
        }
    }
}
