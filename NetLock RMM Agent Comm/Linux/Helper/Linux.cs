using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Agent_Comm.Linux.Helper
{
    internal class Linux
    {
        public static string Get_Last_Boot_Time()
        {
            try
            {
                // Read uptime from /proc/uptime
                string uptimeContent = File.ReadAllText("/proc/uptime");
                string[] parts = uptimeContent.Split(' ');
                double uptimeSeconds = double.Parse(parts[0]);

                // Calculate the last boot time
                DateTime lastBootTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(uptimeSeconds));

                // Format the last boot time
                string formattedBootTime = lastBootTime.ToString("dd.MM.yyyy HH:mm:ss");

                // Log and return the result
                return formattedBootTime;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving last boot time: {ex.ToString()}");
                return null;
            }
        }


    }
}
