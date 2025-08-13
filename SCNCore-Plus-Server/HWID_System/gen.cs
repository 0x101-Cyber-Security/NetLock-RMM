using System;
using System.Text;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace _x101.HWID_System
{
    public static class ENGINE
    {
        [System.Reflection.ObfuscationAttribute(Feature = "Virtualization", Exclude = false)]

        public static async Task<string> Get_Hwid()
        {
            if (OperatingSystem.IsWindows())
            {
                var cpuId = CpuId.GetCpuId();
                var windowsId = WindowsId.GetWindowsId();
                return windowsId + cpuId;
            }
            else
            {
                return "Unknown";
            }
        }
    }
}