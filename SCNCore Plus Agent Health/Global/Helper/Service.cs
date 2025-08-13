using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Global.Helper
{
    internal class Service
    {
        public static async Task<string> Action(string action, string name)
        {
            bool service_start_failed = false;
            string service_error_message = string.Empty;
            string service_status = string.Empty;

            try
            {
                ServiceController sc = new ServiceController(name);
                service_status = sc.Status.Equals(ServiceControllerStatus.Paused).ToString();

                if (action == "start" && sc.Status.Equals(ServiceControllerStatus.Running))
                {
                    return "Service is already running.";
                }
                else if (action == "start" && sc.Status.Equals(ServiceControllerStatus.Stopped))
                {
                    sc.Start();
                    return "Service was stopped and now started.";
                }
                else if (action == "stop" && sc.Status.Equals(ServiceControllerStatus.Stopped))
                {
                    return "Service is already stopped.";
                }
                else if (action == "stop" && sc.Status.Equals(ServiceControllerStatus.Running))
                {
                    sc.Stop();
                    return "Service was running and now stopped.";
                }
                else if (action == "restart" && sc.Status.Equals(ServiceControllerStatus.Running))
                {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                    sc.Start();
                    return "Service was already running and now restarted.";
                }
                else if (action == "restart" && sc.Status.Equals(ServiceControllerStatus.Stopped))
                {
                    sc.Start();
                    return "Service was stopped and now restarted.";
                }
                else if (action == "status")
                {
                    return "Service is " + sc.Status.ToString() + ".";
                }
                else
                {
                    return "Service is in unknown state.";
                }
            }
            catch (Exception ex)
            {
                Logging.Sensors("Global.Helper.Service", "Checking service state, or performing action failed", ex.ToString());

                service_error_message = ex.Message;
                return service_error_message;
            }
        }
    }
}
