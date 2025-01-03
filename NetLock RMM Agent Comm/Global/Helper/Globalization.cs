using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Global.Helper
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

        public static DateTime GetLastBootUpTime()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsLastBootTime();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxLastBootTime();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacLastBootTime();
            }
            else
            {
                Logging.Error("Globalization.SystemUptime.GetLastBootUpTime", "Error getting last boot time", "This operating system is not supported.");
                throw new PlatformNotSupportedException("This operating system is not supported.");
            }
        }

        private static DateTime GetWindowsLastBootTime()
        {
            var bootTime = ManagementDateTimeConverter.ToDateTime(Windows.Helper.WMI.Search("root\\cimv2", "SELECT LastBootUpTime FROM Win32_OperatingSystem", "LastBootUpTime"));
            return bootTime;
        }

        private static DateTime GetLinuxLastBootTime()
        {
            try
            {
                // Read /proc/uptime to get the system uptime in seconds
                string[] uptimeData = System.IO.File.ReadAllText("/proc/uptime").Split(' ');
                double uptimeSeconds = double.Parse(uptimeData[0]);

                Console.WriteLine(DateTime.Now.AddSeconds(-uptimeSeconds));

                return DateTime.Now.AddSeconds(-uptimeSeconds);
            }
            catch (Exception ex)
            {
                Logging.Error("Globalization.SystemUptime.GetLinuxLastBootTime", "Error getting Linux last boot time", ex.ToString());
                return DateTime.MinValue;
            }
        }

        private static DateTime GetMacLastBootTime()
        {
            try
            {

                // Use `sysctl` command to get boot time
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sysctl",
                        Arguments = "kern.boottime",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string result = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // Parse the output, e.g., "kern.boottime: { sec = 1672444800, usec = 0 } Tue Jan 1 00:00:00 2023"
                string bootTimePart = result.Split(new[] { "sec = " }, StringSplitOptions.None)[1].Split(',')[0].Trim();
                long bootTimeUnix = long.Parse(bootTimePart);
                DateTime bootTime = DateTimeOffset.FromUnixTimeSeconds(bootTimeUnix).DateTime;

                return bootTime;
            }
            catch (Exception ex)
            {
                Logging.Error("Globalization.SystemUptime.GetMacLastBootTime", "Error getting Mac last boot time", ex.ToString());
                return DateTime.MinValue;
            }
        }
    }
}
