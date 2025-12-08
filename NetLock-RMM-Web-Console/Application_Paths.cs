using System.Runtime.InteropServices;

namespace NetLock_RMM_Web_Console
{
    public class Application_Paths
    {
        //public static string logs_dir = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Web Console\Logs";
        public static string logs_dir = Path.Combine(GetBasePath(), "0x101 Cyber Security", "NetLock RMM", "Web Console", "Logs");

        public static string _private_files_devices = "devices";

        public static string internal_dir = Path.Combine(GetCurrentDirectory(), "internal");
        public static string internal_temp_dir = Path.Combine(GetCurrentDirectory(), "internal", "temp");

        public static string internal_recordings_dir = Path.Combine(GetCurrentDirectory(), "internal", "recordings");

        //OSSCH_START 578b56fa-a748-45bf-8e56-c25255104034 //OSSCH_END

        public static string certificates_path = Path.Combine(GetCurrentDirectory(), "certificates");

        private static string GetBasePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Application_Settings.IsLiveEnvironment)
            {
                return "/var";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !Application_Settings.IsLiveEnvironment)
            {
                return "/home/nico-mak/.local/share";
            }
            else
            {
                throw new NotSupportedException("Unsupported OS");
            }
        }

        private static string GetCurrentDirectory()
        {
            return AppContext.BaseDirectory;
        }

        //public static string debug_txt_path = @"C:\ProgramData\0x101 Cyber Security\NetLock RMM\Web Console\debug.txt";

        //URLs
        public static string redirect_path = "/redirect";
    }
}
