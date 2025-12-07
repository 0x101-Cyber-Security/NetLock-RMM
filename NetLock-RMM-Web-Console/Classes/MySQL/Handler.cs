using MySqlConnector;
using System.Data.Common;
using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using MudBlazor;
using System.ComponentModel;
using Newtonsoft.Json;

namespace NetLock_RMM_Web_Console.Classes.MySQL
{
    public class Handler
    {
        public static async Task<bool> Test_Connection()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                conn.Open();

                string sql = "SELECT * FROM clients;";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();

                conn.Close();
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> Check_Duplicate(string query)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();

                Logging.Handler.Debug("Classes.MySQL.Handler.Execute_Command", "Query", query);

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Execute_Command", "Query: " + query, ex.Message);
                await conn.CloseAsync();
                return false;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }


        public static async Task<bool> Execute_Command(string query)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();

                Logging.Handler.Debug("Classes.MySQL.Handler.Execute_Command", "Query", query);

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Execute_Command", "Query: " + query, ex.ToString());
                await conn.CloseAsync();
                return false;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public static async Task<string> Quick_Reader(string query, string item)
        {
            Logging.Handler.Debug("Classes.MySQL.Handler.Quick_Reader", "query", query);
            Logging.Handler.Debug("Classes.MySQL.Handler.Quick_Reader", "item",
                item); // war vorher ein Fehler, `query` statt `item`

            List<string> results = new List<string>();
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                Logging.Handler.Debug("Classes.MySQL.Handler.Quick_Reader", "MySQL_Prepared_Query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            var value = reader[item]?.ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                                results.Add(value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Quick_Reader", $"query: {query} item: {item}", ex.Message);
                await conn.CloseAsync();
            }
            finally
            {
                await conn.CloseAsync();
            }

