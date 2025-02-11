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
                Logging.Handler.Error("Classes.MySQL.Handler.Execute_Command", "Query: " + query,  ex.ToString());
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

        public static async Task<bool> Update_Server_Information()
        {
            string ip_address = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).FirstOrDefault().ToString();
            string domain = Dns.GetHostName();
            string os = Environment.OSVersion.ToString();
            string hearthbeat = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string appsettings = File.ReadAllText("appsettings.json");

            int cpu_usage = await Helper.System_Information.Handler.Get_CPU_Usage();
            int ram_usage = await Helper.System_Information.Handler.Get_RAM_Usage();
            int disk_usage = await Helper.System_Information.Handler.Get_Disk_Usage();

            // Check if server already exists in table
            string query = "SELECT COUNT(*) FROM servers WHERE name = @name;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", Environment.MachineName);
                
                int count = Convert.ToInt32(cmd.ExecuteScalar());

                if (count == 0)
                {
                    // Insert new server
                    query = "INSERT INTO servers (name, ip_address, domain, os, hearthbeat, appsettings, cpu_usage, ram_usage, disk_usage) VALUES (@name, @ip_address, @domain, @os, @hearthbeat, @appsettings, @cpu_usage, @ram_usage, @disk_usage);";

                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@name", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@ip_address", ip_address);
                    cmd.Parameters.AddWithValue("@domain", domain);
                    cmd.Parameters.AddWithValue("@os", os);
                    cmd.Parameters.AddWithValue("@hearthbeat", hearthbeat);
                    cmd.Parameters.AddWithValue("@appsettings", appsettings);
                    cmd.Parameters.AddWithValue("@cpu_usage", cpu_usage);
                    cmd.Parameters.AddWithValue("@ram_usage", ram_usage);
                    cmd.Parameters.AddWithValue("@disk_usage", disk_usage);

                    cmd.ExecuteNonQuery();
                }
                else
                {
                    // Update server
                    query = "UPDATE servers SET ip_address = @ip_address, domain = @domain, os = @os, hearthbeat = @hearthbeat, appsettings = @appsettings, cpu_usage = @cpu_usage, ram_usage = @ram_usage, disk_usage = @disk_usage WHERE name = @name;";

                    cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@name", Environment.MachineName);
                    cmd.Parameters.AddWithValue("@ip_address", ip_address);
                    cmd.Parameters.AddWithValue("@domain", domain);
                    cmd.Parameters.AddWithValue("@os", os);
                    cmd.Parameters.AddWithValue("@hearthbeat", hearthbeat);
                    cmd.Parameters.AddWithValue("@appsettings", appsettings);
                    cmd.Parameters.AddWithValue("@cpu_usage", cpu_usage);
                    cmd.Parameters.AddWithValue("@ram_usage", ram_usage);
                    cmd.Parameters.AddWithValue("@disk_usage", disk_usage);

                    cmd.ExecuteNonQuery();
                }

                Logging.Handler.Debug("Classes.MySQL.Handler.Update_Server_Information", "Query", query);

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Update_Server_Information", "Query: " + query, ex.ToString());
                conn.Close();
                return false;
            }
            finally
            {
                conn.Close();
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
                conn.Close();
                return String.Empty;
            }
            finally
            {
                conn.Close();
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
                conn.Close();
            }
        }
    }
}
