using NetLock_RMM_Comm_Agent_Windows.Helper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static NetLock_RMM_Comm_Agent_Windows.Online_Mode.Handler;
using Helper;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace NetLock_RMM_Comm_Agent_Windows.Initialization
{
    internal class Version
    {
        public static async Task<bool> Check_Version()
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
                    ip_address_internal = string.Empty,
                    operating_system = string.Empty,
                    domain = string.Empty,
                    antivirus_solution = string.Empty,
                    firewall_status = string.Empty,
                    architecture = string.Empty,
                    last_boot = string.Empty,
                    timezone = string.Empty,
                    cpu = string.Empty,
                    mainboard = string.Empty,
                    gpu = string.Empty,
                    ram = string.Empty,
                    tpm = string.Empty,
                };

                // Create the object that contains the device_identity object
                var jsonObject = new { device_identity = identity };

                // Serialize the object to a JSON string
                string json = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
                Logging.Handler.Debug("Initialization.Version_Handler.Check_Version", "json", json);

                // Create a HttpClient instance
                using (var httpClient = new HttpClient())
                {
                    // Set the content type header
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Package_Guid", Service.package_guid);

                    Logging.Handler.Debug("Initialization.Version_Handler.Check_Version", "communication_server", Service.communication_server + "/Agent/Windows/Verify_Device");

                    // Send the JSON data to the server
                    var response = await httpClient.PostAsync(Service.http_https + Service.communication_server + "/Agent/Windows/Check_Version", new StringContent(json, Encoding.UTF8, "application/json"));

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, handle the response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Handler.Debug("Initialization.Version_Handler.Check_Version", "result", result);

                        // Parse the JSON response
                        if (result == "identical")
                        {
                            Logging.Handler.Debug("Initialization.Version_Handler.Check_Version", "up2date?", "true");
                            return true;
                        }
                        else if (result == "different")
                        {
                            Logging.Handler.Debug("Initialization.Version_Handler.Check_Version", "up2date?", "false");
                            return false;
                        }
                        else //something mostelikely went wrong. Returning true to avoid any issues
                        {
                            Logging.Handler.Debug("Initialization.Version_Handler.Check_Version", "Something mostelikely went wrong. Returning true to avoid any issues", "true");
                            return true;
                        }
                    }
                    else
                    {
                        // Request failed, handle the error
                        Logging.Handler.Debug("Initialization.Version_Handler.Check_Version", "request", "Request failed: " + response.Content);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Initialization.Version_Handler.Check_Version", "General error", ex.ToString());
                return true;
            }
        }

        public static async Task Update()
        {
            try
            {
                // Delete the temp directory
                if (Directory.Exists(Application_Paths.c_temp_netlock_dir))
                    Directory.Delete(Application_Paths.c_temp_netlock_dir, true);

                // Create the temp directory
                if (!Directory.Exists(Application_Paths.c_temp_netlock_dir))
                    Directory.CreateDirectory(Application_Paths.c_temp_netlock_dir);

                // Download the new version
                Logging.Handler.Debug("Initialization.Version_Handler.Update", "Downloading new version", "true");
                await Http.DownloadFileAsync(Service.ssl, Service.update_server + Application_Paths.installer_package_url, Application_Paths.c_temp_netlock_dir + Application_Paths.installer_package_path, Service.package_guid);

                // Get the hash of the new version
                Logging.Handler.Debug("Initialization.Version_Handler.Update", "Getting hash of new version", "true");
                string hash = await Http.GetHashAsync(Service.ssl, Service.update_server + Application_Paths.installer_package_url + ".sha512", Service.package_guid);

                // Get the hash of the downloaded file
                Logging.Handler.Debug("Initialization.Version_Handler.Update", "Getting hash of downloaded file", "true");
                string downloaded_hash = IO.Get_SHA512(Application_Paths.c_temp_netlock_dir + Application_Paths.installer_package_path);

                // Compare the hashes
                if (hash != downloaded_hash)
                {
                    Logging.Handler.Debug("Initialization.Version_Handler.Update", "Hash check", "Bad. Canceling update.");
                    return;
                }
                else
                    Logging.Handler.Debug("Initialization.Version_Handler.Update", "Hash check", "Good");

                // Extract the new version
                Logging.Handler.Debug("Initialization.Version_Handler.Update", "Extracting new version", "true");
                ZipFile.ExtractToDirectory(Application_Paths.c_temp_netlock_dir + Application_Paths.installer_package_path, Application_Paths.c_temp_installer_dir);

                // Start the installer
                Logging.Handler.Debug("Initialization.Version_Handler.Update", "Starting installer", "true");
                Process.Start(Application_Paths.c_temp_installer_path, $"fix \"{Application_Paths.program_data_server_config_json}\"");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Initialization.Version_Handler.Update", "General error", ex.ToString());
            }
        }
    }
}
