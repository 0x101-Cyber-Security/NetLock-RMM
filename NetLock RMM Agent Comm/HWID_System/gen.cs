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
                var cpuId = "esfiohfuisehf";
                var linuxId = "fizhfuiesuf";
                HW_UID = linuxId + cpuId;
            }
            else
            {
                HW_UID = "Unknown";
            }
        }
    }
}