using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using Global.Helper;
using Global.Configuration;
using NetLock_RMM_Agent_Health;

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
                if (!Directory.Exists(Application_Paths.program_data_logs))
                    Directory.CreateDirectory(Application_Paths.program_data_logs);
            }
            catch (Exception ex)
            {
                Logging.Error("Initialization.Health.Check_Directories", "", ex.ToString());
            }
        }
    }
}
