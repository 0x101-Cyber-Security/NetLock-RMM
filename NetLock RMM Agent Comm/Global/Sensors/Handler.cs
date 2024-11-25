using NetLock_RMM_Agent_Comm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Global.Sensors
{
    internal class Handler
    {
        public static async void Scheduler_Tick(object source, ElapsedEventArgs e)
        {
            if (!String.IsNullOrEmpty(Device_Worker.policy_sensors_json))
                Time_Scheduler.Check_Execution();
        }
    }
}
