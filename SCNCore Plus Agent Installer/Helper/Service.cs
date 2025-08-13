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
                Logging.Handler.Debug("Helper.Service.Start", "Starting service.", name);
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Service.Start] -> Starting service: " + name);

                ServiceController sc = new ServiceController(name);
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running);

                Logging.Handler.Debug("Helper.Service.Start", "Starting service.", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Service.Start] -> Starting service: Done.");

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.Service.Start", "Starting service failed: " + name, ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Service.Start] -> Starting service failed: " + ex.Message);
                return false;
            }
        }

        public static bool Stop(string name)
        {
            try
            {
                Logging.Handler.Debug("Helper.Service.Stop", "Stop service.", name);
                Console.WriteLine("[" + DateTime.Now + "] - [Mode: Fix] -> Stopping service: " + name);

                ServiceController sc = new ServiceController(name);
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);

                Logging.Handler.Debug("Helper.Service.Stop", "Stop service.", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Mode: Fix] -> Stop service: Done.");

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.Service.Stop", "Stopping service failed: " + name, ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Service.Stop] -> Stopping service failed: " + ex.Message);
                return false;
            }
        }
    }
}
