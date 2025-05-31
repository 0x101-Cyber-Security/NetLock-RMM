
namespace NetLock_RMM_Web_Console.Classes.Setup
{
    public class Directories
    {
        public static void Check_Directories()
        {
            // Create the directories if they do not exist
            if (!Directory.Exists(Application_Paths.logs_dir))
                Directory.CreateDirectory(Application_Paths.logs_dir);

            if (!Directory.Exists(Application_Paths.internal_dir))
                Directory.CreateDirectory(Application_Paths.internal_dir);

            if (!Directory.Exists(Application_Paths.internal_temp_dir))
                Directory.CreateDirectory(Application_Paths.internal_temp_dir);

            if(!Directory.Exists(Application_Paths._private_files_devices))
                Directory.CreateDirectory(Application_Paths._private_files_devices);

            if (!Directory.Exists(Application_Paths.lettuceencrypt_persistent_data_dir))
                Directory.CreateDirectory(Application_Paths.lettuceencrypt_persistent_data_dir);

            if (!Directory.Exists(Application_Paths.internal_recordings_dir))
                Directory.CreateDirectory(Application_Paths.internal_recordings_dir);
        }
    }
}
