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
            double sizeInGB = 0;

            // Überprüfen auf Größenangaben in GB
            if (size.EndsWith("G", StringComparison.OrdinalIgnoreCase))
            {
                sizeInGB = double.Parse(size.Replace("G", "").Trim());
            }
            // Überprüfen auf Größenangaben in MB (Umwandlung in GB)
            else if (size.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                sizeInGB = double.Parse(size.Replace("M", "").Trim()) / 1024; // Umrechnung von MB in GB
            }
            // Überprüfen auf Größenangaben in TB (Umwandlung in GB)
            else if (size.EndsWith("T", StringComparison.OrdinalIgnoreCase))
            {
                sizeInGB = double.Parse(size.Replace("T", "").Trim()) * 1024; // Umrechnung von TB in GB
            }
            // Überprüfen auf Größenangaben in Bytes (Umwandlung in GB)
            else if (size.EndsWith("B", StringComparison.OrdinalIgnoreCase))
            {
                sizeInGB = double.Parse(size.Replace("B", "").Trim()) / (1024 * 1024 * 1024); // Umrechnung von Bytes in GB
            }
            // Fall ohne Einheit, Standardannahme: Größe in GB
            else if (double.TryParse(size, out double sizeValue))
            {
                sizeInGB = sizeValue;
            }
            else
            {
                throw new ArgumentException("Ungültiges Größenformat.");
            }

            // Rundung auf 2 Dezimalstellen für eine bessere Anzeige
            return Math.Round(sizeInGB, 2);
        }
    }
}