            return string.Join(",", results);
        }

        public static async Task<string> Get_TenantID_From_DeviceID(string device_id)
        {
            string query = "SELECT tenant_id FROM devices WHERE id = @device_id;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@device_id", device_id);

                string tenant_id = cmd.ExecuteScalar().ToString();

                Logging.Handler.Debug("Classes.MySQL.Handler.Get_TenantID_From_DeviceID", "Query", query);

                return tenant_id;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Get_TenantID_From_DeviceID", "Query: " + query,
                    ex.ToString());
                return String.Empty;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public static async Task<bool> Reset_Device_Sync(bool global, string device_id)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "UPDATE devices SET synced = 0 WHERE id = @device_id;";

                if (global)
                    query = "UPDATE devices SET synced = 0;";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@device_id", device_id);
                cmd.ExecuteNonQuery();

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Add_Policy_Dialog", "Result", ex.ToString());
                return false;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Use MySQL Reader. Get the guid from table tenants and the guid from table locations
        public static async Task<(string, string)> Get_Tenant_Location_Guid(string tenant_name, string location_name)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            string tenant_guid = String.Empty;
            string location_guid = String.Empty;

            try
            {
                await conn.OpenAsync();

                // Abfrage für tenant_guid
                string tenantGuidQuery = "SELECT guid FROM tenants WHERE name = @tenant_name;";
                MySqlCommand tenantCmd = new MySqlCommand(tenantGuidQuery, conn);
                tenantCmd.Parameters.AddWithValue("@tenant_name", tenant_name);

                using (DbDataReader tenantReader = await tenantCmd.ExecuteReaderAsync())
                {
                    if (tenantReader.HasRows && await tenantReader.ReadAsync())
                    {
                        tenant_guid = tenantReader["guid"].ToString();
                    }
                }

                // Abfrage für location_guid
                string locationGuidQuery = "SELECT guid FROM locations WHERE name = @location_name;";
                MySqlCommand locationCmd = new MySqlCommand(locationGuidQuery, conn);
                locationCmd.Parameters.AddWithValue("@location_name", location_name);

                using (DbDataReader locationReader = await locationCmd.ExecuteReaderAsync())
                {
                    if (locationReader.HasRows && await locationReader.ReadAsync())
                    {
                        location_guid = locationReader["guid"].ToString();
                    }
                }

                return (tenant_guid, location_guid);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/devices -> Get_Tenant_Location_Guid", "MySQL_Query", ex.Message);
                return (String.Empty, String.Empty);
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get the tenant id with tenant_name
        public static async Task<int> Get_Tenant_Id(string tenant_name)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                // Abfrage für tenant_id
                string tenantIdQuery = "SELECT id FROM tenants WHERE name = @tenant_name;";
                MySqlCommand tenantCmd = new MySqlCommand(tenantIdQuery, conn);
                tenantCmd.Parameters.AddWithValue("@tenant_name", tenant_name);

                int tenant_id = 0;
                using (MySqlDataReader tenantReader = await tenantCmd.ExecuteReaderAsync())
                {
                    if (tenantReader.HasRows && await tenantReader.ReadAsync())
                    {
                        tenant_id = tenantReader.GetInt32("id");
                    }
                }

                return tenant_id;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Get_Tenant_Id", "General error", ex.ToString());
                return 0;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get the tenant id with tenant_name
        public static async Task<string> Get_Tenant_Name_By_Id(int id)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                // Abfrage für tenant_id
                string tenantIdQuery = "SELECT name FROM tenants WHERE id = @id;";
                MySqlCommand tenantCmd = new MySqlCommand(tenantIdQuery, conn);
                tenantCmd.Parameters.AddWithValue("@id", id);

                string tenant_name = String.Empty;
                using (MySqlDataReader tenantReader = await tenantCmd.ExecuteReaderAsync())
                {
                    if (tenantReader.HasRows && await tenantReader.ReadAsync())
                    {
                        tenant_name = tenantReader.GetString("name");
                    }
                }

                return tenant_name;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Get_Tenant_Id", "General error", ex.ToString());
                return String.Empty;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get session_guid from accounts table by username
        public static async Task<string> Get_Session_Guid_By_Username(string username)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            try
            {
                await conn.OpenAsync();

                // Query for tenant_id
                string sessionGuidQuery = "SELECT session_guid FROM accounts WHERE username = @username;";

                MySqlCommand sessionCmd = new MySqlCommand(sessionGuidQuery, conn);
                sessionCmd.Parameters.AddWithValue("@username", username);

                string session_guid = String.Empty;

                using (MySqlDataReader sessionReader = await sessionCmd.ExecuteReaderAsync())
                {
                    if (sessionReader.HasRows && await sessionReader.ReadAsync())
                    {
                        session_guid = sessionReader.GetString("session_guid");
                    }
                }

                return session_guid;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Session_Guid", "General error", ex.ToString());
                return String.Empty;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get api_key from settings table
        public static async Task<string> Get_Api_Key()
        {
            return await Quick_Reader("SELECT members_portal_api_key FROM settings;", "members_portal_api_key");
        }

        // Get device platform
        public static async Task<string> Get_Device_Platform(string device_id)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            string query = "SELECT platform FROM devices WHERE id = @device_id;";

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@device_id", device_id);
                string platform = cmd.ExecuteScalar()?.ToString() ?? String.Empty;

                Logging.Handler.Debug("Classes.MySQL.Handler.Get_Device_Platform", "Query", query);

                return platform;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Get_Device_Platform", "Query: " + query, ex.ToString());
                return String.Empty;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public static async Task<(string, string)> GetOperatorFirstLastName(string netlock_username)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SELECT * FROM accounts WHERE username = @netlock_username;";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@netlock_username", netlock_username);

                Logging.Handler.Debug("Example", "MySQL_Prepared_Query", query);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            return (reader["first_name"].ToString() ?? String.Empty,
                                reader["last_name"].ToString() ?? String.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Example", "MySQL_Query", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return (String.Empty, String.Empty);
        }

        public class Automation_Entity
        {
            public string? name { get; set; }
            public string? date { get; set; }
            public string? author { get; set; }
            public string? description { get; set; }
            public int? category { get; set; }
            public int? sub_category { get; set; }
            public int? condition { get; set; }
            public string? expected_result { get; set; }
            public string? trigger { get; set; }
        }

        public static async Task<string> GetAssignedDevicePolicyByDeviceId(string device_id)
        {
            string policy_name = null;

            var device_identity = await Get_Device_Details_For_Policy_Assignment(device_id);
            string device_name = device_identity.Item1;
            string tenant_name = device_identity.Item2;
            string location_name = device_identity.Item3;
            string group_name = device_identity.Item4;
            string internal_ip_address = device_identity.Item5;
            string external_ip_address = device_identity.Item6;
            string domain = device_identity.Item7;
            
            Logging.Handler.Debug("Agent.Windows.Policy_Handler.Get_Policy", "Device Details",
                $"Device Name: {device_name}, Tenant Name: {tenant_name}, Location Name: {location_name}, Group Name: {group_name}, Internal IP: {internal_ip_address}, External IP: {external_ip_address}, Domain: {domain}");
            
            // Get automations from database
            List<Automation_Entity> automations_list = new List<Automation_Entity>();

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                string query = "SELECT * FROM automations;";
                
                await conn.OpenAsync();
                
                MySqlCommand command = new MySqlCommand(query, conn);

                Logging.Handler.Debug("Agent.Windows.Policy_Handler.Get_Policy (automations)", "MySQL_Prepared_Query",
                    query);
                
                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            Logging.Handler.Debug("Agent.Windows.Policy_Handler.Get_Policy", "automation_json", reader["json"].ToString());

                            Automation_Entity automation = JsonConvert.DeserializeObject<Automation_Entity>(reader["json"].ToString());
                            automations_list.Add(automation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Agent.Windows.Policy_Handler.Get_Policy", "MySQL_Query", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            // Filter automations, detect which automation applies to the device and return the policy

            // Device Name
            foreach (var automation in automations_list)
            {
                if (automation.category == 0 && automation.condition == 0 && automation.expected_result == device_name)
                {
                    policy_name = automation.trigger;
                    break;
                }
            }

            // Tenant
            if (policy_name == null)
            {
                foreach (var automation in automations_list)
                {
                    if (automation.category == 0 && automation.condition == 1 && automation.expected_result == tenant_name)
                    {
                        policy_name = automation.trigger;
                        break;
                    }
                }
            }

            // Location
            if (policy_name == null)
            {
                foreach (var automation in automations_list)
                {
                    if (automation.category == 0 && automation.condition == 2 && automation.expected_result == location_name)
                    {
                        policy_name = automation.trigger;
                        break;
                    }
                }
            }

            // Group
            if (policy_name == null)
            {
                foreach (var automation in automations_list)
                {
                    if (automation.category == 0 && automation.condition == 3 && automation.expected_result == group_name)
                    {
                        policy_name = automation.trigger;
                        break;
                    }
                }
            }

            // Internal IP
            if (policy_name == null)
            {
                foreach (var automation in automations_list)
                {
                    if (automation.category == 0 && automation.condition == 4 && automation.expected_result == internal_ip_address)
                    {
                        policy_name = automation.trigger;
                        break;
                    }
                }
            }

            // External IP
            if (policy_name == null)
            {
                foreach (var automation in automations_list)
                {
                    if (automation.category == 0 && automation.condition == 5 && automation.expected_result == external_ip_address)
                    {
                        policy_name = automation.trigger;
                        break;
                    }
                }
            }

            // Domain
            if (policy_name == null)
            {
                foreach (var automation in automations_list)
                {
                    if (automation.category == 0 && automation.condition == 6 && automation.expected_result == domain)
                    {
                        policy_name = automation.trigger;
                        break;
                    }
                }
            }

            // No policy found
            if (policy_name == null)
            {
                Logging.Handler.Debug("Agent.Windows.Policy_Handler.Get_Policy", "matched", "no_assigned_policy_found");
                return "-";
            }

            Logging.Handler.Debug("Agent.Windows.Policy_Handler.Get_Policy", "Filter automations (matched)", policy_name);
            
            return policy_name;
        }

        private static async Task<string> GetDeviceNameById(string device_id)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                string query = "SELECT * FROM devices WHERE id = @device_id;";

                MySqlCommand command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@device_id", device_id);

                Logging.Handler.Debug("Agent.Windows.Policy_Handler.Get_Policy (automations)", "MySQL_Prepared_Query",
                    query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            return reader["device_name"].ToString() ?? string.Empty;
                        }
                    }
                }

                return String.Empty;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Agent.Windows.Policy_Handler.Get_Policy", "MySQL_Query", ex.ToString());
                return String.Empty;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get location name by device id
        public static async Task<string> Get_Location_Name_By_Device_Id(string device_id)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SELECT * FROM devices WHERE id = @device_id;";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@device_id", device_id);

                string location_name = cmd.ExecuteScalar()?.ToString() ?? String.Empty;

                Logging.Handler.Debug("Classes.MySQL.Handler.Get_Location_Name_By_Device_Id", "Query", query);

                return location_name;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Get_Location_Name_By_Device_Id", "Query: ", ex.ToString());
                return String.Empty;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get device details for policy assignment by device id
        public static async Task<(string, string, string, string, string, string, string)> Get_Device_Details_For_Policy_Assignment(string device_id)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            try
            {
                await conn.OpenAsync();

                string query = "SELECT * FROM devices WHERE id = @device_id;";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@device_id", device_id);

                string device_name = String.Empty;
                string tenant_name = String.Empty;
                string location_name = String.Empty;
                string group_name = String.Empty;
                string ip_address_internal = String.Empty;
                string ip_address_external = String.Empty;
                string domain = String.Empty;
                
                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            device_name = reader["device_name"].ToString() ?? String.Empty;
                            tenant_name = reader["tenant_name"].ToString() ?? String.Empty;
                            location_name = reader["location_name"].ToString() ?? String.Empty;
                            group_name = reader["group_name"].ToString() ?? String.Empty;
                            ip_address_internal = reader["ip_address_internal"].ToString() ?? String.Empty;
                            ip_address_external = reader["ip_address_external"].ToString() ?? String.Empty;
                            domain = reader["domain"].ToString() ?? String.Empty;
                        }
                    }
                }

                Logging.Handler.Debug("Classes.MySQL.Handler.Get_Device_Details_For_Policy_Assignment", "Query", query);
                return (device_name, tenant_name, location_name, group_name, ip_address_internal, ip_address_external, domain);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Get_Device_Details_For_Policy_Assignment", "Query: ", ex.ToString());
                return (string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
