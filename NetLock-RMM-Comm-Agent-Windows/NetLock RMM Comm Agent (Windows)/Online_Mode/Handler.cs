using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Reflection;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using NetLock_RMM_Comm_Agent_Windows.Helper;
using System.Management;
using System.Reflection.Emit;
using NetFwTypeLib;
using System.Diagnostics;
using NetLock_RMM_Comm_Agent_Windows.Device_Information;
using System.Data.SQLite;
using System.Runtime;

namespace NetLock_RMM_Comm_Agent_Windows.Online_Mode
{
    internal class Handler
    {
        //Class
        public class Device_Identity
        {
            public string agent_version { get; set; }
            public string package_guid { get; set; }
            public string device_name { get; set; }
            public string location_guid { get; set; }
            public string tenant_guid { get; set; }
            public string access_key { get; set; }
            public string hwid { get; set; }
            public string ip_address_internal { get; set; }
            public string operating_system { get; set; }
            public string domain { get; set; }
            public string antivirus_solution { get; set; }
            public string firewall_status { get; set; }
            public string architecture { get; set; }
            public string last_boot { get; set; }
            public string timezone { get; set; }
            public string cpu { get; set; }
            public string cpu_usage { get; set; }
            public string mainboard { get; set; }
            public string gpu { get; set; }
            public string ram { get; set; }
            public string ram_usage { get; set; }
            public string tpm { get; set; }
        }

        public class Processes
        {
            public string name { get; set; }
            public string pid { get; set; }
            public string parent_name { get; set; }
            public string parent_pid { get; set; }
            public string cpu { get; set; }
            public string ram { get; set; }
            public string user { get; set; }
            public string created { get; set; }
            public string path { get; set; }
            public string cmd { get; set; }
            public string handles { get; set; }
            public string threads { get; set; }
            public string read_operations { get; set; }
            public string read_transfer { get; set; }
            public string write_operations { get; set; }
            public string write_transfer { get; set; }
        }

        public class CPU_Information
        {
            public string name { get; set; }
            public string socket_designation { get; set; }
            public string processor_id { get; set; }
            public string revision { get; set; }
            public string usage { get; set; }
            public string voltage { get; set; }
            public string currentclockspeed { get; set; }
            public string processes { get; set; }
            public string threads { get; set; }
            public string handles { get; set; }
            public string maxclockspeed { get; set; }
            public string sockets { get; set; }
            public string cores { get; set; }
            public string logical_processors { get; set; }
            public string virtualization { get; set; }
            public string l1_cache { get; set; }
            public string l2_cache { get; set; }
            public string l3_cache { get; set; }
        }

        public class RAM_Information
        {
            public string name { get; set; }
            public string available { get; set; }
            public string assured { get; set; }
            public string cache { get; set; }
            public string outsourced_pool { get; set; }
            public string not_outsourced_pool { get; set; }
            public string speed { get; set; }
            public string slots { get; set; }
            public string slots_used { get; set; }
            public string form_factor { get; set; }
            public string hardware_reserved { get; set; }
        }

        public class Network_Adapters
        {
            public string name { get; set; }
            public string description { get; set; }
            public string manufacturer { get; set; }
            public string type { get; set; }
            public string link_speed { get; set; }
            public string service_name { get; set; }
            public string dns_domain { get; set; }
            public string dns_hostname { get; set; }
            public string dhcp_enabled { get; set; }
            public string dhcp_server { get; set; }
            public string ipv4_address { get; set; }
            public string ipv6_address { get; set; }
            public string subnet_mask { get; set; }
            public string mac_address { get; set; }
            public string sending { get; set; }
            public string receive { get; set; }
        }

        public class Disks
        {
            public string letter { get; set; }
            public string label { get; set; }
            public string model { get; set; }
            public string firmware_revision { get; set; }
            public string serial_number { get; set; }
            public string interface_type { get; set; }
            public string drive_type { get; set; }
            public string drive_format { get; set; }
            public string drive_ready { get; set; }
            public string capacity { get; set; }
            public string usage { get; set; }
            public string status { get; set; }
        }

        public class Applications_Installed
        {
            public string name { get; set; }
            public string version { get; set; }
            public string installed_date { get; set; }
            public string installation_path { get; set; }
            public string vendor { get; set; }
            public string uninstallation_string { get; set; }
        }

