﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Security.Principal;
using Global.Helper;
using NetLock_RMM_Agent_Comm;
using System.ComponentModel;
using System.Runtime.Intrinsics.Wasm;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Global.Initialization
{
    internal class Server_Config
    {
        public static string Ssl()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);

                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("ssl");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (ssl)", element.GetBoolean().ToString());

                    // if ssl http_https
                    if (element.GetBoolean())
                    {
                        return true.ToString();
                    }
                    else
                    {
                        return false.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (ssl)", ex.ToString());
                return false.ToString();
            }
        }

        public static string Package_Guid()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);

                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("package_guid");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (package_guid)", element.ToString());

                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (ssl)", ex.ToString());
                return false.ToString();
            }
        }

        public static string Communication_Servers()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("communication_servers");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (communication_servers)", element.ToString());
                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (communication_servers)", ex.ToString());
                return "error";
            }
        }

        public static string Remote_Servers()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("remote_servers");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (remote_servers)", element.ToString());
                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (remote_servers)", ex.ToString());
                return "error";
            }
        }

        public static string Update_Servers()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("update_servers");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (update_servers)", element.ToString());
                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (update_servers)", ex.ToString());
                return "error";
            }
        }

        public static string Trust_Servers()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("trust_servers");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (trust_servers)", element.ToString());
                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (trust_servers)", ex.ToString());
                return "error";
            }
        }

        public static string File_Servers()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("file_servers");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (file_servers)", element.ToString());
                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (file_servers)", ex.ToString());
                return "error";
            }
        }

        public static string Tenant_Guid()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("tenant_guid");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (tenant_guid)", element.ToString());
                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (tenant_guid)", ex.ToString());
                return "error";
            }
        }

        public static string Location_Guid()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("location_guid");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (location_guid)", element.ToString());
                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (location_guid)", ex.ToString());
                return "error";
            }
        }

        public static string Language()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("language");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (language)", element.ToString());
                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (language)", ex.ToString());
                return "error";
            }
        }

        public static string Access_Key()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("access_key");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (access_key)", element.ToString());
                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (access_key)", ex.ToString());
                return "error";
            }
        }

        public static string Authorized()
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    JsonElement element = document.RootElement.GetProperty("authorized");
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (authorized)", element.ToString());
                    return element.ToString();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (authorized)", ex.ToString());
                return "error";
            }
        }

        public static string Check_Access_Key(int _return_type)
        {
            try
            {
                string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);

                // Parse the JSON
                using (JsonDocument document = JsonDocument.Parse(server_config_json))
                {
                    string server_config_json = File.ReadAllText(Application_Paths.program_data_server_config_json);
                    Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (server_config_json)", server_config_json);
                    // Parse the JSON
                    using (JsonDocument document = JsonDocument.Parse(server_config_json))
                    {
                        JsonElement element = document.RootElement.GetProperty("access_key");
                        Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (access_key)", element.ToString());
                        return element.ToString();
                    }

                    // Get the authorized status
                    try
                    {
                        JsonElement element = document.RootElement.GetProperty("authorized");
                        Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load (authorized)", element.ToString());

                        if (_return_type == 12)
                            return element.ToString();
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Server_Config_Handler", "Server_Config_Handler.Load (authorized) - Parsing", ex.ToString());
                    }

                    // Check if the access key is valid
                    if (access_key == string.Empty)
                    {
                        Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load", "Access key is empty");

                        // Generate a new access key
                        access_key = Guid.NewGuid().ToString();

                        // Write the new access key to the server config file
                        // Create the JSON object
                        var jsonObject = new
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
                            language = Configuration.Agent.language,
                            access_key = access_key,
                            authorized = false,
                        };

                        // Convert the object into a JSON string
                        string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                        Logging.Debug("Online_Mode.Handler.Update_Device_Information", "json", json);

                        // Write the new server config JSON to the file
                        File.WriteAllText(Application_Paths.program_data_server_config_json, json);
                    }
                    else
                    {
                        Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load", "Access key is not empty");
                    }
                }

                return "ok";
            }
            catch (Exception ex)
            {
                Logging.Debug("Server_Config_Handler", "Server_Config_Handler.Load", ex.ToString());
                return "error";
            }
        }
    }
}
