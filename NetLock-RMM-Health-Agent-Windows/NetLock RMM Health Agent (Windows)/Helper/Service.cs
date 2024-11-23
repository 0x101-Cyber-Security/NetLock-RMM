using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    internal class Service
    {
        public static bool Start(string name)
        {
            try
            {
                Logging.Handler.Debug("Helper.Service.Start", "Stop service.", name);
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Service.Start] -> Stopping service: " + name);

                ServiceController sc = new ServiceController(name);
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running);

                Logging.Handler.Debug("Helper.Service.Start", "Stop service.", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Service.Start] -> Stop service: Done.");

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.Service.Start", "Stopping service failed: " + name, ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Service.Start] -> Stopping service failed: " + ex.Message);
                return false;
            }
        }
    }
}
