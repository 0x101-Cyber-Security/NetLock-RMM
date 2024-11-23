using MySqlConnector;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using static NetLock_RMM_Server.Agent.Windows.Authentification;
using static NetLock_RMM_Server.Agent.Windows.Device_Handler;

namespace NetLock_RMM_Server.Agent.Windows
{
    public class Device_Handler
    {
        public class Device_Identity_Entity
        {
            public string? agent_version { get; set; }
            public string? device_name { get; set; }
            public string? location_guid{ get; set; }
            public string? tenant_guid { get; set; }
            public string? access_key { get; set; }
            public string? hwid { get; set; }
            public string? ip_address_internal { get; set; }
            public string? operating_system { get; set; }
            public string? domain { get; set; }
            public string? antivirus_solution { get; set; }
            public string? firewall_status { get; set; }
            public string? architecture { get; set; }
            public string? last_boot { get; set; }
            public string? timezone { get; set; }
            public string? cpu { get; set; }
            public string? cpu_usage { get; set; }
            public string? mainboard { get; set; }
            public string? gpu { get; set; }
            public string? ram { get; set; }
            public string? ram_usage { get; set; }
            public string? tpm { get; set; }
            //public string? environment_variables { get; set; }
        }

        public class Content_Entity
        {
            public string? network_adapters { get; set; }
            public string? disks { get; set; }
            public string? applications_installed { get; set; }
            public string? applications_logon { get; set; }
            public string? applications_scheduled_tasks { get; set; }
            public string? applications_drivers { get; set; }
            public string? applications_services { get; set; }
            public string? antivirus_information { get; set; }
        }

        public class Root_Entity
        {
            public Device_Identity_Entity? device_identity { get; set; }
            public Content_Entity? content { get; set; }
        }


        public static async Task<string> Update_Device_Information(string json)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            
            try
            {
                //Extract JSON
                Root_Entity rootData = JsonSerializer.Deserialize<Root_Entity>(json);
                Device_Identity_Entity device_identity = rootData.device_identity;
                //Content_Entity content = rootData.content;

                string device_identity_json_string = String.Empty;
                string cpu_information_json_string = String.Empty;
                string ram_information_json_string = String.Empty;
                string network_adapters_json_string = String.Empty;
                string disks_json_string = String.Empty;
                string cpu_json_string = String.Empty;
                string ram_json_string = String.Empty;
                string applications_installed_json_string = String.Empty;
                string applications_logon_json_string = String.Empty;
                string applications_scheduled_tasks_json_string = String.Empty;
                string applications_drivers_json_string = String.Empty;
                string applications_services_json_string = String.Empty;
                string processes_json_string = String.Empty;
                string antivirus_products_json_string = String.Empty;
                string antivirus_information_json_string = String.Empty;

                // Deserialisierung des gesamten JSON-Strings
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    // Extrahieren des JSON-Strings für den "disks"-Abschnitt
                    JsonElement device_identity_element = document.RootElement.GetProperty("device_identity");
                    device_identity_json_string = device_identity_element.ToString();

                    JsonElement cpu_information_element = document.RootElement.GetProperty("cpu_information");
                    cpu_information_json_string = cpu_information_element.ToString();
                    
                    JsonElement ram_information_element = document.RootElement.GetProperty("ram_information");
                    ram_information_json_string = ram_information_element.ToString();

                    JsonElement network_adapters_element = document.RootElement.GetProperty("network_adapters");
                    network_adapters_json_string = network_adapters_element.ToString();

                    JsonElement disks_element = document.RootElement.GetProperty("disks");
                    disks_json_string = disks_element.ToString();

                    JsonElement applications_installed_element = document.RootElement.GetProperty("applications_installed");
                    applications_installed_json_string = applications_installed_element.ToString();

                    JsonElement applications_logon_element = document.RootElement.GetProperty("applications_logon");
                    applications_logon_json_string = applications_logon_element.ToString();

                    JsonElement applications_scheduled_tasks_element = document.RootElement.GetProperty("applications_scheduled_tasks");
                    applications_scheduled_tasks_json_string = applications_scheduled_tasks_element.ToString();

                    JsonElement applications_drivers_element = document.RootElement.GetProperty("applications_drivers");
                    applications_drivers_json_string = applications_drivers_element.ToString();

                    JsonElement applications_services_element = document.RootElement.GetProperty("applications_services");
                    applications_services_json_string = applications_services_element.ToString();

                    JsonElement processes_element = document.RootElement.GetProperty("processes");
                    processes_json_string = processes_element.ToString();

                    JsonElement cpu_element = document.RootElement.GetProperty("cpu_information");
                    cpu_json_string = cpu_element.ToString();

                    JsonElement ram_element = document.RootElement.GetProperty("ram_information");
                    ram_json_string = ram_element.ToString();

                    JsonElement antivirus_products_element = document.RootElement.GetProperty("antivirus_products");
                    antivirus_products_json_string = antivirus_products_element.ToString();

                    JsonElement antivirus_information_element = document.RootElement.GetProperty("antivirus_information");
                    antivirus_information_json_string = antivirus_information_element.ToString();

                    // Ausgabe des extrahierten JSON-Strings
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "ram_information_json_string", ram_information_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "cpu_information_json_string", cpu_information_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "network_adapters_json_string", network_adapters_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "disks_json_string", disks_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "applications_installed_string", applications_installed_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "applications_logon_string", applications_logon_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "applications_scheduled_tasks_string", applications_scheduled_tasks_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "applications_drivers_string", applications_drivers_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "applications_services_string", applications_services_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "processes_json_string", processes_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "cpu_json_string", cpu_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "ram_json_string", ram_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "antivirus_products_json_string", antivirus_products_json_string);
                    Logging.Handler.Debug("Agent.Windows.Device_Handler.Update_Device_Information", "antivirus_information_json_string", antivirus_information_json_string);
                }

