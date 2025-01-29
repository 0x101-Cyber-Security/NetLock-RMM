using NetLock_RMM_Server;
using NetLock_RMM_Server.Configuration;
using System.Net;
using System.IO.Compression;

namespace Helper
{
    public class Package_Provider
    {
        public static async Task Check_Packages()
        {
            try
            {
                if (Roles.Update || Roles.Trust)
                {
                    string version = String.Empty;
                    string old_package_url = String.Empty;
                    string package_url = await NetLock_RMM_Server.MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "package_provider_url");

                    // Check if version.txt exists
                    if (File.Exists(Path.Combine(Application_Paths._private_files_netlock, "version.txt")))
                        version = File.ReadAllText(Path.Combine(Application_Paths._private_files_netlock, "version.txt"));

                    // Check if package_url.txt exists
                    if (File.Exists(Path.Combine(Application_Paths._private_files_netlock, "package_url.txt")))
                        old_package_url = File.ReadAllText(Path.Combine(Application_Paths._private_files_netlock, "package_url.txt"));

                    if (version != Application_Settings.version || String.IsNullOrEmpty(version) || String.IsNullOrEmpty(old_package_url) || package_url != old_package_url)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Package is not setup. Version or package url is different. Attempting to download from package provider...");

                        if (Directory.Exists(Application_Paths._private_files_netlock))
                        {
                            // Delete files & folders in netlock files
                            foreach (string dir in Directory.GetDirectories(Application_Paths._private_files_netlock))
                                Directory.Delete(dir, true);

                            foreach (string file in Directory.GetFiles(Application_Paths._private_files_netlock))
                                File.Delete(file);

                            // Delete files in temp folder
                            if (Directory.Exists(Application_Paths._private_files_netlock_temp))
                            {
                                foreach (string file in Directory.GetFiles(Application_Paths._private_files_netlock_temp))
                                    File.Delete(file);
                            }
                        }

                        Console.WriteLine("Cleaned previous package.");

                        // Create temp folder
                        if (!Directory.Exists(Application_Paths._private_files_netlock_temp))
                            Directory.CreateDirectory(Application_Paths._private_files_netlock_temp);

                        string package_download_location = Path.Combine(Application_Paths._private_files_netlock_temp, "package.zip");
                        
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("Downloading new package. This might take a while...");

                        // Download the new version
                        await Http.Download_File(package_url, package_download_location);

                        Console.WriteLine("Package downloaded. Extracting...");

                        // Unzip the new version
                        ZipFile.ExtractToDirectory(package_download_location, Application_Paths._private_files_netlock);

                        // Write new package_url to package_url.txt
                        File.WriteAllText(Path.Combine(Application_Paths._private_files_netlock, "package_url.txt"), package_url);

                        Console.WriteLine("Registering packages...");

                        // Register all files
                        foreach (string file in Directory.GetFiles(Application_Paths._private_files_netlock, "*", SearchOption.AllDirectories))
                            await NetLock_RMM_Server.Files.Handler.Register_File(file, String.Empty, String.Empty, String.Empty);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Packages successfully setup & ready! :)");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Packages are ready.");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.Package_Provider.Check_Packages", "Result", ex.ToString());

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Packages could not be setup. Please make sure you provided a package provider url in the webconsole and that it can be accessed from the backend. Otherwise you cannot install or update agents with this backend.");
                Console.WriteLine("Error: " + ex.Message);
                Console.ResetColor();
            }
        }
    }
}
