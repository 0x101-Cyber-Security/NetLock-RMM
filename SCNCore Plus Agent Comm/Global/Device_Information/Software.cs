using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Global.Online_Mode.Handler;
using Microsoft.Win32.TaskScheduler;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Global.Helper;
using System.Text.Json;
using System.Xml;

namespace Global.Device_Information
{
    internal class Software
    {
        public static string Applications_Installed()
        {
            string applications_installed_json = String.Empty;

            if (OperatingSystem.IsWindows())
            {
                // Create a list of JSON strings for each installed software
                List<string> applications_installedJsonList = new List<string>();

                try
                {
                    //Collect all 32bit installed programs
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                    if (key != null)
                    {
                        Parallel.ForEach(key.GetSubKeyNames(), subKeyName =>
                        {
                            RegistryKey subKey = key.OpenSubKey(subKeyName);
                            if (subKey != null)
                            {
                                bool empty = true;

                                string DisplayName_32bit = null;
                                string DisplayVersion_32bit = null;
                                string InstallDate_32bit = null;
                                string InstallLocation_32bit = null;
                                string Publisher_32bit = null;
                                string UninstallString_32bit = null;

                                try
                                {
                                    DisplayName_32bit = subKey.GetValue("DisplayName").ToString();
                                    if (!string.IsNullOrEmpty(DisplayName_32bit))
                                        empty = false;
                                }
                                catch { }

                                try
                                {
                                    DisplayVersion_32bit = subKey.GetValue("DisplayVersion_32bit").ToString();
                                    if (!string.IsNullOrEmpty(DisplayVersion_32bit))
                                        empty = false;
                                }
                                catch { }

                                try
                                {
                                    InstallDate_32bit = subKey.GetValue("InstallDate").ToString();
                                    if (!string.IsNullOrEmpty(InstallDate_32bit))
                                        empty = false;
                                }
                                catch { }

                                try
                                {
                                    InstallLocation_32bit = subKey.GetValue("InstallLocation").ToString();
                                    if (!string.IsNullOrEmpty(InstallLocation_32bit))
                                        empty = false;
                                }
                                catch { }

                                try
                                {
                                    Publisher_32bit = subKey.GetValue("Publisher").ToString();
                                    if (!string.IsNullOrEmpty(Publisher_32bit))
                                        empty = false;
                                }
                                catch { }

                                try
                                {
                                    UninstallString_32bit = subKey.GetValue("UninstallString").ToString();
                                    if (!string.IsNullOrEmpty(UninstallString_32bit))
                                        empty = false;
                                }
                                catch { }

                                // Überprüfen, ob mindestens ein Wert gefunden wurde
                                if (!empty)
                                {
                                    // Create installed software object
                                    Applications_Installed applicationInfo = new Applications_Installed
                                    {
                                        name = string.IsNullOrEmpty(DisplayName_32bit) ? "N/A" : DisplayName_32bit,
                                        version = string.IsNullOrEmpty(DisplayVersion_32bit) ? "N/A" : DisplayVersion_32bit,
                                        installed_date = string.IsNullOrEmpty(InstallDate_32bit) ? "N/A" : InstallDate_32bit,
                                        installation_path = string.IsNullOrEmpty(InstallLocation_32bit) ? "N/A" : InstallLocation_32bit,
                                        vendor = string.IsNullOrEmpty(Publisher_32bit) ? "N/A" : Publisher_32bit,
                                        uninstallation_string = string.IsNullOrEmpty(UninstallString_32bit) ? "N/A" : UninstallString_32bit
                                    };

                                    // Serialize the process object into a JSON string and add it to the list
                                    string applications_installedJson = JsonSerializer.Serialize(applicationInfo, new JsonSerializerOptions { WriteIndented = true });
                                    Logging.Device_Information("Client_Information.Installed_Software.Collect", "applications_installedJson", applications_installedJson);
                                    lock (applications_installedJsonList)
                                    {
                                        applications_installedJsonList.Add(applications_installedJson);
                                    }
                                }
                            }
                        });
                    }


                    //Collect all 64bit installed programs
                    RegistryKey localKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                    localKey = localKey.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");

                    Parallel.ForEach(localKey.GetSubKeyNames(), entry =>
                    {
                        // 64bit
                        string DisplayName_64bit = null;
                        string DisplayVersion_64bit = null;
                        string InstallDate_64bit = null;
                        string InstallLocation_64bit = null;
                        string Publisher_64bit = null;
                        string UninstallString_64bit = null;

                        RegistryKey sub_item = localKey.OpenSubKey(entry);
                        if (sub_item != null)
                        {
                            bool empty = true;

                            try
                            {
                                DisplayName_64bit = sub_item.GetValue("DisplayName")?.ToString();
                                if (!string.IsNullOrEmpty(DisplayName_64bit))
                                    empty = false;
                            }
                            catch { }

                            try
                            {
                                DisplayVersion_64bit = sub_item.GetValue("DisplayVersion")?.ToString();
                                if (!string.IsNullOrEmpty(DisplayVersion_64bit))
                                    empty = false;
                            }
                            catch { }

                            try
                            {
                                InstallDate_64bit = sub_item.GetValue("InstallDate")?.ToString();
                                if (!string.IsNullOrEmpty(InstallDate_64bit))
                                    empty = false;
                            }
                            catch { }

                            try
                            {
                                InstallLocation_64bit = sub_item.GetValue("InstallLocation")?.ToString();
                                if (!string.IsNullOrEmpty(InstallLocation_64bit))
                                    empty = false;
                            }
                            catch { }

                            try
                            {
                                Publisher_64bit = sub_item.GetValue("Publisher")?.ToString();
                                if (!string.IsNullOrEmpty(Publisher_64bit))
                                    empty = false;
                            }
                            catch { }

                            try
                            {
                                UninstallString_64bit = sub_item.GetValue("UninstallString")?.ToString();
                                if (!string.IsNullOrEmpty(UninstallString_64bit))
                                    empty = false;
                            }
                            catch { }

                            if (!empty)
                            {
                                // Erstellen des JSON-Objekts
                                Applications_Installed applicationInfo = new Applications_Installed
                                {
                                    name = string.IsNullOrEmpty(DisplayName_64bit) ? "N/A" : DisplayName_64bit,
                                    version = string.IsNullOrEmpty(DisplayVersion_64bit) ? "N/A" : DisplayVersion_64bit,
                                    installed_date = string.IsNullOrEmpty(InstallDate_64bit) ? "N/A" : InstallDate_64bit,
                                    installation_path = string.IsNullOrEmpty(InstallLocation_64bit) ? "N/A" : InstallLocation_64bit,
                                    vendor = string.IsNullOrEmpty(Publisher_64bit) ? "N/A" : Publisher_64bit,
                                    uninstallation_string = string.IsNullOrEmpty(UninstallString_64bit) ? "N/A" : UninstallString_64bit
                                };

                                // Serialisieren des JSON-Objekts und Hinzufügen zur Liste
                                string applications_installedJson = JsonSerializer.Serialize(applicationInfo, new JsonSerializerOptions { WriteIndented = true });

                                // Sichern des Zugriffs auf die gemeinsame Liste
                                lock (applications_installedJsonList)
                                {
                                    applications_installedJsonList.Add(applications_installedJson);
                                }
                            }
                        }
                    });

                    // Create and log JSON array
                    applications_installed_json = "[" + string.Join("," + Environment.NewLine, applications_installedJsonList) + "]";
                    Logging.Device_Information("Device_Information.Software.Applications_Installed", "applications_installed_json", applications_installed_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Software.Applications_Installed", "Collecting failed (general error)", ex.ToString());
                    applications_installed_json = "[]";
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                string distroInfo = Linux.Helper.Linux.Get_Linux_Distribution().ToLower();

                if (distroInfo == "ubuntu" || distroInfo == "debian")
                {
                    List<string> applications_installedJsonList = new List<string>();

                    try
                    {
                        // Execute the command to get the list of installed packages
                        var installedPackages = Linux.Helper.Bash.Execute_Script("Applications_Installed", false, "apt list --installed 2>/dev/null");

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
                                var applicationInfo = new Applications_Installed
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
                                }
                            }
                        }

                        // Create and log JSON array
                        applications_installed_json = "[" + string.Join("," + Environment.NewLine, applications_installedJsonList) + "]";
                        Logging.Device_Information("Device_Information.Software.Applications_Installed", "applications_installed_json", applications_installed_json);
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Device_Information.Software.Applications_Installed", "Collecting failed (general error)", ex.ToString());
                        applications_installed_json = "[]";
                    }
                }
                else
                {
                    Logging.Error("Device_Information.Software.Applications_Installed", "Collecting failed (unsupported Linux distribution)", distroInfo);
                    applications_installed_json = "[]";
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                List<string> applications_installedJsonList = new List<string>();

                try
                {
                    // Execute the command to get the list of installed applications
                    var installedApplications = MacOS.Helper.Zsh.Execute_Script("Applications", false, "system_profiler SPApplicationsDataType -json");

                    // Parse the JSON output from system_profiler
                    var jsonOutput = JsonDocument.Parse(installedApplications);
                    if (jsonOutput.RootElement.TryGetProperty("SPApplicationsDataType", out var applications))
                    {
                        // Iterate over each application
                        foreach (var app in applications.EnumerateArray())
                        {
                            try
                            {
                                string name = app.TryGetProperty("_name", out var nameProperty) ? nameProperty.GetString() ?? "N/A" : "N/A";
                                string version = app.TryGetProperty("version", out var versionProperty) ? versionProperty.GetString() ?? "N/A" : "N/A";
                                string path = app.TryGetProperty("path", out var pathProperty) ? pathProperty.GetString() ?? "N/A" : "N/A";
                                string vendor = app.TryGetProperty("obtained_from", out var vendorProperty) ? vendorProperty.GetString() ?? "N/A" : "N/A"; // Indicates App Store or direct
                                string installDate = "N/A"; // macOS doesn't provide install date in SPApplicationsDataType

                                // Compile application info into JSON format
                                var applicationInfo = new Applications_Installed
                                {
                                    name = name,
                                    version = version,
                                    installed_date = installDate,
                                    installation_path = path,
                                    vendor = vendor,
                                    uninstallation_string = $"sudo rm -rf \"{path}\"" // Approximation for uninstallation
                                };

                                string applications_installedJson = JsonSerializer.Serialize(applicationInfo, new JsonSerializerOptions { WriteIndented = true });

                                lock (applications_installedJsonList)
                                {
                                    applications_installedJsonList.Add(applications_installedJson);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.Error("Device_Information.Software.Applications_Installed", "Failed to process application info", ex.Message);
                            }
                        }

                        // Create and log JSON array
                        applications_installed_json = "[" + string.Join("," + Environment.NewLine, applications_installedJsonList) + "]";
                        Logging.Device_Information("Device_Information.Software.Applications_Installed", "applications_installed_json", applications_installed_json);
                    }
                    else
                    {
                        Logging.Error("Device_Information.Software.Applications_Installed", "SPApplicationsDataType not found in JSON output", "");
                        applications_installed_json = "[]";
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Software.Applications_Installed", "Collecting failed (general error)", ex.ToString());
                    applications_installed_json = "[]";
                }
            }
            else
            {
                applications_installed_json = "[]";
            }

            return applications_installed_json;
        }

        public static string Applications_Logon()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // Create a list of JSON strings for each installed software
                    List<string> applications_logonJsonList = new List<string>();

                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "select * from Win32_StartupCommand"))
                    {
                        foreach (ManagementObject reader in searcher.Get())
                        {
                            try
                            {
                                // Create logon object
                                Applications_Logon logonInfo = new Applications_Logon
                                {
                                    name = string.IsNullOrEmpty(reader["Name"].ToString()) ? "N/A" : reader["Name"].ToString(),
                                    path = string.IsNullOrEmpty(reader["Location"].ToString()) ? "N/A" : reader["Location"].ToString(),
                                    command = string.IsNullOrEmpty(reader["Command"].ToString()) ? "N/A" : reader["Command"].ToString(),
                                    user = string.IsNullOrEmpty(reader["User"].ToString()) ? "N/A" : reader["User"].ToString(),
                                    user_sid = string.IsNullOrEmpty(reader["UserSID"].ToString()) ? "N/A" : reader["UserSID"].ToString(),
                                };

                                // Serialize the logon object into a JSON string and add it to the list
                                string logonJson = JsonSerializer.Serialize(logonInfo, new JsonSerializerOptions { WriteIndented = true });
                                Logging.Device_Information("Device_Information.Software.Applications_Logon", "logonJson", logonJson);
                                applications_logonJsonList.Add(logonJson);
                            }
                            catch (Exception ex)
                            {
                                Logging.Device_Information("Device_Information.Software.Applications_Logon", "Failed.", ex.Message);
                            }
                        }
                    }

