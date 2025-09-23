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
    }
}
