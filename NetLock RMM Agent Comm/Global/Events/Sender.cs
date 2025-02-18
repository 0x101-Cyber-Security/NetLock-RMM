using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Global.Helper;
using System.Management;
using System.Net.Http.Headers;
using System.IO;
using System.Security.Cryptography;
using NetLock_RMM_Agent_Comm;
using System.Text.Json;

namespace Global.Events
{
    internal class Sender
    {
        public class Device_Identity
        {
            public string agent_version { get; set; }
            public string package_guid { get; set; }
            public string device_name { get; set; }
            public string location_guid { get; set; }
            public string tenant_guid { get; set; }
            public string access_key { get; set; }
            public string hwid { get; set; }
            public string platform { get; set; }
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

        public class Event_Entity
        {
            public string severity { get; set; }
            public string reported_by { get; set; }
            public string _event { get; set; }
            public string description { get; set; }
            public string notification_json { get; set;}
            public string type { get; set; }
            public string language { get; set; }
        }

        public static async Task <bool> Send_Event(string severity, string reported_by, string _event, string description, string notification_json, string type, string language)
        {
            try
            {
                // Create the identity_object
                Device_Identity identity_object = new Device_Identity
                {
                    agent_version = Application_Settings.version,
                    package_guid = Configuration.Agent.package_guid,
                    device_name = Configuration.Agent.device_name,
                    location_guid = Configuration.Agent.location_guid,
                    tenant_guid = Configuration.Agent.tenant_guid,
                    access_key = Device_Worker.access_key,
                    hwid = Configuration.Agent.hwid,
                    platform = Configuration.Agent.platform,
                    ip_address_internal = Device_Worker.ip_address_internal,
                    operating_system = Device_Worker.operating_system,
                    domain = Device_Worker.domain,
                    antivirus_solution = Device_Worker.antivirus_solution,
                    firewall_status = Device_Worker.firewall_status.ToString(),
                    architecture = Device_Worker.architecture,
                    last_boot = Device_Worker.last_boot,
                    timezone = Device_Worker.timezone,
                    cpu = Device_Worker.cpu,
                    cpu_usage = Device_Worker.cpu_usage,
                    mainboard = Device_Worker.mainboard,
                    gpu = Device_Worker.gpu,
                    ram = Device_Worker.ram,
                    ram_usage = Device_Worker.ram_usage,
                    tpm = Device_Worker.tpm,
                };

                // Create the event_object
                Event_Entity event_object = new Event_Entity
                {
                    severity = severity,
                    reported_by = reported_by,
                    _event = _event,
                    description = description,
                    notification_json = notification_json,
                    type = type,
                    language = language,
                };

                // Create the JSON object
                var jsonObject = new
                {
                    device_identity = identity_object,
                    _event = event_object,
                };

                // Convert the object into a JSON string
                string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                Logging.Debug("Events.Sender.Send_Event", "json", json);

                // Create a HttpClient instance
                using (var httpClient = new HttpClient())
                {
                    // Set the content type header
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Package-Guid", Configuration.Agent.package_guid);

                    Logging.Debug("Events.Sender.Send_Event", "communication_server", Configuration.Agent.http_https + Device_Worker.communication_server + "/Agent/Windows/Events");

                    // Send the JSON data to the server
                    var response = await httpClient.PostAsync(Configuration.Agent.http_https + Device_Worker.communication_server + "/Agent/Windows/Events", new StringContent(json, Encoding.UTF8, "application/json"));

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, handle the response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Debug("Events.Sender.Send_Event", "result", result);

                        // Parse the JSON response
                        if (result == "unauthorized")
                        {
                            if (Device_Worker.authorized)
                            {
                                // Write the new authorization status to the server config JSON
                                string new_server_config_json = JsonSerializer.Serialize(new
                                {
                                    ssl = Configuration.Agent.ssl,
                                    package_guid = Configuration.Agent.package_guid,
                                    communication_servers = Configuration.Agent.communication_servers,
                                    remote_servers = Configuration.Agent.remote_servers,
                                    update_servers = Configuration.Agent.update_servers,
                                    trust_servers = Configuration.Agent.trust_servers,
                                    file_servers = Configuration.Agent.file_servers,
                                    tenant_guid = Configuration.Agent.tenant_guid,
                                    location_guid = Configuration.Agent.location_guid,
                                    access_key = Device_Worker.access_key,
                                    authorized = false,
                                }, new JsonSerializerOptions { WriteIndented = true });

                                // Write the new server config JSON to the file
                                File.WriteAllText(Application_Paths.program_data_server_config_json, new_server_config_json);

                                Device_Worker.authorized = false;
                            }
                        }
                        else if (result == "success")
                            return true;
                        else
                        {
                            // Request failed, handle the error
                            Logging.Debug("Events.Sender.Send_Event", "request", "Request failed, result was not success: " + result);
                            return false;
                        }
                    }
                    else
                    {
                        // Request failed, handle the error
                        Logging.Debug("Events.Sender.Send_Event", "request", "Request failed: " + response.StatusCode + " " + response.Content.ToString());
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.Error("Events.Sender.Send_Event", "General error", ex.ToString());
                return false;
            }
        }
    }
}
