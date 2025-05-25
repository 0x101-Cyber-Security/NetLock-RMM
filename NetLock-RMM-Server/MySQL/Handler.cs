using MySqlConnector;
using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.ComponentModel;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Data.Common;

namespace NetLock_RMM_Server.MySQL
{
    public class Handler
    {
        // Check connection
        public static async Task<bool> Check_Connection()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Check_Connection", "Result", ex.Message);
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Check if MySQL is used (not MariaDB) and if the version is supported by NetLock
        public static async Task<bool> Verify_Supported_SQL_Server()
        {
            try
            {
                await using var conn = new MySqlConnection(Configuration.MySQL.Connection_String);
                await conn.OpenAsync();

                const string query = "SELECT @@version, @@version_comment;";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    string version = reader.GetString(0);
                    string versionComment = reader.GetString(1);

                    Logging.Handler.Debug("Classes.MySQL.Database.Verify_Supported_SQL_Server", "version", version);
                    Logging.Handler.Debug("Classes.MySQL.Database.Verify_Supported_SQL_Server", "versionComment", versionComment);

                    // Check whether it is MariaDB
                    if (versionComment.IndexOf("mariadb", StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Verify_Supported_SQL_Server", "Fehler beim Überprüfen des SQL-Servers", ex.ToString());
                return false;
            }

            return true;
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
                Logging.Handler.Error("Classes.MySQL.Handler.Execute_Command", "Query: " + query,  ex.ToString());
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
            }
            finally
            {
                await conn.CloseAsync();
            }

            return result;
        }

        public static async Task<bool> Update_Server_Information()
        {
            using MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            string query = "SELECT COUNT(*) FROM servers WHERE name = @name;";

            try
            {
                string ip_address = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "";
                string domain = Dns.GetHostName();
                string os = Environment.OSVersion.ToString();
                string hearthbeat = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string appsettings = File.ReadAllText("appsettings.json");

                int cpu_usage = await Helper.System_Information.Handler.Get_CPU_Usage();
                int ram_usage = await Helper.System_Information.Handler.Get_RAM_Usage();
                int disk_usage = await Helper.System_Information.Handler.Get_Disk_Usage();

                await conn.OpenAsync();

                // Erstes Command für COUNT(*)
                using (MySqlCommand countCmd = new MySqlCommand(query, conn))
                {
                    countCmd.Parameters.AddWithValue("@name", Environment.MachineName);
                    int count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

                    // Neuen Query abhängig vom count setzen
                    if (count == 0)
                    {
                        query = @"INSERT INTO servers (name, ip_address, domain, os, hearthbeat, appsettings, cpu_usage, ram_usage, disk_usage, docker)
                          VALUES (@name, @ip_address, @domain, @os, @hearthbeat, @appsettings, @cpu_usage, @ram_usage, @disk_usage, @docker);";
                    }
                    else
                    {
                        query = @"UPDATE servers 
                          SET ip_address = @ip_address, domain = @domain, os = @os, hearthbeat = @hearthbeat, appsettings = @appsettings, cpu_usage = @cpu_usage, ram_usage = @ram_usage, disk_usage = @disk_usage, docker = @docker 
                          WHERE name = @name;";
                    }

                    // Zweites Command für INSERT oder UPDATE
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", Environment.MachineName);
                        cmd.Parameters.AddWithValue("@ip_address", ip_address);
                        cmd.Parameters.AddWithValue("@domain", domain);
                        cmd.Parameters.AddWithValue("@os", os);
                        cmd.Parameters.AddWithValue("@hearthbeat", hearthbeat);
                        cmd.Parameters.AddWithValue("@appsettings", appsettings);
                        cmd.Parameters.AddWithValue("@cpu_usage", cpu_usage);
                        cmd.Parameters.AddWithValue("@ram_usage", ram_usage);
                        cmd.Parameters.AddWithValue("@disk_usage", disk_usage);
                        cmd.Parameters.AddWithValue("@docker", Configuration.Server.isDocker ? 1 : 0);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    Logging.Handler.Debug("Classes.MySQL.Handler.Update_Server_Information", "Query", query);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Update_Server_Information", "Query: " + query, ex.ToString());
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public static async Task <string> Get_TenantID_From_DeviceID(string device_id)
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
                Logging.Handler.Error("Classes.MySQL.Handler.Get_TenantID_From_DeviceID", "Query: " + query, ex.ToString());
                return String.Empty;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public static async Task <int> Get_Authorized_Devices_Count_30_Days()
        {
            string query = "SELECT COUNT(*) FROM devices WHERE authorized = '1' AND last_access >= NOW() - INTERVAL 30 DAY;\r\n";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(query, conn);
                int count = Convert.ToInt32(cmd.ExecuteScalar());

                Logging.Handler.Debug("Classes.MySQL.Handler.Get_Authorized_Devices", "Query", query);

                return count;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Get_Authorized_Devices", "Query: " + query, ex.ToString());
                return 0;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get members portal api key from settings table
        public static async Task<string> Get_Members_Portal_Api_Key()
        {
            return await MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "members_portal_api_key");
        }

        // Get admin username by remote_session_token
        public static async Task<string> Get_Admin_Username_By_Remote_Session_Token(string remote_session_token)
        {
            string query = "SELECT username FROM accounts WHERE remote_session_token = @remote_session_token;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            
            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@remote_session_token", remote_session_token);
                
                string username = cmd.ExecuteScalar()?.ToString() ?? String.Empty;
                
                Logging.Handler.Debug("Classes.MySQL.Handler.Get_Admin_Username_By_Remote_Session_Token", "Query", query);
                
                return username;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Get_Admin_Username_By_Remote_Session_Token", "Query: " + query, ex.ToString());
                return String.Empty;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
