using MySqlConnector;
using System.Data.Common;
using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using MudBlazor;
using System.ComponentModel;

namespace NetLock_RMM_Web_Console.Classes.MySQL
{
    public class Handler
    {
        public static async Task <bool> Test_Connection()
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
                conn.Close();
                return false;
            }
            finally
            {
                conn.Close();
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
                Logging.Handler.Error("Classes.MySQL.Handler.Execute_Command", "Query: " + query,  ex.Message);
                conn.Close();
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        public static async Task<string> Quick_Reader(string query, string item)
        {
            Logging.Handler.Debug("Classes.MySQL.Handler.Quick_Reader", "query", query);
            Logging.Handler.Debug("Classes.MySQL.Handler.Quick_Reader", "item", query);

            string result = String.Empty;

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                Logging.Handler.Debug("Classes.MySQL.Handler.Quick_Reader", "MySQL_Prepared_Query", query); //Output prepared query

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            result = reader[item].ToString() ?? String.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Quick_Reader", "query: " + query + " item: " + item, ex.Message);
                conn.Close();
            }
            finally
            {
                conn.Close();
            }

            return result;
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
    }
}