                    // Create and log JSON array
                    string applications_logon_json = "[" + string.Join("," + Environment.NewLine, applications_logonJsonList) + "]";
                    Logging.Device_Information("Device_Information.Software.Applications_Installed", "applications_logon_json", applications_logon_json);
                    return applications_logon_json;
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Software.Applications_Logon", "Collecting failed (general error)", ex.ToString());
                    return "[]";
                }
            }
            else
            {
                Logging.Debug("Device_Information.Software.Applications_Logon", "Operating system is not Windows", "");
                return "[]";
            }

        }

        public static string Applications_Scheduled_Tasks()
        {
            if (OperatingSystem.IsWindows())
            {
                // Create a list of JSON strings for each installed software
                List<string> applications_scheduled_tasksJsonList = new List<string>();

                try
                {
                    Microsoft.Win32.TaskScheduler.Task[] allTasks = TaskService.Instance.FindAllTasks(new Regex(".*")); // this will list ALL tasks for ALL users

                    foreach (Microsoft.Win32.TaskScheduler.Task tsk in allTasks)
                    {
                        try
                        {
                            // Create scheduled task object
                            Applications_Scheduled_Tasks scheduledTaskInfo = new Applications_Scheduled_Tasks
                            {
                                name = string.IsNullOrEmpty(tsk.Name) ? "N/A" : tsk.Name,
                                status = string.IsNullOrEmpty(tsk.State.ToString()) ? "N/A" : tsk.State.ToString(),
                                author = string.IsNullOrEmpty(tsk.Definition.Principal.Account) ? "N/A" : tsk.Definition.Principal.Account,
                                path = string.IsNullOrEmpty(tsk.Path) ? "N/A" : tsk.Path,
                                folder = string.IsNullOrEmpty(tsk.Folder.ToString()) ? "N/A" : tsk.Folder.ToString(),
                                user_sid = string.IsNullOrEmpty(tsk.Definition.Principal.UserId) ? "N/A" : tsk.Definition.Principal.UserId,
                                next_execution = string.IsNullOrEmpty(tsk.NextRunTime.ToString()) ? "N/A" : tsk.NextRunTime.ToString(),
                                last_execution = string.IsNullOrEmpty(tsk.LastRunTime.ToString()) ? "N/A" : tsk.LastRunTime.ToString(),
                            };

                            // Serialize the scheduled task object into a JSON string and add it to the list
                            string scheduledTaskJson = JsonSerializer.Serialize(scheduledTaskInfo, new JsonSerializerOptions { WriteIndented = true });
                            Logging.Device_Information("Device_Information.Software.Applications_Scheduled_Tasks", "scheduledTaskJson", scheduledTaskJson);
                            applications_scheduled_tasksJsonList.Add(scheduledTaskJson);
                        }
                        catch (Exception ex)
                        {
                            Logging.Device_Information("Device_Information.Software.Applications_Scheduled_Tasks", "Failed.", ex.Message);
                        }
                    }

                    // Create and log JSON array
                    string applications_scheduled_tasks_json = "[" + string.Join("," + Environment.NewLine, applications_scheduled_tasksJsonList) + "]";
                    Logging.Device_Information("Device_Information.Software.Applications_Scheduled_Tasks", "applications_scheduled_tasks_json", applications_scheduled_tasks_json);
                    return applications_scheduled_tasks_json;
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Software.Applications_Scheduled_Tasks", "Collecting failed (general error)", ex.ToString());
                    return "[]";
                }
            }
            else
            {
                Logging.Debug("Device_Information.Software.Applications_Scheduled_Tasks", "Operating system is not Windows", "");
                return "[]";
            }
        }

        public static string Applications_Services()
        {
            string applications_services_json = String.Empty;

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // Create a list of JSON strings for each installed service
                    List<string> applications_servicesJsonList = new List<string>();

                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "select * from Win32_Service"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            try
                            {
                                // Create service object
                                Applications_Services serviceInfo = new Applications_Services
                                {
                                    display_name = string.IsNullOrEmpty(obj["DisplayName"].ToString()) ? "N/A" : obj["DisplayName"].ToString(),
                                    name = string.IsNullOrEmpty(obj["Name"].ToString()) ? "N/A" : obj["Name"].ToString(),
                                    status = string.IsNullOrEmpty(obj["State"].ToString()) ? "N/A" : obj["State"].ToString(),
                                    start_type = string.IsNullOrEmpty(obj["StartMode"].ToString()) ? "N/A" : obj["StartMode"].ToString(),
                                    login_as = string.IsNullOrEmpty(obj["StartName"].ToString()) ? "N/A" : obj["StartName"].ToString(),
                                    path = string.IsNullOrEmpty(obj["PathName"].ToString()) ? "N/A" : obj["PathName"].ToString(),
                                    description = string.IsNullOrEmpty(obj["Description"].ToString()) ? "N/A" : obj["Description"].ToString(),
                                };

                                // Serialize the service object into a JSON string and add it to the list
                                string serviceJson = JsonSerializer.Serialize(serviceInfo, new JsonSerializerOptions { WriteIndented = true });
                                Logging.Device_Information("Device_Information.Software.Applications_Services", "serviceJson", serviceJson);
                                applications_servicesJsonList.Add(serviceJson);
                            }
                            catch (Exception ex)
                            {
                                Logging.Device_Information("Device_Information.Software.Applications_Services", "Failed.", ex.Message);
                            }
                        }
                    }

                    // Create and log JSON array
                    applications_services_json = "[" + string.Join("," + Environment.NewLine, applications_servicesJsonList) + "]";
                    Logging.Device_Information("Device_Information.Software.Applications_Services", "applications_services_json", applications_services_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Software.Applications_Services", "Collecting failed (general error)", ex.ToString());
                    applications_services_json = "[]";
                }
            }
            if (OperatingSystem.IsLinux())
            {
                try
                {
                    // Create a list of JSON strings for each service
                    List<string> applications_servicesJsonList = new List<string>();

                    // Execute the systemctl command to list services
                    string output = Linux.Helper.Bash.Execute_Script("Applications_Services", false, "systemctl list-units --type=service --all --no-pager");

                    // Split the output into lines (each line corresponds to a service)
                    var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        try
                        {
                            // Split the line by spaces to get individual fields (columns)
                            var details = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (details.Length >= 5)
                            {
                                // Extract the service information
                                string name = details[0].Trim();
                                string status = details[3].Trim();
                                string description = string.Join(" ", details.Skip(4)).Trim();  // Join the remaining parts as description

                                // Collect the service information
                                Applications_Services serviceInfo = new Applications_Services
                                {
                                    name = name,
                                    status = status,
                                    description = description,
                                    display_name = "N/A", // Linux system doesn't always provide a 'DisplayName'
                                    start_type = "N/A",   // StartType is typically not provided by systemd
                                    login_as = "N/A",     // Not directly available in systemd output
                                    path = "N/A"          // Path information is not always available in systemd output
                                };

                                // Serialize the service object into a JSON string and add it to the list
                                string serviceJson = JsonSerializer.Serialize(serviceInfo, new JsonSerializerOptions { WriteIndented = true });
                                applications_servicesJsonList.Add(serviceJson);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Device_Information("Device_Information.Software.Applications_Services", "Failed to process line.", ex.Message);
                        }
                    }

                    // Create and log the JSON array
                    applications_services_json = "[" + string.Join("," + Environment.NewLine, applications_servicesJsonList) + "]";
                    Logging.Device_Information("Device_Information.Software.Applications_Services", "applications_services_json", applications_services_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Software.Applications_Services", "Collecting failed (general error)", ex.ToString());
                    applications_services_json = "[]";
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    // Create a list of JSON strings for each service
                    List<string> applications_servicesJsonList = new List<string>();

                    // Execute the launchctl command to list services
                    string output = MacOS.Helper.Zsh.Execute_Script("Services", false, "launchctl list");

                    // Split the output into lines (each line corresponds to a service)
                    var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines.Skip(1)) // Skip the header line
                    {
                        try
                        {
                            // Split the line by tabs or spaces to get individual fields (columns)
                            var details = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            if (details.Length >= 3)
                            {
                                string label = details[2].Trim();
                                string status = details[1].Trim() == "0" ? "Running" : "Stopped";

                                // Extract more details from the plist file
                                string plistPath = $"/Library/LaunchDaemons/{label}.plist";
                                string plistContent = MacOS.Helper.Zsh.Execute_Script("Plist", false, $"plutil -p \"{plistPath}\"");

                                string startType = plistContent.Contains("KeepAlive") ? "Automatic" : "Manual";
                                string loginAs = plistContent.Contains("UserName") ? "User Defined" : "root";

                                string programPath = plistContent.Contains("Program") ? ExtractValueFromPlist(plistContent, "Program") : "N/A";

                                Applications_Services serviceInfo = new Applications_Services
                                {
                                    name = label,
                                    status = status,
                                    description = "N/A",
                                    display_name = label,
                                    start_type = startType,
                                    login_as = loginAs,
                                    path = programPath
                                };
                                // Serialize the service object into a JSON string and add it to the list
                                string serviceJson = JsonSerializer.Serialize(serviceInfo, new JsonSerializerOptions { WriteIndented = true });
                                applications_servicesJsonList.Add(serviceJson);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Device_Information("Device_Information.Software.Applications_Services", "Failed to process line.", ex.Message);
                        }
                    }

                    // Create and log the JSON array
                    applications_services_json = "[" + string.Join("," + Environment.NewLine, applications_servicesJsonList) + "]";
                    Logging.Device_Information("Device_Information.Software.Applications_Services", "applications_services_json", applications_services_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Software.Applications_Services", "Collecting failed (general error)", ex.ToString());
                    applications_services_json = "[]";
                }
            }

            return applications_services_json;
        }
        private static string ExtractValueFromPlist(string plistContent, string key)
        {
            var match = Regex.Match(plistContent, $"\"{key}\" => \"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : "N/A";
        }

        public static string Applications_Drivers()
        {
            string applications_drivers_json = String.Empty;

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // Create a list of JSON strings for each installed driver
                    List<string> applications_driversJsonList = new List<string>();

                    // Get all drivers from the Win32_SystemDriver class
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "select * from Win32_SystemDriver"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            try
                            {
                                //Get image version
                                var file_info = FileVersionInfo.GetVersionInfo(obj["PathName"].ToString());
                                string image_version = file_info.FileVersion;

                                // Create driver object
                                Applications_Drivers driverInfo = new Applications_Drivers
                                {
                                    display_name = string.IsNullOrEmpty(obj["DisplayName"].ToString()) ? "N/A" : obj["DisplayName"].ToString(),
                                    name = string.IsNullOrEmpty(obj["DisplayName"].ToString()) ? "N/A" : obj["DisplayName"].ToString(),
                                    description = string.IsNullOrEmpty(obj["Description"].ToString()) ? "N/A" : obj["Description"].ToString(),
                                    status = string.IsNullOrEmpty(obj["state"].ToString()) ? "N/A" : obj["state"].ToString(),
                                    type = string.IsNullOrEmpty(obj["ServiceType"].ToString()) ? "N/A" : obj["ServiceType"].ToString(),
                                    start_type = string.IsNullOrEmpty(obj["StartMode"].ToString()) ? "N/A" : obj["StartMode"].ToString(),
                                    path = string.IsNullOrEmpty(obj["PathName"].ToString()) ? "N/A" : obj["PathName"].ToString(),
                                    version = string.IsNullOrEmpty(image_version) ? "N/A" : image_version,
                                };

                                // Serialize the driver object into a JSON string and add it to the list
                                string driverJson = JsonSerializer.Serialize(driverInfo, new JsonSerializerOptions { WriteIndented = true });
                                Logging.Device_Information("Device_Information.Software.Applications_Drivers", "driverJson", driverJson);
                                applications_driversJsonList.Add(driverJson);
                            }
                            catch (Exception ex)
                            {
                                Logging.Device_Information("Device_Information.Software.Applications_Drivers", "Failed.", ex.Message);
                            }
                        }
                    }

                    // Create and log JSON array
                    applications_drivers_json = "[" + string.Join("," + Environment.NewLine, applications_driversJsonList) + "]";
                    Logging.Device_Information("Device_Information.Software.Applications_Drivers", "applications_drivers_json", applications_drivers_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Software.Applications_Drivers", "Collecting failed (general error)", ex.ToString());
                    applications_drivers_json = "[]";
                }
            }
            else
            {
                Logging.Debug("Device_Information.Software.Applications_Drivers", "Operating system is not Windows", "");
                applications_drivers_json = "[]";
            }

            return applications_drivers_json;
        }

        public static string Cronjobs()
        {
            string cronjobsJson = String.Empty;

            // Liste zum Speichern der JSON-Daten für jeden Cronjob
            List<string> cronjobsJsonList = new List<string>();

            if (OperatingSystem.IsLinux())
            {
                try
                {
                    // Verwenden von `awk` oder besserem Parsing-Befehl
                    string output = Linux.Helper.Bash.Execute_Script("Cronjobs", false, "systemctl list-timers --all --no-pager | awk 'NR>1 {for(i=1;i<=NF;i++) printf \"%s|\", $i; printf \"\\n\"}'");
                    // Aufteilen der Ausgabe in Zeilen
                    var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        // Aufteilen jeder Zeile anhand des Pipesymbols '|'
                        var details = line.Split('|', StringSplitOptions.RemoveEmptyEntries);

                        // Überprüfen, ob genügend Felder vorhanden sind
                        if (details.Length >= 7)
                        {
                            string cronjobNextRun = details[0].Trim() + " " + details[1].Trim() + " " + details[2].Trim();
                            string cronjobLeft = details[3].Trim();
                            string cronjobLastRun = details[4].Trim() + " " + details[5].Trim();
                            string cronjobPassed = details[6].Trim();
                            string cronjobUnit = details.Length > 7 ? details[7].Trim() : string.Empty;
                            string activates = details.Length > 8 ? details[8].Trim() : string.Empty;

                            // Erstellen eines Cronjobs-Objekts
                            var cronjobInfo = new Cronjobs
                            {
                                next = cronjobNextRun,
                                left = cronjobLeft,
                                last = cronjobLastRun,
                                passed = cronjobPassed,
                                unit = cronjobUnit,
                                activates = activates
                            };

                            // JSON-Serialisierung
                            string cronjobJson = JsonSerializer.Serialize(cronjobInfo, new JsonSerializerOptions { WriteIndented = true });

                            lock (cronjobsJsonList)
                            {
                                cronjobsJsonList.Add(cronjobJson);
                            }
                        }
                    }

                    // JSON-Array aus der Liste erstellen
                    cronjobsJson = "[" + string.Join("," + Environment.NewLine, cronjobsJsonList) + "]";
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Software.Cronjobs", "Collecting failed (general error)", ex.ToString());
                    cronjobsJson = "[]";
                }
            }
            else
            {
                cronjobsJson = "[]";
            }

            return "[]"; // return empty JSON array till the implementation is done
        }


    }
}
