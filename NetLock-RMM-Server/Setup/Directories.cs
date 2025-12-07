namespace NetLock_RMM_Server.Setup
{
    public class Directories
    {
        public static void Check_Directories()
        {
            try
            {
                // Create the directories if they do not exist
                if (!Directory.Exists(Application_Paths.logs_dir))
                    Directory.CreateDirectory(Application_Paths.logs_dir);

                if (!Directory.Exists(Application_Paths.internal_dir))
                    Directory.CreateDirectory(Application_Paths.internal_dir);

                if (!Directory.Exists(Application_Paths.internal_temp_dir))
                    Directory.CreateDirectory(Application_Paths.internal_temp_dir);

                if (!Directory.Exists(Application_Paths._private_files_netlock))
                    Directory.CreateDirectory(Application_Paths._private_files_netlock);

                if (!Directory.Exists(Application_Paths._private_files_netlock_temp))
                    Directory.CreateDirectory(Application_Paths._private_files_netlock_temp);

                if (!Directory.Exists(Application_Paths.certificates_path))
                    Directory.CreateDirectory(Application_Paths.certificates_path);

                if (!Directory.Exists(Application_Paths._public_uploads_user))
                    Directory.CreateDirectory(Application_Paths._public_uploads_user);

                if (!Directory.Exists(Application_Paths._public_downloads_user))
                    Directory.CreateDirectory(Application_Paths._public_downloads_user);
            }
            catch (Exception e)
            {
                Logging.Handler.Error("Directories", "Check_Directories", $"Error checking/creating directories: {e.ToString()}");
            }
        }
        
        public static bool Delete_Directories()
        {
            try
            {
                if (Directory.Exists(Application_Paths._public_downloads_user))
                    Directory.Delete(Application_Paths._public_downloads_user, true);
            
                if (Directory.Exists(Application_Paths._public_uploads_user))
                    Directory.Delete(Application_Paths._public_uploads_user, true);
            
                // Delete every directory & files inside private_files
                if (Directory.Exists(Application_Paths._private_files))
                {
                    var dirInfo = new DirectoryInfo(Application_Paths._private_files);
                    foreach (var dir in dirInfo.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                    
                    foreach (var file in dirInfo.GetFiles())
                    {
                        file.Delete();
                    }
                }

                if (Directory.Exists(Application_Paths.internal_temp_dir))
                    Directory.Delete(Application_Paths.internal_temp_dir, true);
            
                if (Directory.Exists(Application_Paths.logs_dir))
                    Directory.Delete(Application_Paths.logs_dir, true);
            
                // Recreate directories
                Check_Directories();

                return true;
            }
            catch (Exception e)
            {
                Logging.Handler.Error("Directories", "Delete_Directories", $"Error deleting directories: {e.ToString()}");
                return false;
            }
        }
    }
}