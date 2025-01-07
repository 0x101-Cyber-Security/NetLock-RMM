using Global.Helper;
using System;
using System.Text;
using System.Security.Cryptography;
using NetLock_RMM_Agent_Comm;
using System.Runtime.InteropServices;

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
                    //string motherboard = Linux.Helper.Bash.Execute_Command("cat /sys/class/dmi/id/board_serial");
                    //string cpuInfo = Linux.Helper.Bash.Execute_Command("cat /proc/cpuinfo | grep -m 1 'model name' | awk -F: '{print $2}'").Trim();
                    //string macAddress = Linux.Helper.Bash.Execute_Command("cat /sys/class/net/$(ip route show default | awk '/default/ {print $5}')/address");
                    //string diskSerial = Linux.Helper.Bash.Execute_Command("udevadm info --query=property --name=/dev/sda | grep ID_SERIAL_SHORT | awk -F= '{print $2}'");

                    // Combining the data
                    string rawHwid = $"{Device_Worker.access_key}"; // we use the access key to generate the hwid because I yet dont know a better way to generate a hwid on linux, without causing problems in the future

                    // Generate MD5 hash
                    using (var md5 = MD5.Create())
                    {
                        byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(rawHwid));
                        HW_UID = BitConverter.ToString(hashBytes).Replace("-", "");
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("HwidGenerator", "Error generating HWID", ex.ToString());
                    HW_UID = "ND"; // ND = Not Defined
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    // Retrieving hardware data
                    //string motherboard = Linux.Helper.Bash.Execute_Command("cat /sys/class/dmi/id/board_serial");
                    //string cpuInfo = Linux.Helper.Bash.Execute_Command("cat /proc/cpuinfo | grep -m 1 'model name' | awk -F: '{print $2}'").Trim();
                    //string macAddress = Linux.Helper.Bash.Execute_Command("cat /sys/class/net/$(ip route show default | awk '/default/ {print $5}')/address");
                    //string diskSerial = Linux.Helper.Bash.Execute_Command("udevadm info --query=property --name=/dev/sda | grep ID_SERIAL_SHORT | awk -F= '{print $2}'");
                    // Combining the data
                    string rawHwid = $"{Device_Worker.access_key}"; // we use the access key to generate the hwid because I yet dont know a better way to generate a hwid on linux, without causing problems in the future
                    // Generate MD5 hash
                    using (var md5 = MD5.Create())
                    {
                        byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(rawHwid));
                        HW_UID = BitConverter.ToString(hashBytes).Replace("-", "");
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("HwidGenerator", "Error generating HWID", ex.ToString());
                    HW_UID = "ND"; // ND = Not Defined
                }
            }
            else
            {
                HW_UID = "unknown platform";
            }
        }
    }
}