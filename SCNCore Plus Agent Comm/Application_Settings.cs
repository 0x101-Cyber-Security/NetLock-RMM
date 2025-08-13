using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCNCore_Plus_Agent_Comm
{
    internal class Application_Settings
    {
        public static string version = "2.5.2.2cs";
        public static string SCNCore_Data_Database_String = @"Data Source=" + Application_Paths.program_data_scncore_policy_database + ";";
        public static string SCNCore_Events_Database_String = @"Data Source=" + Application_Paths.program_data_scncore_events_database + ";";
        public static string SCNCore_Local_Encryption_Key = "()TZ%/N)NZTG$/()4i59du4)";
    }
}