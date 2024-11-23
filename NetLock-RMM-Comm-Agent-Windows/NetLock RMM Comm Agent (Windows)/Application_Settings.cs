using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Comm_Agent_Windows
{
    internal class Application_Settings
    {
        public static string version = "2.0.0.0";
        public static string NetLock_Data_Database_String = @"Data Source=" + Application_Paths.program_data_netlock_policy_database + ";Version=3;";
        public static string NetLock_Events_Database_String = @"Data Source=" + Application_Paths.program_data_netlock_events_database + ";Version=3;";
        public static string NetLock_Local_Encryption_Key = "Xp8,=&=@7XDEUeUUbeln";
    }
}