                // Get the tenant id & location id with tenant_guid & location_guid
                (int tenant_id, int location_id) = await Helper.Get_Tenant_Location_Id(device_identity.tenant_guid, device_identity.location_guid);

                //Insert into database
                await conn.OpenAsync();

                string execute_query = "UPDATE `devices` SET " +
                       "`ram_information` = @ram_information, " +
                       "`cpu_information` = @cpu_information, " +
                       "`network_adapters` = @network_adapters, " +
                       "`disks` = @disks, " +
                       "`applications_installed` = @applications_installed, " +
                       "`applications_logon` = @applications_logon, " +
                       "`applications_scheduled_tasks` = @applications_scheduled_tasks, " +
                       "`applications_drivers` = @applications_drivers, " +
                       "`applications_services` = @applications_services, " +
                       "`processes` = @processes, " +
                       "`antivirus_products` = @antivirus_products, " +
                       "`antivirus_information` = @antivirus_information " +
                       "WHERE device_name = @device_name AND location_id = @location_id AND tenant_id = @tenant_id";

                MySqlCommand cmd = new MySqlCommand(execute_query, conn);

                cmd.Parameters.AddWithValue("@tenant_id", tenant_id);
                cmd.Parameters.AddWithValue("@location_id", location_id);
                cmd.Parameters.AddWithValue("@device_name", device_identity.device_name);
                cmd.Parameters.AddWithValue("@ram_information", ram_information_json_string);
                cmd.Parameters.AddWithValue("@cpu_information", cpu_information_json_string);
                cmd.Parameters.AddWithValue("@network_adapters", network_adapters_json_string);
                cmd.Parameters.AddWithValue("@disks", disks_json_string);
                cmd.Parameters.AddWithValue("@applications_installed", applications_installed_json_string);
                cmd.Parameters.AddWithValue("@applications_logon", applications_logon_json_string);
                cmd.Parameters.AddWithValue("@applications_scheduled_tasks", applications_scheduled_tasks_json_string);
                cmd.Parameters.AddWithValue("@applications_drivers", applications_drivers_json_string);
                cmd.Parameters.AddWithValue("@applications_services", applications_services_json_string);
                cmd.Parameters.AddWithValue("@processes", processes_json_string);
                cmd.Parameters.AddWithValue("@antivirus_products", antivirus_products_json_string);
                cmd.Parameters.AddWithValue("@antivirus_information", antivirus_information_json_string);
                cmd.ExecuteNonQuery();

