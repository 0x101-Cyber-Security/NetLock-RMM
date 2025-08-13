using Global.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MacOS.Helper
{
    internal class MacOS
    {
        public static double Disks_Convert_Size_To_GB(string size)
        {
            try
            {
                double sizeInGB = 0;

                // Check for size specifications in GB
                if (size.EndsWith("G", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("G", "").Trim());
                }
                // Check for size specifications in MB (conversion to GB)
                else if (size.EndsWith("M", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("M", "").Trim()) / 1024; // Umrechnung von MB in GB
                }
                // Check for size specifications in TB (conversion to GB)
                else if (size.EndsWith("T", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("T", "").Trim()) * 1024; // Umrechnung von TB in GB
                }
                // Check for size specifications in bytes (conversion to GB)
                else if (size.EndsWith("B", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("B", "").Trim()) / (1024 * 1024 * 1024); // Umrechnung von Bytes in GB
                }
                // Case without unit, default assumption: size in GB
                else if (double.TryParse(size, out double sizeValue))
                {
                    sizeInGB = sizeValue;
                }
                else
                {
                    throw new ArgumentException("Invalid size format.");
                }

                // Rounding to 2 decimal places for a better display
                return Math.Round(sizeInGB, 2);
            }
            catch (Exception ex)
            { 
                Logging.Error("MacOS.Helper.MacOS.Disks_Convert_Size_To_GB", "Error converting size to GB", ex.ToString());
                return 0;
            }
        }

        public static double Disks_Convert_Size_To_GB_Two(long sizeInBytes)
        {
            try
            {
                // Conversion from bytes to GB
                double sizeInGB = (double)sizeInBytes / (1024 * 1024 * 1024);

                // Rounding to 2 decimal places for a better display
                return Math.Round(sizeInGB, 2);
            }
            catch (Exception ex)
            {
                Logging.Error("MacOS.Helper.MacOS.Disks_Convert_Size_To_GB_Two", "Error converting size to GB", ex.ToString());
                return 0;
            }
        }

        public static bool Firewall_Status()
        {
            // Firewall states:
            // 0 = Off
            // 1 = On for specific services
            // 2 = On for essential services

            try
            {
                // Check if the firewall is enabled
                string firewallStatus = Zsh.Execute_Script("Firewall_Status", false, "defaults read /Library/Preferences/com.apple.alf globalstate");

                if (firewallStatus.Contains("1"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("MacOS.Helper.MacOS.Firewall_Status", "Error checking firewall status", ex.ToString());
                return false;
            }
        }
    }
}
