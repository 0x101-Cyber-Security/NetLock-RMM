using System;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace _x101.HWID_System
{
    internal class WindowsId
    {
        [System.Reflection.ObfuscationAttribute(Feature = "Virtualization", Exclude = false)]

        public static string GetWindowsId()
        {
            try
            {
                string cpuId = GetHardwareId("Win32_Processor", "ProcessorId");
                string biosSerial = GetHardwareId("Win32_BIOS", "SerialNumber");
                string motherboardSerial = GetHardwareId("Win32_BaseBoard", "SerialNumber");
                string diskSerial = GetHardwareId("Win32_DiskDrive", "SerialNumber");

                // Combine identifiers
                string combinedId = $"{cpuId}{biosSerial}{motherboardSerial}{diskSerial}";

                // Generate MD5 hash
                using (var md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(combinedId));
                    return BitConverter.ToString(hashBytes).Replace("-", "");
                }
            }
            catch
            {
                return "Unknown";
            }
        }

        private static string GetHardwareId(string wmiClass, string wmiProperty)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT {wmiProperty} FROM {wmiClass}"))
                {
                    return searcher.Get()
                                   .Cast<ManagementObject>()
                                   .Select(mo => mo[wmiProperty]?.ToString())
                                   .FirstOrDefault() ?? "";
                }
            }
            catch
            {
                return "";
            }
        }
    }
}