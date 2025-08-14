using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using static Global.Online_Mode.Handler;
using Windows.Helper;
using Global.Helper;
using System.Text.Json;
using NetLock_RMM_Agent_Comm;
using System.Diagnostics.Eventing.Reader;
using System.Net.Http.Headers;

namespace Global.Device_Information
{
    internal class OS
    {
        public static string Version()
        {
            if (OperatingSystem.IsWindows())
            {
                string operating_system = "-";

                try
                {
                    bool windows11 = false;

                    string _CurrentBuild = Registry.HKLM_Read_Value("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "CurrentBuild");
                    string _ProductName = Registry.HKLM_Read_Value("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "ProductName");
                    string _DisplayVersion = Registry.HKLM_Read_Value("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "DisplayVersion");

                    int CurrentBuild = Convert.ToInt32(_CurrentBuild);

                    // If the build is >= 22000, it could be Windows 11.
                    // But if the product name contains "Server", it is a Windows Server version.
                    if (CurrentBuild >= 22000 && !_ProductName.Contains("Server", StringComparison.OrdinalIgnoreCase))
                    {
                        windows11 = true;
                    }

                    operating_system = windows11 ? "Windows 11" + " (" + _DisplayVersion + ")" : _ProductName + " (" + _DisplayVersion + ")";

                    return operating_system ?? "";
                }
                catch (Exception ex)
                {
                    Logging.Error("NetLock_RMM_Comm_Agent_Windows.Helper.Windows.Windows_Version", "Collect windows product name & version", ex.ToString());
                    return "-";
                }

            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    string osReleaseFile = "/etc/os-release";
                    if (!File.Exists(osReleaseFile))
                    {
                        throw new FileNotFoundException("OS release file not found.");
                    }

                    string[] lines = File.ReadAllLines(osReleaseFile);
                    string version = "-";
                    string name = "-";

                    foreach (string line in lines)
                    {
                        if (line.StartsWith("NAME="))
                        {
                            name = line.Replace("NAME=", "").Replace("\"", "").Trim();
                        }
                        if (line.StartsWith("VERSION="))
                        {
                            version = line.Replace("VERSION=", "").Replace("\"", "").Trim();
                        }
                    }

                    return $"{name} {version}";
                }
                catch (Exception ex)
                {
                    Logging.Error("LinuxHelper.UbuntuVersion", "Error retrieving Ubuntu version", ex.Message);
                    return "-";
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    // macOS Systeminformationen abrufen
                    string systemInfo = MacOS.Helper.Zsh.Execute_Script("Version", false, "sw_vers");
                    string productName = "-";
                    string productVersion = "-";
                    string buildVersion = "-";

                    // Zeilen parsen
                    string[] lines = systemInfo.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("ProductName:"))
                        {
                            productName = line.Replace("ProductName:", "").Trim();
                        }
                        else if (line.StartsWith("ProductVersion:"))
                        {
                            productVersion = line.Replace("ProductVersion:", "").Trim();
                        }
                        else if (line.StartsWith("BuildVersion:"))
                        {
                            buildVersion = line.Replace("BuildVersion:", "").Trim();
                        }
                    }

                    return $"{productName} {productVersion} (Build {buildVersion})";
                }
                catch (Exception ex)
                {
                    Logging.Error("MacOSHelper.VersionInfo", "Error retrieving macOS version", ex.Message);
                    return "-";
                }
            }

            return "-";
        }

        public static string GetActiveAntivirusProduct()
        {
            if (OperatingSystem.IsWindows())
            {
                // Bitmaske für "Real-Time Protection aktiviert" (0x10)
                int RealTimeProtectionFlag = 0x10;

                try
                {
                    var products = new List<(string Name, int State, DateTime? Timestamp)>();

                    using (var searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT displayName, productState, timestamp FROM AntiVirusProduct"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            var name = obj["displayName"]?.ToString() ?? "Unknown AV";
                            var state = Convert.ToInt32(obj["productState"] ?? 0);

                            // Convert timestamp in WMI format to DateTime (if available)
                            DateTime? ts = null;
                            if (obj["timestamp"] != null)
                            {
                                try
                                {
                                    ts = ManagementDateTimeConverter.ToDateTime(obj["timestamp"].ToString());
                                }
                                catch
                                {
                                    // Ignore errors during parsing
                                }
                            }

                            products.Add((name, state, ts));
                            Logging.Device_Information("Device_Information.OS.AV_Candidates", name, $"State={state}, Timestamp={ts?.ToString("o") ?? "n/a"}");
                        }
                    }

                    if (products.Count == 0)
                        return "-";

                    // 1) If there is at least one timestamp -> the latest product
                    var withTimestamp = products.Where(p => p.Timestamp.HasValue);
                    if (withTimestamp.Any())
                    {
                        var latestByTime = withTimestamp
                            .OrderByDescending(p => p.Timestamp.Value)
                            .First();
                        
                        return latestByTime.Name;
                    }

                    // 2) Otherwise: all with Real-Time Protection activated, the strongest (highest state value)
                    var realtimeOn = products
                        .Where(p => (p.State & RealTimeProtectionFlag) == RealTimeProtectionFlag);
                    if (realtimeOn.Any())
                    {
                        var strongestRealtime = realtimeOn
                            .OrderByDescending(p => p.State)
                            .First();
                    
                        return strongestRealtime.Name;
                    }

                    // 3) Fallback: Product with the highest productState
                    var strongestOverall = products
                        .OrderByDescending(p => p.State)
                        .First();

                    return strongestOverall.Name;
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.OS.LatestAntivirusProduct", "Collect latest antivirus product", ex.ToString());
                    return "-";
                }
            }
            else // Not Windows, therefore no antivirus information available
            {
                return "-"; 
            }
        }

