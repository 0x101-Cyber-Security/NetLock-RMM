using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static NetLock_RMM_Comm_Agent_Windows.Online_Mode.Handler;
using Microsoft.Win32.TaskScheduler;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace NetLock_RMM_Comm_Agent_Windows.Device_Information
{
    internal class Software
    {
        // Hilfsmethode zum Abrufen eines Registrierungswerts oder "N/A", wenn er nicht vorhanden ist
        private string GetValueOrDefault(RegistryKey key, string valueName)
        {
            object value = key.GetValue(valueName);
            return value != null ? value.ToString() : "N/A";
        }

        public static string Applications_Installed()
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
                                string applications_installedJson = JsonConvert.SerializeObject(applicationInfo, Formatting.Indented);
                                Logging.Handler.Device_Information("Client_Information.Installed_Software.Collect", "applications_installedJson", applications_installedJson);
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
                            string applications_installedJson = JsonConvert.SerializeObject(applicationInfo, Formatting.Indented);

                            // Sichern des Zugriffs auf die gemeinsame Liste
                            lock (applications_installedJsonList)
                            {
                                applications_installedJsonList.Add(applications_installedJson);
                            }
                        }
                    }
                });

                // Create and log JSON array
                string applications_installed_json = "[" + string.Join("," + Environment.NewLine, applications_installedJsonList) + "]";
                Logging.Handler.Device_Information("Device_Information.Software.Applications_Installed", "applications_installed_json", applications_installed_json);
                return applications_installed_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Software.Applications_Installed", "Collecting failed (general error)", ex.ToString());
                return "[]";
            }
        }

        public static string Applications_Logon()
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
                            string logonJson = JsonConvert.SerializeObject(logonInfo, Formatting.Indented);
                            Logging.Handler.Device_Information("Device_Information.Software.Applications_Logon", "logonJson", logonJson);
                            applications_logonJsonList.Add(logonJson);
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Device_Information("Device_Information.Software.Applications_Logon", "Failed.", ex.Message);
                        }
                    }
                }

                // Create and log JSON array
                string applications_logon_json = "[" + string.Join("," + Environment.NewLine, applications_logonJsonList) + "]";
                Logging.Handler.Device_Information("Device_Information.Software.Applications_Installed", "applications_logon_json", applications_logon_json);
                return applications_logon_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Software.Applications_Logon", "Collecting failed (general error)", ex.ToString());
                return "[]";
            }
        }

        public static string Applications_Scheduled_Tasks()
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
                        string scheduledTaskJson = JsonConvert.SerializeObject(scheduledTaskInfo, Formatting.Indented);
                        Logging.Handler.Device_Information("Device_Information.Software.Applications_Scheduled_Tasks", "scheduledTaskJson", scheduledTaskJson);
                        applications_scheduled_tasksJsonList.Add(scheduledTaskJson);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Device_Information("Device_Information.Software.Applications_Scheduled_Tasks", "Failed.", ex.Message);
                    }
                }

                // Create and log JSON array
                string applications_scheduled_tasks_json = "[" + string.Join("," + Environment.NewLine, applications_scheduled_tasksJsonList) + "]";
                Logging.Handler.Device_Information("Device_Information.Software.Applications_Scheduled_Tasks", "applications_scheduled_tasks_json", applications_scheduled_tasks_json);
                return applications_scheduled_tasks_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Software.Applications_Scheduled_Tasks", "Collecting failed (general error)", ex.ToString());
                return "[]";
            }
        }

        public static string Applications_Services()
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
                            string serviceJson = JsonConvert.SerializeObject(serviceInfo, Formatting.Indented);
                            Logging.Handler.Device_Information("Device_Information.Software.Applications_Services", "serviceJson", serviceJson);
                            applications_servicesJsonList.Add(serviceJson);
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Device_Information("Device_Information.Software.Applications_Services", "Failed.", ex.Message);
                        }
                    }
                }

                // Create and log JSON array
                string applications_services_json = "[" + string.Join("," + Environment.NewLine, applications_servicesJsonList) + "]";
                Logging.Handler.Device_Information("Device_Information.Software.Applications_Services", "applications_services_json", applications_services_json);
                return applications_services_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Software.Applications_Services", "Collecting failed (general error)", ex.ToString());
                return "[]";
            }
        }

        public static string Applications_Drivers()
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
                            string driverJson = JsonConvert.SerializeObject(driverInfo, Formatting.Indented);
                            Logging.Handler.Device_Information("Device_Information.Software.Applications_Drivers", "driverJson", driverJson);
                            applications_driversJsonList.Add(driverJson);
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Device_Information("Device_Information.Software.Applications_Drivers", "Failed.", ex.Message);
                        }
                    }
                }

                // Create and log JSON array
                string applications_drivers_json = "[" + string.Join("," + Environment.NewLine, applications_driversJsonList) + "]";
                Logging.Handler.Device_Information("Device_Information.Software.Applications_Drivers", "applications_drivers_json", applications_drivers_json);
                return applications_drivers_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Software.Applications_Drivers", "Collecting failed (general error)", ex.ToString());
                return "[]";
            }
        }
    }
}
