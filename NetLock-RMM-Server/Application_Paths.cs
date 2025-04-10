using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Primitives;
using System.Runtime.InteropServices;

namespace NetLock_RMM_Server
{
    public class Application_Paths
    {
        public static string logs_dir = Path.Combine(GetBasePath(), "0x101 Cyber Security", "NetLock RMM", "Server", "Logs");
        public static string debug_txt_path = Path.Combine(GetBasePath(), "0x101 Cyber Security", "NetLock RMM", "Server", "debug.txt");

        public static string _public_uploads_user = Path.Combine(GetCurrentDirectory(), "www", "public", "uploads", "user");
        public static string _public_downloads_user = Path.Combine(GetCurrentDirectory(), "www", "public", "downloads", "user");
        
        public static string _private_uploads_remote_temp = Path.Combine(GetCurrentDirectory(), "www", "private", "uploads", "remote", "temp");
        public static string _private_downloads_remote_temp = Path.Combine(GetCurrentDirectory(), "www", "private", "downloads", "remote", "temp");

        public static string _private_files = Path.Combine(GetCurrentDirectory(), "www", "private", "files");
        //public static string _private_files_admin = Path.Combine(GetCurrentDirectory(), "www", "private", "files", "admin");
        public static string _private_files_admin_db_friendly = Path.Combine(GetCurrentDirectory(), "www", "private", "files");

        public static string llm_model_path = Path.Combine(GetCurrentDirectory(), "llm", "model.gguf");

        public static string _private_files_netlock = Path.Combine(GetCurrentDirectory(), "www", "private", "files", "netlock");
        public static string _private_files_netlock_temp = Path.Combine(GetCurrentDirectory(), "www", "private", "files", "netlock", "temp");        

        public static string internal_dir = Path.Combine(GetCurrentDirectory(), "internal");
        public static string internal_temp_dir = Path.Combine(GetCurrentDirectory(), "internal", "temp");
        //OSSCH_START d4149c13-35cf-4555-8f75-f4bfd348ad6e //OSSCH_END

        //OSSCH_START a260ffed-bbd9-43fe-b791-d13b8a22197f //OSSCH_END

        //OSSCH_START 265e4c5f-5430-4d65-a1e1-8e2682be5390 //OSSCH_END

        public static string internal_packages_netlock_core_dir = Path.Combine(GetCurrentDirectory(), "internal", "packages", "netlock_core");
        public static string internal_packages_netlock_core_metadata_json_path = Path.Combine(GetCurrentDirectory(), "internal", "packages", "netlock_core", "metadata.json");


        public static string lettuceencrypt_persistent_data_dir = Path.Combine(GetCurrentDirectory(), "letsencrypt");

        // URLs
        public static string redirect_path = "/";

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
    }
}