        public static string Antivirus_Products()
        {
            try
            {   // Create a list of JSON strings for each antivirus product
                List<string> antivirus_productsJsonList = new List<string>();

                // Get the antivirus products from the AntiVirusProduct class in the SecurityCenter2 namespace (Windows Security Center), which is available since Windows 10 version 1703 (Creators Update) and Windows Server 2016 (Windows 10 version 1607)
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT * FROM AntiVirusProduct"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        // Create antivirus product JSON object
                        Antivirus_Products antivirus_productsInfo = new Antivirus_Products
                        {
                            display_name = obj["displayName"]?.ToString() ?? "N/A",
                            instance_guid = obj["instanceGuid"]?.ToString() ?? "N/A",
                            path_to_signed_product_exe = obj["pathToSignedProductExe"]?.ToString() ?? "N/A",
                            path_to_signed_reporting_exe = obj["pathToSignedReportingExe"]?.ToString() ?? "N/A",
                            product_state = obj["productState"]?.ToString() ?? "N/A",
                            timestamp = obj["timestamp"]?.ToString() ?? "N/A",
                        };

                        // Serialize the antivirus product object into a JSON string and add it to the list
                        string network_adapterJson = JsonSerializer.Serialize(antivirus_productsInfo, new JsonSerializerOptions { WriteIndented = true });
                        antivirus_productsJsonList.Add(network_adapterJson);
                    }
                }

