using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Security.Principal;
using System.ComponentModel;

namespace NetLock_RMM_Health_Agent_Windows
{
    internal class Server_Config
    {
        public static bool Load()
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

                        if (Service.ssl)
                            Service.http_https = "https://";
                        else
                            Service.http_https = "http://";
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

                    // Get the trust servers
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
