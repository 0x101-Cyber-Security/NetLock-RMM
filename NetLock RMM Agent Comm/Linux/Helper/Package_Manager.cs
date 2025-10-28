using System.Text.Json;
using Global.Helper;
using Global.Online_Mode;

namespace Linux.Helper;

public class Package_Manager
{
        public static void ParseAptPackages(string installedPackages, List<string> applications_installedJsonList, List<Handler.Applications_Installed> currentApplications)
        {
            try
            {
                // Split the output into lines
                var lines = installedPackages.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Iterate over each line (excluding the header)
                foreach (var line in lines.Skip(1))
                {
                    var details = line.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (details.Length >= 2)
                    {
                        string packageName = details[0].Trim();
                        string packageVersion = details[1].Trim();
                        string packageStatus = details.Length > 2 ? details[2].Trim() : "N/A";

                        // Here, you would collect the necessary package information into a JSON format
                        var applicationInfo = new Handler.Applications_Installed
                        {
                            name = packageName,
                            version = packageVersion,
                            installed_date = "N/A",  // apt doesn't provide install date directly, consider using additional logic if needed
                            installation_path = "N/A",  // apt doesn't provide path; you might use 'dpkg-query' or similar for that info
                            vendor = "N/A",  // Vendor info isn't available directly with 'apt list --installed'
                            uninstallation_string = $"sudo apt remove {packageName}"  // Uninstall command
                        };

                        string applications_installedJson = JsonSerializer.Serialize(applicationInfo, new JsonSerializerOptions { WriteIndented = true });

                        lock (applications_installedJsonList)
                        {
                            applications_installedJsonList.Add(applications_installedJson);
                            currentApplications.Add(applicationInfo); // Add object to comparison list
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Error("Device_Information.Software.ParseAptPackages", "Error parsing apt packages", e.ToString());
                Console.WriteLine(e);
            }
        }

        public static void ParseYumPackages(string installedPackages, List<string> applications_installedJsonList, List<Handler.Applications_Installed> currentApplications, string packageManager)
        {
            try
            {
                // Split the output into lines
                var lines = installedPackages.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Iterate over each line (excluding the header)
                foreach (var line in lines.Skip(1))
                {
                    var details = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (details.Length >= 2)
                    {
                        string packageName = details[0].Trim();
                        string packageVersion = details[1].Trim();
                        string packageStatus = details.Length > 2 ? details[2].Trim() : "N/A";

                        // Here, you would collect the necessary package information into a JSON format
                        var applicationInfo = new Handler.Applications_Installed
                        {
                            name = packageName,
                            version = packageVersion,
                            installed_date = "N/A",  // yum/dnf doesn't provide install date directly
                            installation_path = "N/A",  // yum/dnf doesn't provide path; consider using 'rpm -ql packageName' for that info
                            vendor = "N/A",  // Vendor info isn't available directly with 'yum list installed'
                            uninstallation_string = $"sudo {packageManager} remove {packageName}"  // Uninstall command
                        };

                        string applications_installedJson = JsonSerializer.Serialize(applicationInfo, new JsonSerializerOptions { WriteIndented = true });

                        lock (applications_installedJsonList)
                        {
                            applications_installedJsonList.Add(applications_installedJson);
                            currentApplications.Add(applicationInfo); // Add object to comparison list
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Error("Device_Information.Software.ParseYumPackages", "Error parsing yum/dnf packages", e.ToString());
                Console.WriteLine(e);
            }
        }

        public static void ParseZypperPackages(string installedPackages, List<string> applications_installedJsonList, List<Handler.Applications_Installed> currentApplications)
        {
            try
            {
                // Split the output into lines
                var lines = installedPackages.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Iterate over each line (excluding the header)
                foreach (var line in lines.Skip(2)) // Skip the first two lines (header)
                {
                    var details = line.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (details.Length >= 3)
                    {
                        string packageName = details[0].Trim();
                        string packageVersion = details[1].Trim();
                        string packageStatus = details[2].Trim();

                        // Here, you would collect the necessary package information into a JSON format
                        var applicationInfo = new Handler.Applications_Installed
                        {
                            name = packageName,
                            version = packageVersion,
                            installed_date = "N/A",  // zypper doesn't provide install date directly
                            installation_path = "N/A",  // zypper doesn't provide path; consider using 'rpm -ql packageName' for that info
                            vendor = "N/A",  // Vendor info isn't available directly with 'zypper se --installed-only'
                            uninstallation_string = $"sudo zypper remove {packageName}"  // Uninstall command
                        };

                        string applications_installedJson = JsonSerializer.Serialize(applicationInfo, new JsonSerializerOptions { WriteIndented = true });

                        lock (applications_installedJsonList)
                        {
                            applications_installedJsonList.Add(applications_installedJson);
                            currentApplications.Add(applicationInfo); // Add object to comparison list
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Error("Device_Information.Software.ParseZypperPackages", "Error parsing zypper packages", e.ToString());
                Console.WriteLine(e);
            }
        }

        public static void ParsePacmanPackages(string installedPackages, List<string> applications_installedJsonList, List<Handler.Applications_Installed> currentApplications)
        {
            try
            {
                // Split the output into lines
                var lines = installedPackages.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Iterate over each line
                foreach (var line in lines)
                {
                    var details = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (details.Length >= 2)
                    {
                        string packageName = details[0].Trim();
                        string packageVersion = details[1].Trim();

                        // Here, you would collect the necessary package information into a JSON format
                        var applicationInfo = new Handler.Applications_Installed
                        {
                            name = packageName,
                            version = packageVersion,
                            installed_date = "N/A",  // pacman doesn't provide install date directly
                            installation_path = "N/A",  // pacman doesn't provide path; consider using 'pacman -Ql packageName' for that info
                            vendor = "N/A",  // Vendor info isn't available directly with 'pacman -Q'
                            uninstallation_string = $"sudo pacman -R {packageName}"  // Uninstall command
                        };

                        string applications_installedJson = JsonSerializer.Serialize(applicationInfo, new JsonSerializerOptions { WriteIndented = true });

                        lock (applications_installedJsonList)
                        {
                            applications_installedJsonList.Add(applications_installedJson);
                            currentApplications.Add(applicationInfo); // Add object to comparison list
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Error("Device_Information.Software.ParsePacmanPackages", "Error parsing pacman packages", e.ToString());
                Console.WriteLine(e);
            }
        }

        public static string DetectPackageManager()
        {
            try
            {
                // Check for the presence of common package manager commands
                if (Bash.Execute_Script("Detect_Package_Manager", false, "command -v apt") != "")
                    return "apt";
                if (Bash.Execute_Script("Detect_Package_Manager", false, "command -v yum") != "")
                    return "yum";
                if (Bash.Execute_Script("Detect_Package_Manager", false, "command -v dnf") != "")
                    return "dnf";
                if (Bash.Execute_Script("Detect_Package_Manager", false, "command -v zypper") != "")
                    return "zypper";
                if (Bash.Execute_Script("Detect_Package_Manager", false, "command -v pacman") != "")
                    return "pacman";

                return null;
            }
            catch (Exception e)
            {
                Logging.Error("Device_Information.Software.DetectPackageManager", "Error detecting package manager", e.ToString());
                Console.WriteLine(e);
                return null;
            }
        }
}