        public class Applications_Logon
        {
            public string name { get; set; }
            public string path { get; set; }
            public string command { get; set; }
            public string user { get; set; }
            public string user_sid { get; set; }
        }

        public class Applications_Scheduled_Tasks
        {
            public string name { get; set; }
            public string status { get; set; }
            public string author { get; set; }
            public string path { get; set; }
            public string folder { get; set; }
            public string user_sid { get; set; }
            public string next_execution { get; set; }
            public string last_execution { get; set; }
        }

        public class Applications_Services
        {
            public string display_name { get; set; }
            public string name { get; set; }
            public string status { get; set; }
            public string start_type { get; set; }
            public string login_as { get; set; }
            public string path { get; set; }
            public string description { get; set; }
        }

        public class Applications_Drivers
        {
            public string display_name { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public string status { get; set; }
            public string type { get; set; }
            public string start_type { get; set; }
            public string path { get; set; }
            public string version { get; set; }
        }

        public class Antivirus_Products
        {
            public string display_name { get; set; }
            public string instance_guid { get; set; }
            public string path_to_signed_product_exe { get; set; }
            public string path_to_signed_reporting_exe { get; set; }
            public string product_state { get; set; }
            public string timestamp { get; set; }
        }

        public class Antivirus_Information
        {
            public string amengineversion { get; set; }
            public string amproductversion { get; set; }
            public bool amserviceenabled { get; set; }
            public string amserviceversion { get; set; }
            public bool antispywareenabled { get; set; }
            public string antispywaresignaturelastupdated { get; set; }
            public string antispywaresignatureversion { get; set; }
            public bool antivirusenabled { get; set; }
            public string antivirussignaturelastupdated { get; set; }
            public string antivirussignatureversion { get; set; }
            public bool behaviormonitorenabled { get; set; }
            public bool ioavprotectionenabled { get; set; }
            public bool istamperprotected { get; set; }
            public bool nisenabled { get; set; }
            public string nisengineversion { get; set; }
            public string nissignaturelastupdated { get; set; }
            public string nissignatureversion { get; set; }
            public bool onaccessprotectionenabled { get; set; }
            public bool realtimetprotectionenabled { get; set; }
        }


        public static async Task<string> Authenticate()
        {
            try
            {
                // Get ip_address_internal
                Service.ip_address_internal = Helper.Network.Get_Local_IP_Address();
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "ip_address_internal", Service.ip_address_internal);

                // Get Windows version
                Service.operating_system = Windows.Windows_Version();
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "operating_system", Service.operating_system);

                // Get DOMAIN
                Service.domain = Environment.UserDomainName;
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "domain", Service.domain);

                // Get Antivirus solution
                Service.antivirus_solution = WMI.Search("root\\SecurityCenter2", "select * FROM AntivirusProduct", "displayName");
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "antivirus_solution", Service.antivirus_solution);

