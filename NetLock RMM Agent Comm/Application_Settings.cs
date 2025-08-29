using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Agent_Comm
{
    internal class Application_Settings
    {
        public static string version = "2.5.2.7";
        public static string NetLock_Data_Database_String = @"Data Source=" + Application_Paths.program_data_netlock_policy_database + ";";
        public static string NetLock_Events_Database_String = @"Data Source=" + Application_Paths.program_data_netlock_events_database + ";";
        public static string NetLock_Local_Encryption_Key = "()TZ%/N)NZTG$/()4i59du4)";
    }
} 