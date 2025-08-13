namespace SCNCore_Plus_Server.Setup
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

            if (!Directory.Exists(Application_Paths._private_files_scncore))
                Directory.CreateDirectory(Application_Paths._private_files_scncore);

            if (!Directory.Exists(Application_Paths._private_files_scncore_temp))
                Directory.CreateDirectory(Application_Paths._private_files_scncore_temp);

            if (!Directory.Exists(Application_Paths.lettuceencrypt_persistent_data_dir))
                Directory.CreateDirectory(Application_Paths.lettuceencrypt_persistent_data_dir);

            if (!Directory.Exists(Application_Paths._public_uploads_user))
                Directory.CreateDirectory(Application_Paths._public_uploads_user);

            if (!Directory.Exists(Application_Paths._public_downloads_user))
                Directory.CreateDirectory(Application_Paths._public_downloads_user);
        }
    }
}