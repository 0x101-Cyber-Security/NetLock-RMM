using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Security.Principal;
using System.Data.SQLite;

namespace NetLock_RMM_Comm_Agent_Windows.Initialization
{
    internal class Server_Config
    {
        public async static Task<bool> Load()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);

                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    // Get ssl as boolean
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("ssl");
                        Service.ssl = element.GetBoolean();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (ssl)", Service.ssl.ToString());

                        // if ssl http_https
                        if (Service.ssl)
                        {
                            Service.http_https = "https://";
                        }
                        else
                        {
                            Service.http_https = "http://";
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (ssl) - Parsing", ex.ToString());
                    }

                    // Get package_guid
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("package_guid");
                        Service.package_guid = element.ToString();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (package_guid)", Service.package_guid);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (package_guid) - Parsing", ex.ToString());
                    }

                    // Get the communication servers
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("communication_servers");
                        Service.communication_servers = element.ToString();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (communication_servers)", Service.communication_servers);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (communication_servers) - Parsing", ex.ToString());
                    }
                    
                    // Get the remote servers
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("remote_servers");
                        Service.remote_servers = element.ToString();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (remote_servers)", Service.remote_servers);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (remote_servers) - Parsing", ex.ToString());
                    }
                    
                    // Get the update servers
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("update_servers");
                        Service.update_servers = element.ToString();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (update_servers)", Service.update_servers);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (update_servers) - Parsing", ex.ToString());
                    }

                    // Get the main trust servers
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("trust_servers");
                        Service.trust_servers = element.ToString();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (trust_servers)", Service.trust_servers);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (trust_servers) - Parsing", ex.ToString());
                    }

                    // Get the main file servers
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("file_servers");
                        Service.file_servers = element.ToString();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (file_servers)", Service.file_servers);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (file_servers) - Parsing", ex.ToString());
                    }

                    // Get the tenant guid
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("tenant_guid");
                        Service.tenant_guid = element.ToString();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (tenant_guid)", Service.tenant_guid);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (tenant_guid) - Parsing", ex.ToString());
                    }

                    // Get the location guid
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("location_guid");
                        Service.location_guid = element.ToString();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (location_guid)", Service.location_guid);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (location_guid) - Parsing", ex.ToString());
                    }

                    // Get language
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("language");
                        Service.language = element.ToString();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (language)", Service.language);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (language) - Parsing", ex.ToString());
                    }

                    // Get the access key
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("access_key");
                        Service.access_key = element.ToString();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (access_key)", Service.access_key);
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (access_key) - Parsing", ex.ToString());
                    }

                    // Get the authorized status
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("authorized");
                        Service.authorized = element.GetBoolean();
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load (authorized)", Service.authorized.ToString());
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Server_Config_Handler", "Server_Config_Handler.Load (authorized) - Parsing", ex.ToString());
                    }

                    // Check if the access key is valid
                    if (Service.access_key == String.Empty)
                    {
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load", "Access key is empty");

                        // Generate a new access key
                        Service.access_key = await Randomizer.Handler.Generate_Access_Key(32);

                        // Write the new access key to the server config file
                        // Create the JSON object
                        var jsonObject = new
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

                        // Convert the object into a JSON string
                        string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                        Logging.Handler.Debug("Online_Mode.Handler.Update_Device_Information", "json", json);

                        // Write the new server config JSON to the file
                        File.WriteAllText(Application_Paths.program_data_server_config_json, json);
                    }
                    else
                    {
                        Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load", "Access key is not empty");
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Debug("Server_Config_Handler", "Server_Config_Handler.Load", ex.ToString());
                return false;
            }
        }
    }
}
