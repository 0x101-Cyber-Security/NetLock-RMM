using Global.Helper;
using System;
using System.Text;
using System.Security.Cryptography;

namespace _x101.HWID_System
{
    public static class ENGINE
    {
        [System.Reflection.ObfuscationAttribute(Feature = "Virtualization", Exclude = false)]

        public static string HW_UID { get; private set; }

        static ENGINE()
        {
            if (OperatingSystem.IsWindows())
            {
                var cpuId = CpuId.GetCpuId();
                var windowsId = WindowsId.GetWindowsId();
                HW_UID = windowsId + cpuId;
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    // Retrieving hardware data
                    string motherboard = Linux.Helper.Bash.Execute_Command("cat /sys/class/dmi/id/board_serial");
                    string cpuInfo = Linux.Helper.Bash.Execute_Command("cat /proc/cpuinfo | grep -m 1 'model name' | awk -F: '{print $2}'").Trim();
                    string macAddress = Linux.Helper.Bash.Execute_Command("cat /sys/class/net/$(ip route show default | awk '/default/ {print $5}')/address");
                    //string diskSerial = Linux.Helper.Bash.Execute_Command("udevadm info --query=property --name=/dev/sda | grep ID_SERIAL_SHORT | awk -F= '{print $2}'");

                    // Combining the data
                    string rawHwid = $"{motherboard}-{cpuInfo}-{macAddress}";

                    // Generate hash
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawHwid));
                        HW_UID = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                    }

                    HW_UID = "blaaaaaaaaa";
                }
                catch (Exception ex)
                {
                    Logging.Error("HwidGenerator", "Error generating HWID", ex.ToString());
                    HW_UID = "ND";
                }
            }
            else
            {
                HW_UID = "unknown platform";
            }
        }
    }
}