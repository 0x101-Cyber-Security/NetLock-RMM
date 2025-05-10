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

        //OSSCH_START
        public static string internal_license_info_json_path = Path.Combine(GetCurrentDirectory(), "internal", "license_info.json");
        //OSSCH_END

        public static string lettuceencrypt_persistent_data_dir = Path.Combine(GetCurrentDirectory(), "letsencrypt");

        private static string GetBasePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "/var";
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
