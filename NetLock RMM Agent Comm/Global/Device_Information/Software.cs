using Global.Helper;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using NetLock_RMM_Agent_Comm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static Global.Online_Mode.Handler;

namespace Global.Device_Information
{
    internal class Software
    {
        public static string Applications_Installed()
        {
            string applications_installed_json = String.Empty;
            List<Applications_Installed> currentApplications = new List<Applications_Installed>();

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

                                // Check if at least one value was found
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
                                        currentApplications.Add(applicationInfo); // Add object to comparison list
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
                                // Create JSON object
                                Applications_Installed applicationInfo = new Applications_Installed
                                {
                                    name = string.IsNullOrEmpty(DisplayName_64bit) ? "N/A" : DisplayName_64bit,
                                    version = string.IsNullOrEmpty(DisplayVersion_64bit) ? "N/A" : DisplayVersion_64bit,
                                    installed_date = string.IsNullOrEmpty(InstallDate_64bit) ? "N/A" : InstallDate_64bit,
                                    installation_path = string.IsNullOrEmpty(InstallLocation_64bit) ? "N/A" : InstallLocation_64bit,
                                    vendor = string.IsNullOrEmpty(Publisher_64bit) ? "N/A" : Publisher_64bit,
                                    uninstallation_string = string.IsNullOrEmpty(UninstallString_64bit) ? "N/A" : UninstallString_64bit
                                };

                                // Ensure thread-safe access to shared list
                                lock (applications_installedJsonList)
                                {
                                    // Serialize the JSON object and add it to the list
                                    string applications_installedJson = JsonSerializer.Serialize(applicationInfo, new JsonSerializerOptions { WriteIndented = true });

                                    applications_installedJsonList.Add(applications_installedJson);
                                    currentApplications.Add(applicationInfo); // Add object to comparison list
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
                                    currentApplications.Add(applicationInfo); // Add object to comparison list
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
                                    currentApplications.Add(applicationInfo); // Add object to comparison list
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

            // Compare on object level instead of string comparison
            bool hasChanges = false;

            Console.WriteLine("checking for application changes");
            Console.WriteLine($"application: {(Device_Worker.applicationsInstalledJson == null ? "null" : Device_Worker.applicationsInstalledJson.Length.ToString())}");

            if (Device_Worker.applicationsInstalledJson == null)
            {
                Console.WriteLine("applicationsInstalledJson is null, changes are accepted.");
                hasChanges = true;
            }
            else
            {
                try
                {
                    Console.WriteLine("Trying to deserialize previous applications from JSON...");
                    // Try to deserialize previous applications from JSON
                    var previousApplications = JsonSerializer.Deserialize<List<Applications_Installed>>(
                        Device_Worker.applicationsInstalledJson.StartsWith("[") ?
                        Device_Worker.applicationsInstalledJson :
                        "[]") ?? new List<Applications_Installed>();

                    Console.WriteLine($"Previous applications loaded: {previousApplications.Count}, current applications: {currentApplications.Count}");

                    // Check if number of applications has changed
                    if (previousApplications.Count != currentApplications.Count)
                    {
                        Console.WriteLine("Number of applications has changed.");
                        hasChanges = true;
                    }
                    else
                    {
                        Console.WriteLine("Number of applications is unchanged, check details...");
                        // Check if number of applications is unchanged, check details...
                        // Compare applications by name and version, but prevent duplicate keys
                        var currentDict = new Dictionary<string, Applications_Installed>();
                        foreach (var a in currentApplications)
                        {
                            string key = (a.name ?? "N/A") + "|" + (a.version ?? "N/A");
                            if (!currentDict.ContainsKey(key))
                                currentDict[key] = a;
                        }
                        foreach (var app in previousApplications)
                        {
                            string key = (app.name ?? "N/A") + "|" + (app.version ?? "N/A");
                            if (!currentDict.ContainsKey(key))
                            {
                                Console.WriteLine($"Application changed or removed: {key}");
                                hasChanges = true;
                                break;
                            }
                            Console.WriteLine($"Application unchanged: {key}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing or comparing applications: {ex.Message}");
                    // Treat deserialization problems as a change
                    hasChanges = true;
                }
            }

            if (!hasChanges)
            {
                Logging.Device_Information("Device_Information.Software.Applications_Installed", "No changes detected since last check.", "");
                Console.WriteLine("No new application data found.");
                return "-";
            }
            else
            {
                Console.WriteLine("New application data found, updating applicationsInstalledJson.");
                Device_Worker.applicationsInstalledJson = applications_installed_json;
                return applications_installed_json;
            }
        }

        public static string Applications_Logon()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // List for collecting logon objects for comparison
                    List<Applications_Logon> currentLogons = new List<Applications_Logon>();
                    
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

                                // Collect object for comparison list
                                currentLogons.Add(logonInfo);

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
                    Logging.Device_Information("Device_Information.Software.Applications_Logon", "applications_logon_json", applications_logon_json);

                    // Object-based comparison instead of string comparison
                    bool hasChanges = false;
                    
                    if (string.IsNullOrEmpty(Device_Worker.applicationsLogonJson) || Device_Worker.applicationsLogonJson == "[]")
                    {
                        hasChanges = true;
                        Logging.Device_Information("Device_Information.Software.Applications_Logon", "First capture or empty previous data", "");
                    }
                    else
                    {
                        try
                        {
                            // Try to deserialize previous logons from JSON
                            var previousLogons = JsonSerializer.Deserialize<List<Applications_Logon>>(
                                Device_Worker.applicationsLogonJson.StartsWith("[") ? 
                                Device_Worker.applicationsLogonJson : 
                                "[]") ?? new List<Applications_Logon>();

                            // Check if number of logons has changed
                            if (previousLogons.Count != currentLogons.Count)
                            {
                                hasChanges = true;
                                Logging.Device_Information("Device_Information.Software.Applications_Logon", 
                                    "Changes detected: Different number of logon entries", 
                                    $"Previous: {previousLogons.Count}, Current: {currentLogons.Count}");
                            }
                            else
                            {
                                // Create dictionary for fast access and comparison
                                var currentDict = new Dictionary<string, Applications_Logon>();
                                foreach (var logon in currentLogons)
                                {
                                    string key = (logon.name ?? "N/A") + "|" + (logon.command ?? "N/A");
                                    if (!currentDict.ContainsKey(key))
                                        currentDict[key] = logon;
                                }

                                // Compare each previous entry with the current ones
                                foreach (var logon in previousLogons)
                                {
                                    string key = (logon.name ?? "N/A") + "|" + (logon.command ?? "N/A");
                                    
                                    // If a previous entry is no longer present or has changed
                                    if (!currentDict.TryGetValue(key, out var currentLogon) ||
                                        logon.path != currentLogon.path ||
                                        logon.user != currentLogon.user ||
                                        logon.user_sid != currentLogon.user_sid)
                                    {
                                        hasChanges = true;
                                        Logging.Device_Information("Device_Information.Software.Applications_Logon", 
                                            "Changes detected in logon entry", logon.name);
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Treat deserialization problems as a change
                            hasChanges = true;
                            Logging.Error("Device_Information.Software.Applications_Logon", 
                                "Error in comparison", ex.ToString());
                        }
                    }

                    if (!hasChanges)
                    {
                        Logging.Device_Information("Device_Information.Software.Applications_Logon", "No changes detected since last check.", "");
                        Console.WriteLine("Applications_Logon: " + "No changes detected since last check");
                        return "-";
                    }
                    else
                    {
                        Console.WriteLine("Applications_Logon: " + "New data found, updating applicationsLogonJson.");
                        Device_Worker.applicationsLogonJson = applications_logon_json;
                        return applications_logon_json;
                    }
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
                // List for collecting task objects for comparison
                List<Applications_Scheduled_Tasks> currentTasks = new List<Applications_Scheduled_Tasks>();
                
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

                            // Add object to comparison list
                            currentTasks.Add(scheduledTaskInfo);

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

                    // Object-based comparison instead of string comparison
                    bool hasChanges = false;
                    
                    if (string.IsNullOrEmpty(Device_Worker.applicationsScheduledTasksJson) || Device_Worker.applicationsScheduledTasksJson == "[]")
                    {
                        hasChanges = true;
                        Logging.Device_Information("Device_Information.Software.Applications_Scheduled_Tasks", "First capture or empty previous data", "");
                        Console.WriteLine("Applications_Scheduled_Tasks: First capture or empty previous data");
                    }
                    else
                    {
                        try
                        {
                            Console.WriteLine("Applications_Scheduled_Tasks: Trying to deserialize previous tasks from JSON...");
                            
                            // Try to deserialize previous tasks from JSON
                            var previousTasks = JsonSerializer.Deserialize<List<Applications_Scheduled_Tasks>>(
                                Device_Worker.applicationsScheduledTasksJson.StartsWith("[") ? 
                                Device_Worker.applicationsScheduledTasksJson : 
                                "[]") ?? new List<Applications_Scheduled_Tasks>();

                            Console.WriteLine($"Applications_Scheduled_Tasks: Previous tasks loaded: {previousTasks.Count}, current tasks: {currentTasks.Count}");

                            // Check if number of tasks has changed
                            if (previousTasks.Count != currentTasks.Count)
                            {
                                hasChanges = true;
                                Logging.Device_Information("Device_Information.Software.Applications_Scheduled_Tasks", 
                                    "Changes detected: Different number of scheduled tasks", 
                                    $"Previous: {previousTasks.Count}, Current: {currentTasks.Count}");
                                Console.WriteLine("Applications_Scheduled_Tasks: Number of tasks has changed.");
                            }
                            else
                            {
                                Console.WriteLine("Applications_Scheduled_Tasks: Number of tasks is unchanged, check details...");
                                
                                // Create dictionary for fast access and comparison
                                var currentDict = new Dictionary<string, Applications_Scheduled_Tasks>();
                                foreach (var task in currentTasks)
                                {
                                    // Use name and path as unique key
                                    string key = (task.name ?? "N/A") + "|" + (task.path ?? "N/A");
                                    if (!currentDict.ContainsKey(key))
                                        currentDict[key] = task;
                                }

                                // Compare each previous task with the current ones
                                foreach (var task in previousTasks)
                                {
                                    string key = (task.name ?? "N/A") + "|" + (task.path ?? "N/A");
                                    
                                    // If a previous task is no longer present or important properties have changed
                                    if (!currentDict.TryGetValue(key, out var currentTask) ||
                                        task.status != currentTask.status ||
                                        task.author != currentTask.author ||
                                        task.user_sid != currentTask.user_sid)
                                    {
                                        hasChanges = true;
                                        Logging.Device_Information("Device_Information.Software.Applications_Scheduled_Tasks", 
                                            "Changes detected in scheduled task", task.name);
                                        Console.WriteLine($"Applications_Scheduled_Tasks: Task changed or removed: {task.name}");
                                        break;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Treat deserialization problems as a change
                            hasChanges = true;
                            Logging.Error("Device_Information.Software.Applications_Scheduled_Tasks", 
                                "Error in comparison", ex.ToString());
                            Console.WriteLine($"Applications_Scheduled_Tasks: Error deserializing or comparing: {ex.Message}");
                        }
                    }

                    if (!hasChanges)
                    {
                        Logging.Device_Information("Device_Information.Software.Applications_Scheduled_Tasks", "No changes detected since last check.", "");
                        Console.WriteLine("Applications_Scheduled_Tasks: No changes detected.");
                        return "-";
                    }
                    else
                    {
                        Console.WriteLine("Applications_Scheduled_Tasks: New data found, updating applicationsScheduledTasksJson.");
                        Device_Worker.applicationsScheduledTasksJson = applications_scheduled_tasks_json;
                        return applications_scheduled_tasks_json;
                    }
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
            // List for collecting service objects for comparison
            List<Applications_Services> currentServices = new List<Applications_Services>();

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

                                // Add object to comparison list
                                currentServices.Add(serviceInfo);

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
            else if (OperatingSystem.IsLinux())
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

                                // Add object to comparison list
                                currentServices.Add(serviceInfo);

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
                                
                                // Add object to comparison list
                                currentServices.Add(serviceInfo);
                                
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
            else
            {
                applications_services_json = "[]";
            }

            // Object-based comparison instead of string comparison
            bool hasChanges = false;
            
            Console.WriteLine("Checking for service changes...");
            
            if (string.IsNullOrEmpty(Device_Worker.applicationsServicesJson) || Device_Worker.applicationsServicesJson == "[]")
            {
                hasChanges = true;
                Logging.Device_Information("Device_Information.Software.Applications_Services", "First capture or empty previous data", "");
                Console.WriteLine("Applications_Services: First capture or empty previous data");
            }
            else
            {
                try
                {
                    Console.WriteLine("Applications_Services: Trying to deserialize previous services from JSON...");
                    
                    // Try to deserialize previous services from JSON
                    var previousServices = JsonSerializer.Deserialize<List<Applications_Services>>(
                        Device_Worker.applicationsServicesJson.StartsWith("[") ? 
                        Device_Worker.applicationsServicesJson : 
                        "[]") ?? new List<Applications_Services>();

                    Console.WriteLine($"Applications_Services: Previous services loaded: {previousServices.Count}, current services: {currentServices.Count}");

                    // Check if number of services has changed
                    if (previousServices.Count != currentServices.Count)
                    {
                        hasChanges = true;
                        Logging.Device_Information("Device_Information.Software.Applications_Services", 
                            "Changes detected: Different number of services", 
                            $"Previous: {previousServices.Count}, Current: {currentServices.Count}");
                        Console.WriteLine("Applications_Services: Number of services has changed.");
                    }
                    else
                    {
                        Console.WriteLine("Applications_Services: Number of services is unchanged, check details...");
                        
                        // Create dictionary for fast access and comparison
                        var currentDict = new Dictionary<string, Applications_Services>();
                        foreach (var service in currentServices)
                        {
                            // Use name as unique key (Name is usually unique for services)
                            string key = service.name ?? "N/A";
                            if (!currentDict.ContainsKey(key))
                                currentDict[key] = service;
                        }

                        // Compare each previous service with the current ones
                        foreach (var service in previousServices)
                        {
                            string key = service.name ?? "N/A";
                            
                            // If a previous service is no longer present or important properties have changed
                            if (!currentDict.TryGetValue(key, out var currentService) ||
                                service.status != currentService.status ||
                                service.start_type != currentService.start_type ||
                                service.path != currentService.path)
                            {
                                hasChanges = true;
                                Logging.Device_Information("Device_Information.Software.Applications_Services", 
                                    "Changes detected in service", service.name);
                                Console.WriteLine($"Applications_Services: Service changed or removed: {service.name}");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Treat deserialization problems as a change
                    hasChanges = true;
                    Logging.Error("Device_Information.Software.Applications_Services", 
                        "Error in comparison", ex.ToString());
                    Console.WriteLine($"Applications_Services: Error deserializing or comparing: {ex.Message}");
                }
            }

            if (!hasChanges)
            {
                Logging.Device_Information("Device_Information.Software.Applications_Services", "No changes detected since last check.", "");
                Console.WriteLine("Applications_Services: No changes detected.");
                return "-";
            }
            else
            {
                Console.WriteLine("Applications_Services: New data found, updating applicationsServicesJson.");
                Device_Worker.applicationsServicesJson = applications_services_json;
                return applications_services_json;
            }
        }

        private static string ExtractValueFromPlist(string plistContent, string key)
        {
            var match = Regex.Match(plistContent, $"\"{key}\" => \"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : "N/A";
        }

        public static string Applications_Drivers()
        {
            string applications_drivers_json = String.Empty;
            // List for collecting driver objects for comparison
            List<Applications_Drivers> currentDrivers = new List<Applications_Drivers>();

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

                                // Add object to comparison list
                                currentDrivers.Add(driverInfo);

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


            // Object-based comparison instead of string comparison
            bool hasChanges = false;
        
            if (string.IsNullOrEmpty(Device_Worker.applicationsDriversJson) || Device_Worker.applicationsDriversJson == "[]")
            {
                hasChanges = true;
                Logging.Device_Information("Device_Information.Software.Applications_Drivers", "First capture or empty previous data", "");
            }
            else
            {
                try
                {            
                    // Try to deserialize previous drivers from JSON
                    var previousDrivers = JsonSerializer.Deserialize<List<Applications_Drivers>>(
                        Device_Worker.applicationsDriversJson.StartsWith("[") ? 
                        Device_Worker.applicationsDriversJson : 
                        "[]") ?? new List<Applications_Drivers>();

                    // Check if number of drivers has changed
                    if (previousDrivers.Count != currentDrivers.Count)
                    {
                        hasChanges = true;
                        Logging.Device_Information("Device_Information.Software.Applications_Drivers", 
                            "Changes detected: Different number of drivers", 
                            $"Previous: {previousDrivers.Count}, Current: {currentDrivers.Count}");
                    }
                    else
                    {                        
                        // Create dictionary for fast access and comparison
                        var currentDict = new Dictionary<string, Applications_Drivers>();
                        foreach (var driver in currentDrivers)
                        {
                            // Use name and path as unique key
                            string key = (driver.name ?? "N/A") + "|" + (driver.path ?? "N/A");
                            if (!currentDict.ContainsKey(key))
                                currentDict[key] = driver;
                        }

                        // Compare each previous driver with the current ones
                        foreach (var driver in previousDrivers)
                        {
                            string key = (driver.name ?? "N/A") + "|" + (driver.path ?? "N/A");
                            
                            // If a previous driver is no longer present or important properties have changed
                            if (!currentDict.TryGetValue(key, out var currentDriver) ||
                                driver.status != currentDriver.status ||
                                driver.version != currentDriver.version ||
                                driver.start_type != currentDriver.start_type)
                            {
                                hasChanges = true;
                                Logging.Device_Information("Device_Information.Software.Applications_Drivers", 
                                    "Changes detected in driver", driver.name);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Treat deserialization problems as a change
                    hasChanges = true;
                    Logging.Error("Device_Information.Software.Applications_Drivers", 
                        "Error in comparison", ex.ToString());
                }
            }

            if (!hasChanges)
            {
                Logging.Device_Information("Device_Information.Software.Applications_Drivers", "No changes detected since last check.", "");
                return "-";
            }
            else
            {
                Device_Worker.applicationsDriversJson = applications_drivers_json;
                return applications_drivers_json;
            }
        }

        public static string Cronjobs()
        {
            string cronjobsJson = String.Empty;

            // List for storing JSON data for each Cronjob
            List<string> cronjobsJsonList = new List<string>();

            if (OperatingSystem.IsLinux())
            {
                try
                {
                    // Use `awk` or better parsing command
                    string output = Linux.Helper.Bash.Execute_Script("Cronjobs", false, "systemctl list-timers --all --no-pager | awk 'NR>1 {for(i=1;i<=NF;i++) printf \"%s|\", $i; printf \"\\n\"}'");
                    // Split output into lines
                    var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var line in lines)
                    {
                        // Split each line by the pipe symbol '|'
                        var details = line.Split('|', StringSplitOptions.RemoveEmptyEntries);

                        // Check if enough fields are present
                        if (details.Length >= 7)
                        {
                            string cronjobNextRun = details[0].Trim() + " " + details[1].Trim() + " " + details[2].Trim();
                            string cronjobLeft = details[3].Trim();
                            string cronjobLastRun = details[4].Trim() + " " + details[5].Trim();
                            string cronjobPassed = details[6].Trim();
                            string cronjobUnit = details.Length > 7 ? details[7].Trim() : string.Empty;
                            string activates = details.Length > 8 ? details[8].Trim() : string.Empty;

                            // Create a cronjob object
                            var cronjobInfo = new Cronjobs
                            {
                                next = cronjobNextRun,
                                left = cronjobLeft,
                                last = cronjobLastRun,
                                passed = cronjobPassed,
                                unit = cronjobUnit,
                                activates = activates
                            };

                            // JSON serialization
                            string cronjobJson = JsonSerializer.Serialize(cronjobInfo, new JsonSerializerOptions { WriteIndented = true });

                            lock (cronjobsJsonList)
                            {
                                cronjobsJsonList.Add(cronjobJson);
                            }
                        }
                    }

                    // JSON array from the list
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

            // Check if the JSON matches with the previously collected JSON, if yes, return empty string
            if (Device_Worker.cronjobsJson == cronjobsJson)
            {
                Logging.Device_Information("Device_Information.Software.Cronjobs", "No changes detected since last check.", "");
                return "-";
            }
            else
            {
                Device_Worker.cronjobsJson = cronjobsJson;
                return "[]"; // return empty JSON array till the implementation is done
            }
        }
    }
}