                // Get Firewall status
                Service.firewall_status = Microsoft_Defender_Firewall.Handler.Status();
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "firewall_status", Service.firewall_status.ToString());

                // Get Architecture
                Service.architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "architecture", Service.architecture);

                // Get last boot
                string _last_boot = WMI.Search("root\\CIMV2", "SELECT LastBootUpTime FROM Win32_OperatingSystem", "LastBootUpTime");
                DateTime last_boot_datetime = ManagementDateTimeConverter.ToDateTime(_last_boot);
                Service.last_boot = last_boot_datetime.ToString("dd.MM.yyyy HH:mm:ss");
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "last_boot", Service.last_boot);

                // Get timezone
                Service.timezone = Globalization.Local_Time_Zone();
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "timezone", Service.timezone);

                // Get CPU
                Service.cpu = WMI.Search("root\\CIMV2", "SELECT Name FROM Win32_Processor", "Name");
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "cpu", Service.cpu);

                // Get CPU usage
                Service.cpu_usage = Hardware.CPU_Usage();
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "cpu_usage", Service.cpu_usage);

                // Get Mainboard
                string _mainboard = WMI.Search("root\\CIMV2", "SELECT Product FROM Win32_BaseBoard", "Product");
                string mainboard_manufacturer = WMI.Search("root\\CIMV2", "SELECT Manufacturer FROM Win32_BaseBoard", "Manufacturer");

                Service.mainboard = _mainboard + " (" + mainboard_manufacturer + ")";
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "mainboard", Service.mainboard);

                // Get GPU
                Service.gpu = WMI.Search("root\\CIMV2", "SELECT Name FROM Win32_VideoController", "Name");
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "gpu", Service.gpu);

                // Get RAM
                string _ram = WMI.Search("root\\CIMV2", "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem", "TotalPhysicalMemory");
                Service.ram = Math.Round(Convert.ToDouble(_ram) / 1024 / 1024 / 1024).ToString();
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "ram", Service.ram);

                // Get RAM usage
                Service.ram_usage = Hardware.RAM_Usage();
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "ram_usage", Service.ram_usage);

                // Get TPM
                //string tpm_IsActivated_InitialValue = WMI.Search("root\\CIMV2", "SELECT IsActivated_InitialValue FROM Win32_Tpm", "IsActivated_InitialValue");
                Service.tpm = WMI.Search("root\\cimv2\\Security\\MicrosoftTpm", "SELECT IsEnabled_InitialValue FROM Win32_Tpm", "IsEnabled_InitialValue");
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "tpm_IsEnabled_InitialValue", Service.tpm);
                //string tpm_IsOwned_InitialValue = WMI.Search("root\\CIMV2", "SELECT IsOwned_InitialValue FROM Win32_Tpm", "IsOwned_InitialValue");
                //string tpm_manufacturer = WMI.Search("root\\CIMV2", "SELECT Manufacturer FROM Win32_Tpm", "Manufacturer");
                //string tpm_ManufacturerId = WMI.Search("root\\CIMV2", "SELECT ManufacturerId FROM Win32_Tpm", "ManufacturerId");
                //string tpm_ManufacturerIdTxt = WMI.Search("root\\CIMV2", "SELECT ManufacturerIdTxt FROM Win32_Tpm", "ManufacturerIdTxt");
                //string tpm_ManufacturerVersion = WMI.Search("root\\CIMV2", "SELECT ManufacturerVersion FROM Win32_Tpm", "ManufacturerVersion");
                //string tpm_ManufacturerVersionFull20 = WMI.Search("root\\CIMV2", "SELECT ManufacturerVersionFull20 FROM Win32_Tpm", "ManufacturerVersionFull20");
                //string tpm_ManufacturerVersionInfo = WMI.Search("root\\CIMV2", "SELECT ManufacturerVersionInfo FROM Win32_Tpm", "ManufacturerVersionInfo");
                //string tpm_PhysicalPresenceVersionInfo = WMI.Search("root\\CIMV2", "SELECT PhysicalPresenceVersionInfo FROM Win32_Tpm", "PhysicalPresenceVersionInfo");
                //string tpm_SpecVersion = WMI.Search("root\\CIMV2", "SELECT SpecVersion FROM Win32_Tpm", "SpecVersion");

                //tpm data to much for a one liner. Needs own table in web console and therefore a own json object

                //Create JSON
                Device_Identity identity = new Device_Identity
                {
                    agent_version = Application_Settings.version,
                    package_guid = Service.package_guid,
                    device_name = Service.device_name,
                    location_guid = Service.location_guid,
                    tenant_guid = Service.tenant_guid,
                    access_key = Service.access_key,
                    hwid = Service.hwid,
                    ip_address_internal = Service.ip_address_internal,
                    operating_system = Service.operating_system,
                    domain = Service.domain,
                    antivirus_solution = Service.antivirus_solution,
                    firewall_status = Service.firewall_status.ToString(),
                    architecture = Service.architecture,
                    last_boot = Service.last_boot,
                    timezone = Service.timezone,
                    cpu = Service.cpu,
                    cpu_usage = Service.cpu_usage,
                    mainboard = Service.mainboard,
                    gpu = Service.gpu,
                    ram = Service.ram,
                    ram_usage = Service.ram_usage,
                    tpm = Service.tpm,
                };

                // Create the object that contains the device_identity object
                var jsonObject = new { device_identity = identity };

                // Serialize the object to a JSON string
                string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "json", json);

                // Declare public
                Service.device_identity_json = json;

                // Create a HttpClient instance
                using (var httpClient = new HttpClient())
                {
                    // Set the content type header
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Package_Guid", Service.package_guid);

                    Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "communication_server", Service.http_https + Service.communication_server + "/Agent/Windows/Verify_Device");

                    // Send the JSON data to the server
                    var response = await httpClient.PostAsync(Service.http_https + Service.communication_server + "/Agent/Windows/Verify_Device", new StringContent(json, Encoding.UTF8, "application/json"));

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, handle the response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "result", result);

                        // Parse the JSON response
                        if (result == "authorized" || result == "synced" || result == "not_synced")
                        {
                            if (!Service.authorized)
                            {
                                // Write the new authorization status to the server config JSON
                                var new_server_config = new
                                {
                                    ssl = Service.ssl,
                                    package_guid = Service.package_guid,
                                    communication_servers = Service.communication_servers,
                                    remote_servers = Service.remote_servers,
                                    update_servers = Service.update_servers,
                                    trust_servers = Service.trust_servers,
                                    file_servers = Service.file_servers,
                                    tenant_guid = Service.tenant_guid,
                                    location_guid = Service.location_guid,
                                    language = Service.language,
                                    access_key = Service.access_key,
                                    authorized = true,
                                };

                                string new_server_config_json = JsonSerializer.Serialize(new_server_config, new JsonSerializerOptions { WriteIndented = true });

                                // Write the new server config JSON to the file
                                File.WriteAllText(Application_Paths.program_data_server_config_json, new_server_config_json);

                                Service.authorized = true;
                            }

                            Service.sync_timer.Interval = 600000; // change to 10 minutes
                            Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "sync_timer.Interval", Service.sync_timer.Interval.ToString());
                        }
                        else if (result == "unauthorized")
                        {
                            if (Service.authorized)
                            {
                                // Write the new authorization status to the server config JSON
                                var new_server_config = new
                                {
                                    ssl = Service.ssl,
                                    package_guid = Service.package_guid,
                                    communication_servers = Service.communication_servers,
                                    remote_servers = Service.remote_servers,
                                    update_servers = Service.update_servers,
                                    trust_servers = Service.trust_servers,
                                    file_servers = Service.file_servers,
                                    tenant_guid = Service.tenant_guid,
                                    location_guid = Service.location_guid,
                                    language = Service.language,
                                    access_key = Service.access_key,
                                    authorized = false,
                                };

                                string new_server_config_json = JsonSerializer.Serialize(new_server_config, new JsonSerializerOptions { WriteIndented = true });

                                // Write the new server config JSON to the file
                                File.WriteAllText(Application_Paths.program_data_server_config_json, new_server_config_json);

                                Service.authorized = false;
                            }

                            Service.sync_timer.Interval = 30000; // change to 30 seconds
                            Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "sync_timer.Interval", Service.sync_timer.Interval.ToString());
                        }

                        return result;
                    }
                    else
                    {
                        // Request failed, handle the error
                        Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "request", "Request failed: " + response.Content);
                        return "invalid";
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Online_Mode.Handler.Authenticate", "General error", ex.ToString());
                return "invalid";
            }
        }

        public static async Task<string> Update_Device_Information()
        {
            try
            {
                // Create the device_identity object
                Device_Identity identity = new Device_Identity
                {
                    agent_version = Application_Settings.version,
                    package_guid = Service.package_guid,
                    device_name = Service.device_name,
                    location_guid = Service.location_guid,
                    tenant_guid = Service.tenant_guid,
                    access_key = Service.access_key,
                    hwid = Service.hwid,
                    ip_address_internal = Service.ip_address_internal,
                    operating_system = Service.operating_system,
                    domain = Service.domain,
                    antivirus_solution = Service.antivirus_solution,
                    firewall_status = Service.firewall_status.ToString(),
                    architecture = Service.architecture,
                    last_boot = Service.last_boot,
                    timezone = Service.timezone,
                    cpu = Service.cpu,
                    cpu_usage = Service.cpu_usage,
                    mainboard = Service.mainboard,
                    gpu = Service.gpu,
                    ram = Service.ram,
                    ram_usage = Service.ram_usage,
                    tpm = Service.tpm,
                };

                // Get the processes list
                string processes_json = Device_Information.Processes.Collect();

                // Create the data for "cpu_information"
                string cpu_information_json = Device_Information.Hardware.CPU_Information();

                string ram_information_json = Device_Information.Hardware.RAM_Information();

                string network_adapters_json = Device_Information.Network.Network_Adapter_Information();

                string disks_json = Device_Information.Hardware.Disks();

                string antivirus_products_json = Device_Information.Windows.Antivirus_Products();

                string applications_installed_json = Device_Information.Software.Applications_Installed();

                string applications_logon_json = Device_Information.Software.Applications_Logon();

                string applications_scheduled_tasks_json = Device_Information.Software.Applications_Scheduled_Tasks();

                string applications_services_json = Device_Information.Software.Applications_Services();

                string applications_drivers_json = Device_Information.Software.Applications_Drivers();

                string antivirus_information_json = Device_Information.Windows.Antivirus_Information();

                // Erstelle das JSON-Objekt
                var jsonObject = new
                {
                    device_identity = identity,
                    processes = processes_json,
                    cpu_information = cpu_information_json,
                    ram_information = ram_information_json,
                    network_adapters = network_adapters_json,
                    disks = disks_json,
                    applications_installed = applications_installed_json,
                    applications_logon = applications_logon_json,
                    applications_scheduled_tasks = applications_scheduled_tasks_json,
                    applications_services = applications_services_json,
                    applications_drivers = applications_drivers_json,
                    antivirus_products = antivirus_products_json,
                    antivirus_information = antivirus_information_json,
                };

                // Konvertiere das Objekt in ein JSON-String
                string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                Logging.Handler.Debug("Online_Mode.Handler.Update_Device_Information", "json", json);

                // Create a HttpClient instance
                using (var httpClient = new HttpClient())
                {
                    // Set the content type header
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Package_Guid", Service.package_guid);

                    Logging.Handler.Debug("Online_Mode.Handler.Update_Device_Information", "communication_server", Service.http_https + Service.communication_server + "/Agent/Windows/Update_Device_Information");

                    // Send the JSON data to the server
                    var response = await httpClient.PostAsync(Service.http_https + Service.communication_server + "/Agent/Windows/Update_Device_Information", new StringContent(json, Encoding.UTF8, "application/json"));

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, handle the response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Handler.Debug("Online_Mode.Handler.Update_Device_Information", "result", result);

                        // Parse the JSON response
                        if (result == "authorized" || result == "synced" || result == "not_synced")
                        {
                            if (!Service.authorized)
                            {
                                // Write the new authorization status to the server config JSON
                                var new_server_config = new
                                {
                                    ssl = Service.ssl,
                                    package_guid = Service.package_guid,
                                    communication_servers = Service.communication_servers,
                                    remote_servers = Service.remote_servers,
                                    update_servers = Service.update_servers,
                                    trust_servers = Service.trust_servers,
                                    file_servers = Service.file_servers,
                                    tenant_guid = Service.tenant_guid,
                                    location_guid = Service.location_guid,
                                    language = Service.language,
                                    access_key = Service.access_key,
                                    authorized = true,
                                };

                                string new_server_config_json = JsonSerializer.Serialize(new_server_config, new JsonSerializerOptions { WriteIndented = true });

                                // Write the new server config JSON to the file
                                File.WriteAllText(Application_Paths.program_data_server_config_json, new_server_config_json);

                                Service.authorized = true;
                            }
                        }
                        else if (result == "unauthorized")
                        {
                            if (Service.authorized)
                            {
                                // Write the new authorization status to the server config JSON
                                var new_server_config = new
                                {
                                    ssl = Service.ssl,
                                    package_guid = Service.package_guid,
                                    communication_servers = Service.communication_servers,
                                    remote_servers = Service.remote_servers,
                                    update_servers = Service.update_servers,
                                    trust_servers = Service.trust_servers,
                                    file_servers = Service.file_servers,
                                    tenant_guid = Service.tenant_guid,
                                    location_guid = Service.location_guid,
                                    language = Service.language,
                                    access_key = Service.access_key,
                                    authorized = false,
                                };

                                string new_server_config_json = JsonSerializer.Serialize(new_server_config, new JsonSerializerOptions { WriteIndented = true });

                                // Write the new server config JSON to the file
                                File.WriteAllText(Application_Paths.program_data_server_config_json, new_server_config_json);

                                Service.authorized = false;
                            }
                        }

                        return result;
                    }
                    else
                    {
                        // Request failed, handle the error
                        Logging.Handler.Debug("Online_Mode.Handler.Update_Device_Information", "request", "Request failed: " + response.StatusCode + " " + response.Content.ToString());
                        return "invalid";
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Online_Mode.Handler.Update_Device_Information", "General error", ex.ToString());
                return "invalid";
            }
        }

        public static async Task<string> Policy()
        {
            try
            {
                //Create JSON
                Device_Identity identity = new Device_Identity
                {
                    agent_version = Application_Settings.version,
                    package_guid = Service.package_guid,
                    device_name = Service.device_name,
                    location_guid = Service.location_guid,
                    tenant_guid = Service.tenant_guid,
                    access_key = Service.access_key,
                    hwid = Service.hwid,
                    ip_address_internal = Service.ip_address_internal,
                    operating_system = Service.operating_system,
                    domain = Service.domain,
                    antivirus_solution = Service.antivirus_solution,
                    firewall_status = Service.firewall_status.ToString(),
                    architecture = Service.architecture,
                    last_boot = Service.last_boot,
                    timezone = Service.timezone,
                    cpu = Service.cpu,
                    cpu_usage = Service.cpu_usage,
                    mainboard = Service.mainboard,
                    gpu = Service.gpu,
                    ram = Service.ram,
                    ram_usage = Service.ram_usage,
                    tpm = Service.tpm,
                };

                // Create the object that contains the device_identity object
                var jsonObject = new { device_identity = identity };

                // Serialize the object to a JSON string
                string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                Logging.Handler.Debug("Online_Mode.Handler.Policy", "json", json);

                // Create a HttpClient instance
                using (var httpClient = new HttpClient())
                {
                    // Set the content type header
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Package_Guid", Service.package_guid);

                    Logging.Handler.Debug("Online_Mode.Handler.Policy", "communication_server", Service.http_https + Service.communication_server + "/Agent/Windows/Policy");

                    // Send the JSON data to the server
                    var response = await httpClient.PostAsync(Service.http_https + Service.communication_server + "/Agent/Windows/Policy", new StringContent(json, Encoding.UTF8, "application/json"));

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, handle the response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Handler.Debug("Online_Mode.Handler.Policy", "result", result);

                        if (result == "unauthorized")
                        {
                            if (Service.authorized)
                            {
                                // Write the new authorization status to the server config JSON
                                var new_server_config = new
                                {
                                    ssl = Service.ssl,
                                    package_guid = Service.package_guid,
                                    communication_servers = Service.communication_servers,
                                    remote_servers = Service.remote_servers,
                                    update_servers = Service.update_servers,
                                    trust_servers = Service.trust_servers,
                                    file_servers = Service.file_servers,
                                    tenant_guid = Service.tenant_guid,
                                    location_guid = Service.location_guid,
                                    language = Service.language,
                                    access_key = Service.access_key,
                                    authorized = false,
                                };

                                string new_server_config_json = JsonSerializer.Serialize(new_server_config, new JsonSerializerOptions { WriteIndented = true });

                                // Write the new server config JSON to the file
                                File.WriteAllText(Application_Paths.program_data_server_config_json, new_server_config_json);

                                Service.authorized = false;
                            }
                        }
                        
                        if (result != "no_assigned_policy_found")
                        {
                            // Deserialization of the entire JSON string
                            using (JsonDocument document = JsonDocument.Parse(result))
                            {
                                JsonElement policy_antivirus_settings_element = document.RootElement.GetProperty("antivirus_settings_json");
                                Service.policy_antivirus_settings_json = policy_antivirus_settings_element.ToString();

                                JsonElement policy_antivirus_exclusions_element = document.RootElement.GetProperty("antivirus_exclusions_json");
                                Service.policy_antivirus_exclusions_json = policy_antivirus_exclusions_element.ToString();

                                JsonElement policy_antivirus_scan_jobs_element = document.RootElement.GetProperty("antivirus_scan_jobs_json");
                                Service.policy_antivirus_scan_jobs_json = policy_antivirus_scan_jobs_element.ToString();

                                JsonElement policy_antivirus_controlled_folder_access_folders_element = document.RootElement.GetProperty("antivirus_controlled_folder_access_folders_json");
                                Service.policy_antivirus_controlled_folder_access_folders_json = policy_antivirus_controlled_folder_access_folders_element.ToString();

                                JsonElement policy_antivirus_controlled_folder_access_ruleset_element = document.RootElement.GetProperty("antivirus_controlled_folder_access_ruleset_json");
                                Service.policy_antivirus_controlled_folder_access_ruleset_json = policy_antivirus_controlled_folder_access_ruleset_element.ToString();

                                JsonElement policy_sensors_json_element = document.RootElement.GetProperty("policy_sensors_json");
                                Service.policy_sensors_json = policy_sensors_json_element.ToString();

                                JsonElement policy_jobs_json_element = document.RootElement.GetProperty("policy_jobs_json");
                                Service.policy_jobs_json = policy_jobs_json_element.ToString();
                            }

                            // Insert into policy database
                            Initialization.Database.NetLock_Data_Setup();

                            Logging.Handler.Debug("Online_Mode.Handler.Policy", "Insert into policy database", "Starting...");

                            using (SQLiteConnection db_conn = new SQLiteConnection(Application_Settings.NetLock_Data_Database_String)) // Remove old policy and insert new policy
                            {
                                db_conn.Open();

                                SQLiteCommand command = new SQLiteCommand(@"DELETE FROM policy; " +
                                "INSERT INTO policy (" +
                                "'antivirus_settings_json', " +
                                "'antivirus_exclusions_json', " +
                                "'antivirus_scan_jobs_json', " +
                                "'antivirus_controlled_folder_access_folders_json', " +
                                "'antivirus_controlled_folder_access_ruleset_json', " +
                                "'sensors_json', " +
                                "'jobs_json'" +

                                ") VALUES (" +

                                "'" + Service.policy_antivirus_settings_json + "', " + //policy_antivirus_settings_json
                                "'" + Service.policy_antivirus_exclusions_json + "'," + //policy_antivirus_exclusions_json
                                "'" + Service.policy_antivirus_scan_jobs_json + "'," + //policy_antivirus_scan_jobs_json
                                "'" + Service.policy_antivirus_controlled_folder_access_folders_json + "'," + //policy_antivirus_controlled_folder_access_folders_json
                                "'" + Service.policy_antivirus_controlled_folder_access_ruleset_json + "'," + //policy_antivirus_controlled_folder_access_ruleset_json
                                "'" + Service.policy_sensors_json + "'," + //policy_sensors_json
                                "'" + Service.policy_jobs_json + "'" + //policy_jobs_json

                                ");"
                                , db_conn);

                                command.ExecuteNonQuery();

                                db_conn.Close();
                                db_conn.Dispose();

                                Logging.Handler.Debug("Online_Mode.Handler.Policy", "Insert into policy database", "Done." + Environment.NewLine);
                            }
                        }
                        else if (result == "no_assigned_policy_found" || result == "unauthorized") // Remove old policy
                        {
                            Logging.Handler.Debug("Online_Mode.Handler.Policy", "Insert into policy database", "Starting...");

                            using (SQLiteConnection db_conn = new SQLiteConnection(Application_Settings.NetLock_Data_Database_String))
                            {
                                db_conn.Open();

                                SQLiteCommand command = new SQLiteCommand("DELETE FROM policy;", db_conn);

                                command.ExecuteNonQuery();

                                db_conn.Close();
                                db_conn.Dispose();

                                Logging.Handler.Debug("Online_Mode.Handler.Policy", "Insert into policy database", "Done." + Environment.NewLine);
                            }
                        }

                        return "ok";
                    }
                    else
                    {
                        // Request failed, handle the error
                        Logging.Handler.Debug("Online_Mode.Handler.Policy", "request", "Request failed: " + response.Content);
                        return "invalid";
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Online_Mode.Handler.Policy", "General error", ex.ToString());
                return "invalid";
            }
        }
    }
}