                // Get device id with device name, tenant id & location id
                int device_id = await Helper.Get_Device_Id(device_identity.device_name, tenant_id, location_id);

                //Insert applications_installed_history
                string applications_installed_history_execute_query = "INSERT INTO `applications_installed_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand applications_installed_history_cmd = new MySqlCommand(applications_installed_history_execute_query, conn);

                applications_installed_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                applications_installed_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                applications_installed_history_cmd.Parameters.AddWithValue("@json", applications_installed_json_string);
                applications_installed_history_cmd.ExecuteNonQuery();

                //Insert applications_logon_history
                string applications_logon_history_execute_query = "INSERT INTO `applications_logon_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand applications_logon_history_cmd = new MySqlCommand(applications_logon_history_execute_query, conn);

                applications_logon_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                applications_logon_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                applications_logon_history_cmd.Parameters.AddWithValue("@json", applications_logon_json_string);
                applications_logon_history_cmd.ExecuteNonQuery();

                //Insert applications_scheduled_tasks_history
                string applications_scheduled_tasks_history_execute_query = "INSERT INTO `applications_scheduled_tasks_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand applications_scheduled_tasks_history_cmd = new MySqlCommand(applications_scheduled_tasks_history_execute_query, conn);

                applications_scheduled_tasks_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                applications_scheduled_tasks_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                applications_scheduled_tasks_history_cmd.Parameters.AddWithValue("@json", applications_scheduled_tasks_json_string);
                applications_scheduled_tasks_history_cmd.ExecuteNonQuery();

                //Insert applications_services_history
                string applications_services_history_execute_query = "INSERT INTO `applications_services_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand applications_services_history_cmd = new MySqlCommand(applications_services_history_execute_query, conn);

                applications_services_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                applications_services_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                applications_services_history_cmd.Parameters.AddWithValue("@json", applications_services_json_string);
                applications_services_history_cmd.ExecuteNonQuery();

                //Insert applications_drivers_history
                string applications_drivers_history_execute_query = "INSERT INTO `applications_drivers_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand applications_drivers_history_cmd = new MySqlCommand(applications_drivers_history_execute_query, conn);

                applications_drivers_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                applications_drivers_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                applications_drivers_history_cmd.Parameters.AddWithValue("@json", applications_drivers_json_string);
                applications_drivers_history_cmd.ExecuteNonQuery();

                //Insert device_information_general_history

                //Get current policy from the device, since its not in the devices identity
                string device_information_general_history_ip_address_internal = String.Empty;
                string device_information_general_history_ip_address_external = String.Empty;
                string device_information_general_history_network_adapters = String.Empty;
                
                try
                {
                    string device_information_general_history_reader_query = $"SELECT * FROM `devices` WHERE device_name = @device_name AND location_id = @location_id AND tenant_id = @tenant_id;";
                    Logging.Handler.Debug("Modules.Authentification.Verify_Device", "MySQL_Query", device_information_general_history_reader_query);

                    MySqlCommand device_information_general_history_command = new MySqlCommand(device_information_general_history_reader_query, conn);
                    device_information_general_history_command.Parameters.AddWithValue("@device_name", device_identity.device_name);
                    device_information_general_history_command.Parameters.AddWithValue("@location_id", location_id);
                    device_information_general_history_command.Parameters.AddWithValue("@tenant_id", tenant_id);

                    DbDataReader device_information_general_history_reader = await device_information_general_history_command.ExecuteReaderAsync();

                    if (device_information_general_history_reader.HasRows)
                    {
                        while (await device_information_general_history_reader.ReadAsync())
                        {
                            device_information_general_history_ip_address_internal = device_information_general_history_reader["ip_address_internal"].ToString() ?? String.Empty;
                            device_information_general_history_ip_address_external = device_information_general_history_reader["ip_address_external"].ToString() ?? String.Empty;
                            device_information_general_history_network_adapters = device_information_general_history_reader["network_adapters"].ToString() ?? String.Empty;
                        }
                        await device_information_general_history_reader.CloseAsync();
                    }

                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("NetLock_RMM_Server.Modules.Authentification.Verify_Device", "Result", ex.Message);
                }

