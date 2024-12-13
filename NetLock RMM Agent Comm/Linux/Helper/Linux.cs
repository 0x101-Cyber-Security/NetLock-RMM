using Global.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linux.Helper
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

        public static string Get_Linux_Distribution()
        {
            try
            {
                // Read /etc/os-release file
                var osReleasePath = "/etc/os-release";
                if (File.Exists(osReleasePath))
                {
                    var lines = File.ReadAllLines(osReleasePath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("ID="))
                        {
                            // Extract the distribution ID
                            var distro = line.Substring(3).Trim('"');

                            Logging.Debug("Linux.Helper.Linux.Get_Linux_Distribution", "Distro", distro);

                            return distro;
                        }
                    }
                }

                Logging.Debug("Linux.Helper.Linux.Get_Linux_Distribution", "Distro", "Unknown");

                return "Could not determine Linux distribution.";
            }
            catch (Exception ex)
            {
                Logging.Error("Linux.Helper.Linux.Get_Linux_Distribution", "", ex.ToString());
                return $"Error reading /etc/os-release: {ex.Message}";
            }
        }

        public static double Disks_Convert_Size_To_GB(string size)
        {
            double sizeInGB = 0;

            // Check for size in GB
            if (size.EndsWith("G", StringComparison.OrdinalIgnoreCase))
            {
                sizeInGB = double.Parse(size.Replace("G", "").Trim());
            }
            // Check for size in MB (convert to GB)
            else if (size.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                sizeInGB = double.Parse(size.Replace("M", "").Trim()) / 1024; // Convert MB to GB
            }
            // Check for size in TB (convert to GB)
            else if (size.EndsWith("T", StringComparison.OrdinalIgnoreCase))
            {
                sizeInGB = double.Parse(size.Replace("T", "").Trim()) * 1024; // Convert TB to GB
            }
            // Handle case without unit (assuming size is in GB)
            else if (double.TryParse(size, out double sizeValue))
            {
                sizeInGB = sizeValue;
            }
            else
            {
                throw new ArgumentException("Invalid size format.");
            }

            // Round to 2 decimal places for better display
            return Math.Round(sizeInGB, 2);
        }

    }
}
