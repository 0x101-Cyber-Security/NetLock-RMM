using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Primitives;
using System.Runtime.InteropServices;

namespace NetLock_RMM_Server
{
    public class Application_Paths
    {
        public static string logs_dir = Path.Combine(GetBasePath(), "0x101 Cyber Security", "NetLock RMM", "Server", "Logs");
        
        public static string log_debug_path = Path.Combine(logs_dir, "debug.log");
        public static string log_info_path = Path.Combine(logs_dir, "info.log");
        public static string log_warning_path = Path.Combine(logs_dir, "warning.log");
        public static string log_error_path = Path.Combine(logs_dir, "error.log");

        public static string _public_uploads_user = Path.Combine(GetCurrentDirectory(), "www", "public", "uploads", "user");
        public static string _public_downloads_user = Path.Combine(GetCurrentDirectory(), "www", "public", "downloads", "user");
        
        public static string _private_files = Path.Combine(GetCurrentDirectory(), "www", "private", "files");
        public static string _private_files_admin_db_friendly = Path.Combine(GetCurrentDirectory(), "www", "private", "files");

        public static string _private_files_netlock = Path.Combine(GetCurrentDirectory(), "www", "private", "files", "netlock");
        public static string _private_files_netlock_temp = Path.Combine(GetCurrentDirectory(), "www", "private", "files", "netlock", "temp");        

        public static string internal_dir = Path.Combine(GetCurrentDirectory(), "internal");
        public static string internal_temp_dir = Path.Combine(GetCurrentDirectory(), "internal", "temp");

        //OSSCH_START ffac9a1d-4ff0-489f-b112-a6e502a967db //OSSCH_END

        // Lets Encrypt path
        public static string certificates_path = Path.Combine(GetCurrentDirectory(), "certificates");

        // LLM
        public static string llm_model_path = Path.Combine(GetCurrentDirectory(), "llm", "model.gguf");

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
    }
}