using NetLock_RMM_Comm_Agent_Windows.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NetLock_RMM_Comm_Agent_Windows.Sensors
{
    internal class Handler
    {
        public static async void Scheduler_Tick(object source, ElapsedEventArgs e)
        {
            if (!String.IsNullOrEmpty(Service.policy_sensors_json))
                Time_Scheduler.Check_Execution();
        }
    }
}
