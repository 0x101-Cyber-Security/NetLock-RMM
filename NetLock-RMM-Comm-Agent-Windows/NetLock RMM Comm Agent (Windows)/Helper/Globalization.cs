using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Comm_Agent_Windows.Helper
{
    internal class Globalization
    {
        public static string Local_Time_Zone()
        {
            // Retrieve the time zone of the local system
            TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

            // Return the display name of the time zone
            return localTimeZone.DisplayName;
        }
    }
}