                string device_information_general_history_execute_query = "INSERT INTO `device_information_general_history` (`device_id`, `date`, `ip_address_internal`, `ip_address_external`, `network_adapters`, `json`) VALUES (@device_id, @date, @ip_address_internal, @ip_address_external, @network_adapters, @json);";

                MySqlCommand device_information_general_history_cmd = new MySqlCommand(device_information_general_history_execute_query, conn);
                device_information_general_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                device_information_general_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                device_information_general_history_cmd.Parameters.AddWithValue("@ip_address_internal", device_information_general_history_ip_address_internal);
                device_information_general_history_cmd.Parameters.AddWithValue("@ip_address_external", device_information_general_history_ip_address_external);
                device_information_general_history_cmd.Parameters.AddWithValue("@network_adapters", device_information_general_history_network_adapters);
                device_information_general_history_cmd.Parameters.AddWithValue("@json", device_identity_json_string);
                device_information_general_history_cmd.ExecuteNonQuery();

                //Insert device_information_disks_history
                string device_information_disks_history_execute_query = "INSERT INTO `device_information_disks_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand device_information_disks_history_cmd = new MySqlCommand(device_information_disks_history_execute_query, conn);

                device_information_disks_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                device_information_disks_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                device_information_disks_history_cmd.Parameters.AddWithValue("@json", disks_json_string);
                device_information_disks_history_cmd.ExecuteNonQuery();

                //Insert device_information_cpu_history
                string device_information_cpu_history_execute_query = "INSERT INTO `device_information_cpu_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand device_information_cpu_history_cmd = new MySqlCommand(device_information_cpu_history_execute_query, conn);

                device_information_cpu_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                device_information_cpu_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                device_information_cpu_history_cmd.Parameters.AddWithValue("@json", cpu_json_string);
                device_information_cpu_history_cmd.ExecuteNonQuery();

                //Insert device_information_ram_history
                string device_information_ram_history_execute_query = "INSERT INTO `device_information_ram_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand device_information_ram_history_cmd = new MySqlCommand(device_information_ram_history_execute_query, conn);

                device_information_ram_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                device_information_ram_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                device_information_ram_history_cmd.Parameters.AddWithValue("@json", ram_json_string);
                device_information_ram_history_cmd.ExecuteNonQuery();

                //Insert device_information_network_adapters_history
                string device_information_network_adapters_history_execute_query = "INSERT INTO `device_information_network_adapters_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand device_information_network_adapters_history_cmd = new MySqlCommand(device_information_network_adapters_history_execute_query, conn);

                device_information_network_adapters_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                device_information_network_adapters_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                device_information_network_adapters_history_cmd.Parameters.AddWithValue("@json", network_adapters_json_string);
                device_information_network_adapters_history_cmd.ExecuteNonQuery();

                //Insert device_information_task_manager_history
                string device_information_task_manager_history_execute_query = "INSERT INTO `device_information_task_manager_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand device_information_task_manager_history_cmd = new MySqlCommand(device_information_task_manager_history_execute_query, conn);

                device_information_task_manager_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                device_information_task_manager_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                device_information_task_manager_history_cmd.Parameters.AddWithValue("@json", processes_json_string);
                device_information_task_manager_history_cmd.ExecuteNonQuery();

                //Insert device_information_antivirus_history
                string device_information_antivirus_products_history_execute_query = "INSERT INTO `device_information_antivirus_products_history` (`device_id`, `date`, `json`) VALUES (@device_id, @date, @json);";

                MySqlCommand device_information_antivirus_products_history_cmd = new MySqlCommand(device_information_antivirus_products_history_execute_query, conn);

                device_information_antivirus_products_history_cmd.Parameters.AddWithValue("@device_id", device_id);
                device_information_antivirus_products_history_cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                device_information_antivirus_products_history_cmd.Parameters.AddWithValue("@json", antivirus_products_json_string);
                device_information_antivirus_products_history_cmd.ExecuteNonQuery();



                await conn.CloseAsync();

                return "authorized";
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Agent.Windows.Device_Handler.Update_Device_Information", "General error", ex.ToString());
                return "invalid";
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
