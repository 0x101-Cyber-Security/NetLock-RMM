using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Global.Helper;

namespace Windows.Helper
{
    internal class Service
    {
        public static bool Start(string name)
        {
            try
            {
                Logging.Debug("Windows.Helper.Start", "Starting service.", name);
                Console.WriteLine("[" + DateTime.Now + "] - [Windows.Helper.Start] -> Starting service: " + name);

                ServiceController sc = new ServiceController(name);
                sc.Start();
                sc.WaitForStatus(ServiceControllerStatus.Running);

                Logging.Debug("Windows.Helper.Start", "Starting service.", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Windows.Helper.Start] -> Starting service: Done.");

                return true;
            }
            catch (Exception ex)
            {
                Logging.Error("Windows.Helper.Start", "Starting service failed: " + name, ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Windows.Helper.Start] -> Starting service failed: " + ex.Message);
                return false;
            }
        }

        public static bool Stop(string name)
        {
            try
            {
                Logging.Debug("Windows.Helper.Stop", "Stop service.", name);
                Console.WriteLine("[" + DateTime.Now + "] - [Mode: Fix] -> Stopping service: " + name);

                ServiceController sc = new ServiceController(name);
                sc.Stop();
                sc.WaitForStatus(ServiceControllerStatus.Stopped);

                Logging.Debug("Windows.Helper.Stop", "Stop service.", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Mode: Fix] -> Stop service: Done.");

                return true;
            }
            catch (Exception ex)
            {
                Logging.Error("Windows.Helper.Stop", "Stopping service failed: " + name, ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Windows.Helper.Stop] -> Stopping service failed: " + ex.Message);
                return false;
            }
        }
    }
}