                // Return the list of antivirus products as a JSON array
                string antivirus_products_json = "[" + string.Join("," + Environment.NewLine, antivirus_productsJsonList) + "]";
                Logging.Device_Information("Device_Information.OS.Antivirus_Products", "antivirus_products_json", antivirus_products_json);
                return antivirus_products_json;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.OS.Antivirus_Products", "Collect antivirus products", ex.ToString());
                return "[]";
            }
        }

        public static string Antivirus_Information()
        {
            try
            {
                string antivirus_information_json = "{}";

                // Get the antivirus information from the AntiVirusProduct class in the SecurityCenter2 namespace (Windows Security Center), which is available since Windows 10 version 1703 (Creators Update) and Windows Server 2016 (Windows 10 version 1607)
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("Root\\Microsoft\\Windows\\Defender", "SELECT * FROM MSFT_MpComputerStatus"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        // Create antivirus information object
                        Antivirus_Information antivirus_information = new Antivirus_Information
                        {
                            amengineversion = obj["AMEngineVersion"]?.ToString() ?? "N/A",
                            amproductversion = obj["AMProductVersion"]?.ToString() ?? "N/A",
                            amserviceenabled = Convert.ToBoolean(obj["AMServiceEnabled"]),
                            amserviceversion = obj["AMServiceVersion"]?.ToString() ?? "N/A",
                            antispywareenabled = Convert.ToBoolean(obj["AntispywareEnabled"]),
                            antispywaresignaturelastupdated = obj["AntispywareSignatureLastUpdated"]?.ToString() ?? "N/A",
                            antispywaresignatureversion = obj["AntispywareSignatureVersion"]?.ToString() ?? "N/A",
                            antivirusenabled = Convert.ToBoolean(obj["AntivirusEnabled"]),
                            antivirussignaturelastupdated = obj["AntivirusSignatureLastUpdated"]?.ToString() ?? "N/A",
                            antivirussignatureversion = obj["AntivirusSignatureVersion"]?.ToString() ?? "N/A",
                            behaviormonitorenabled = Convert.ToBoolean(obj["BehaviorMonitorEnabled"]),
                            ioavprotectionenabled = Convert.ToBoolean(obj["IoavProtectionEnabled"]),
                            istamperprotected = Convert.ToBoolean(obj["IsTamperProtected"]),
                            nisenabled = Convert.ToBoolean(obj["NISEnabled"]),
                            nisengineversion = obj["NISEngineVersion"]?.ToString() ?? "N/A",
                            nissignaturelastupdated = obj["NISSignatureLastUpdated"]?.ToString() ?? "N/A",
                            nissignatureversion = obj["NISSignatureVersion"]?.ToString() ?? "N/A",
                            onaccessprotectionenabled = Convert.ToBoolean(obj["OnAccessProtectionEnabled"]),
                            realtimetprotectionenabled = Convert.ToBoolean(obj["RealTimeProtectionEnabled"]),
                        };
    
                        // Serialize the antivirus information object into a JSON string
                        antivirus_information_json = JsonSerializer.Serialize(antivirus_information, new JsonSerializerOptions { WriteIndented = true });
                        Logging.Device_Information("Device_Information.OS.Antivirus_Information", "antivirus_information_json", antivirus_information_json);
                    }
                }

                // Return the antivirus information as a JSON object
                return antivirus_information_json;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.OS.Antivirus_Information", "Collect antivirus information", ex.ToString());
                return "{}";
            }
        }

        public static string Get_Last_Boot_Time()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    string _last_boot = Windows.Helper.WMI.Search("root\\CIMV2", "SELECT LastBootUpTime FROM Win32_OperatingSystem", "LastBootUpTime");
                    DateTime last_boot_datetime = ManagementDateTimeConverter.ToDateTime(_last_boot);
                    return last_boot_datetime.ToString("dd.MM.yyyy HH:mm:ss");
                }
                else if (OperatingSystem.IsLinux())
                {
                    // Read uptime from /proc/uptime
                    string uptimeContent = File.ReadAllText("/proc/uptime");
                    string[] parts = uptimeContent.Split(' ');
                    double uptimeSeconds = double.Parse(parts[0]);

                    // Calculate the last boot time
                    DateTime lastBootTime = DateTime.Now.Subtract(TimeSpan.FromSeconds(uptimeSeconds));

                    // Format the last boot time
                    string formattedBootTime = lastBootTime.ToString("dd.MM.yyyy HH:mm:ss");

                    // Log and return the result
                    return formattedBootTime;
                }
                else if (OperatingSystem.IsMacOS())
                {
                    // Get the last boot time from the system
                    string rawBootTime = MacOS.Helper.Zsh.Execute_Script("Get_Last_Boot_Time", false, "sysctl kern.boottime");

                    // Extract the seconds part using Regex
                    var match = System.Text.RegularExpressions.Regex.Match(rawBootTime, @"sec\s*=\s*(\d+)");
                    if (!match.Success)
                    {
                        throw new FormatException("Could not extract the boot time from the system output.");
                    }

                    string lastBootTime = match.Groups[1].Value;

                    // Convert the last boot time to a DateTime object
                    DateTime lastBootDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    lastBootDateTime = lastBootDateTime.AddSeconds(Convert.ToDouble(lastBootTime));

                    // Format the last boot time
                    string formattedBootTime = lastBootDateTime.ToString("dd.MM.yyyy HH:mm:ss");

                    Logging.Error("Device_Information.MacOS.Get_Last_Boot_Time", "Last boot time", formattedBootTime);

                    // Log and return the result
                    return formattedBootTime;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.OS.Get_Last_Boot_Time", "Error retrieving last boot time", ex.ToString());
                return null;
            }
        }

        public static string Get_Last_Active_User()
        {
            try
            {
                string lastLoggedOnUser = "-";

                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        lastLoggedOnUser = UserSessionHelper.GetActiveUser();

                        if (string.IsNullOrEmpty(lastLoggedOnUser))
                            lastLoggedOnUser = "-";
                    }
                    catch (Exception ex)
                    {
                        lastLoggedOnUser = "-";
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    // Get the last active user from who command including ip
                    lastLoggedOnUser = Linux.Helper.Bash.Execute_Script("Get_Last_Active_User", false, "who | awk '{print $1, $5}' | head -n 1");
                }
                else if (OperatingSystem.IsMacOS())
                {
                    // Trim any whitespace and return the user
                    lastLoggedOnUser = MacOS.Helper.Zsh.Execute_Script("Get_Last_Active_User", false, "whoami");
                }

                return lastLoggedOnUser;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.OS.Get_Last_Active_User", "Error retrieving last active user", ex.ToString());
                return "-";
            }
        }
    }
}