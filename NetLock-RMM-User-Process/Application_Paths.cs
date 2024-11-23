using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_User_Process
{
    internal class Application_Paths
    {
        public static string application_data_debug_txt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NetLock RMM User Process", "debug.txt");
        public static string application_data_logs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NetLock RMM User Process", "Logs");
    }
}
