using MySqlConnector;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace NetLock_RMM_Web_Console.Classes.MySQL
{
    public class Database
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

        public static async Task<string> Get_Version()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SELECT VERSION();";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        return reader.GetString(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Version", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return String.Empty;
        }

        public static async Task<string> Get_Uptime()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SHOW GLOBAL STATUS LIKE 'Uptime';";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        return reader.GetString(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Uptime", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return String.Empty;
        }

        public static async Task<string> Get_Connected_Users()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            var result = new List<object>();

            try
            {
                await conn.OpenAsync();

                string query = "SELECT COUNT(*) AS ConnectedUserCount FROM information_schema.processlist WHERE user != 'system user';";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        int connectedUserCount = reader.GetInt32(reader.GetOrdinal("ConnectedUserCount"));
                        return connectedUserCount.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Connected_Users", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return "0";
        }

        public static async Task<string> Get_Active_Queries()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            var result = new List<object>();

            try
            {
                await conn.OpenAsync();

                string query = "SHOW FULL PROCESSLIST;";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        { 
                            // Adjust the correct index to obtain the required data.
                            var queryInfo = new
                            {
                                Id = reader.GetInt32(0),
                                User = reader.GetString(1),
                                Host = reader.GetString(2),
                                Database = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Command = reader.GetString(4),
                                Time = reader.GetInt32(5),
                                State = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Info = reader.IsDBNull(7) ? null : reader.GetString(7)
                            };

                            result.Add(queryInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Active_Queries", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            string json = JsonSerializer.Serialize(result);

            Logging.Handler.Debug("Classes.MySQL.Database.Get_Active_Queries", "json", json);

            return json;
        }

        public static async Task<string> Get_Database_Size()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SELECT table_schema 'Database', SUM(data_length + index_length) / 1024 / 1024 'Size (MB)' " +
                               "FROM information_schema.tables WHERE table_schema = @DatabaseName GROUP BY table_schema;";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DatabaseName", Configuration.MySQL.Database);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        // Hole die Größe als Dezimalwert
                        var sizeInMB = reader.GetDecimal(1);

                        // Runden auf zwei Nachkommastellen
                        var roundedSize = Math.Round(sizeInMB, 2);

                        // Rückgabe als String
                        return roundedSize.ToString() + " MB";
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Database_Size", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return String.Empty;
        }


        public static async Task<string> Get_Failed_Logins()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            var result = new List<object>();

            try
            {
                await conn.OpenAsync();

                string query = "SELECT COUNT(*) AS FailedLoginCount FROM accounts WHERE reset_password = 1;";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        int failedLoginCount = reader.GetInt32(reader.GetOrdinal("FailedLoginCount"));
                        return failedLoginCount.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Failed_Logins", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return "0";
        }

        public static async Task<string> Get_Max_Connections()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SHOW VARIABLES LIKE 'max_connections';";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        return reader.GetString(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Max_Connections", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return String.Empty;
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
                Logging.Handler.Error("Classes.MySQL.Database.Get_Tenant_Id", "General error", ex.ToString());
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
                Logging.Handler.Error("Classes.MySQL.Database.Get_Tenant_Id", "General error", ex.ToString());
                return String.Empty;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Check table existing
        public static async Task<bool> Check_Table_Existing()
        {


            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '" + Configuration.MySQL.Database + "' AND table_name = 'settings';";

                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        if (reader.GetInt32(0) > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Check_Table_Existing", "Result", ex.Message);
            }
            finally
            {
                await conn.CloseAsync();
            }

            return false;
        }

        // Check database existing
        public static async Task<bool> Check_Database_Existing()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '" + Configuration.MySQL.Database + "';";

                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        if (reader.GetInt32(0) > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Check_Database_Existing", "Result", ex.Message);
            }
            finally
            {
                await conn.CloseAsync();
            }

            return false;
        }

        public static async Task<string> Check_NetLock_Database_Version()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                // Get assembly version
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

                string version = String.Empty;

                conn.Open();

                string query = "SELECT db_version FROM settings;";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows && reader.Read())
                    {
                        version = reader["db_version"].ToString();

                        if (version !=  fvi.ProductVersion)
                        {
                            Logging.Handler.Debug("Check_Database_Version", "Database Version", "Database version is outdated. Please update the database.");
                        }
                        else
                        {
                            Logging.Handler.Debug("Check_Database_Version", "Database Version", "Database version is up to date.");
                        }
                    }
                }

                return version;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Check_Database_Version", "Result", ex.Message);
                return String.Empty;
            }
            finally
            {
                conn.Close();
            }
        }

        // Execute installation SQL script
        public static async Task<bool> Execute_Installation_Script()
        {
            // Read old settings:
            // smtp
            string smtp = String.Empty; 
            smtp = await MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "smtp");

            // files api key
            string files_api_key = String.Empty;
                files_api_key = await MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "files_api_key");

            // Generate random files api key if empty
            if (String.IsNullOrEmpty(files_api_key))
                files_api_key = Guid.NewGuid().ToString();

            try
            {
                // Get assembly version
                //Assembly assembly = Assembly.GetExecutingAssembly();
                //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

                string script = @"/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

CREATE TABLE IF NOT EXISTS `accounts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `password` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `reset_password` int DEFAULT '0',
  `role` enum('Administrator','Moderator','Customer') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `mail` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `phone` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `last_login` datetime DEFAULT '2000-01-01 00:00:00',
  `ip_address` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `two_factor_enabled` int DEFAULT '0',
  `two_factor_account_secret_key` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `permissions` mediumtext,
  `tenants` mediumtext,
  `session_guid` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE IF NOT EXISTS `agent_package_configurations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ssl` int DEFAULT '1',
  `communication_servers` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `remote_servers` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `update_servers` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `trust_servers` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `file_servers` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `tenant_id` int DEFAULT NULL,
  `location_id` int DEFAULT NULL,
  `language` enum('de-DE','en-US') COLLATE utf8mb4_unicode_ci DEFAULT 'en-US',
  `guid` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `antivirus_controlled_folder_access_rulesets` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `author` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `applications_drivers_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `applications_installed_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `applications_logon_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `applications_scheduled_tasks_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `applications_services_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `automations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `author` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `category` int DEFAULT NULL,
  `sub_category` int DEFAULT NULL,
  `condition` int DEFAULT NULL,
  `expected_result` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `trigger` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `devices` (
  `id` int NOT NULL AUTO_INCREMENT,
  `agent_version` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT '0.0.0.0',
  `tenant_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `tenant_id` int DEFAULT NULL,
  `location_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT '-',
  `location_id` int DEFAULT NULL,
  `group_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT '-',
  `group_id` int DEFAULT NULL,
  `device_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `access_key` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `hwid` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `blacklisted` int DEFAULT '0',
  `authorized` int DEFAULT '0',
  `last_access` datetime DEFAULT '2000-01-01 00:00:00',
  `synced` int DEFAULT '0',
  `ip_address_internal` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ip_address_external` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `operating_system` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `domain` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `antivirus_solution` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `firewall_status` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `architecture` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `last_boot` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `timezone` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `cpu` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `cpu_usage` int DEFAULT '0',
  `cpu_information` mediumtext COLLATE utf8mb4_unicode_ci,
  `mainboard` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `gpu` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ram` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ram_usage` int DEFAULT '0',
  `ram_information` mediumtext COLLATE utf8mb4_unicode_ci,
  `tpm` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `environment_variables` mediumtext COLLATE utf8mb4_unicode_ci,
  `network_adapters` mediumtext COLLATE utf8mb4_unicode_ci,
  `disks` mediumtext COLLATE utf8mb4_unicode_ci,
  `applications_installed` mediumtext COLLATE utf8mb4_unicode_ci,
  `applications_logon` mediumtext COLLATE utf8mb4_unicode_ci,
  `applications_scheduled_tasks` mediumtext COLLATE utf8mb4_unicode_ci,
  `applications_services` mediumtext COLLATE utf8mb4_unicode_ci,
  `applications_drivers` mediumtext COLLATE utf8mb4_unicode_ci,
  `processes` mediumtext COLLATE utf8mb4_unicode_ci,
  `notes` mediumtext COLLATE utf8mb4_unicode_ci,
  `antivirus_products` mediumtext COLLATE utf8mb4_unicode_ci,
  `antivirus_information` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `device_information_antivirus_products_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `device_information_cpu_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `device_information_disks_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `device_information_general_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `policy_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ip_address_internal` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `ip_address_external` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `network_adapters` mediumtext COLLATE utf8mb4_unicode_ci,
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `device_information_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `device_information_network_adapters_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `device_information_notes_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `author` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `note` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `device_information_ram_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `device_information_remote_shell_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `author` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `command` mediumtext COLLATE utf8mb4_unicode_ci,
  `result` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `device_information_task_manager_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `events` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `tenant_name_snapshot` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `location_name_snapshot` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `device_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT NULL,
  `severity` int DEFAULT NULL,
  `reported_by` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `_event` mediumtext COLLATE utf8mb4_unicode_ci,
  `description` mediumtext COLLATE utf8mb4_unicode_ci,
  `notification_json` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `read` int DEFAULT '0',
  `type` int DEFAULT NULL,
  `language` int DEFAULT NULL,
  `mail_status` int DEFAULT '0',
  `ms_teams_status` int DEFAULT '0',
  `telegram_status` int DEFAULT '0',
  `ntfy_sh_status` int DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `files` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `path` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `sha512` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `guid` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `password` varchar(255) DEFAULT NULL,
  `access` enum('Private','Public') CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `groups` (
  `id` int NOT NULL AUTO_INCREMENT,
  `tenant_id` int DEFAULT NULL,
  `location_id` int DEFAULT NULL,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `author` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `infrastructure_events` (
  `id` int NOT NULL AUTO_INCREMENT,
  `tenant_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `device_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT NULL,
  `reported_by` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `event` mediumtext COLLATE utf8mb4_unicode_ci,
  `description` mediumtext COLLATE utf8mb4_unicode_ci,
  `read` int DEFAULT '0',
  `log_id` varchar(10) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `type` int DEFAULT '0',
  `lang` int DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `jobs` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `author` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `platform` enum('Windows','System') COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `type` enum('PowerShell','MySQL') COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `script_id` int DEFAULT NULL,
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `locations` (
  `id` int NOT NULL AUTO_INCREMENT,
  `tenant_id` int NOT NULL DEFAULT '0',
  `guid` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `author` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `mail_notifications` (
  `id` int NOT NULL AUTO_INCREMENT,
  `mail_address` mediumtext,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `author` varchar(255) DEFAULT NULL,
  `description` varchar(255) CHARACTER SET latin1 COLLATE latin1_swedish_ci DEFAULT NULL,
  `severity` int DEFAULT '0',
  `tenants` mediumtext,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE IF NOT EXISTS `microsoft_teams_notifications` (
  `id` int NOT NULL AUTO_INCREMENT,
  `connector_name` mediumtext,
  `connector_url` mediumtext,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `author` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `severity` int DEFAULT '0',
  `tenants` mediumtext,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE IF NOT EXISTS `ntfy_sh_notifications` (
  `id` int NOT NULL AUTO_INCREMENT,
  `topic_name` mediumtext,
  `topic_url` mediumtext,
  `access_token` mediumtext,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `author` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `severity` int DEFAULT '0',
  `tenants` mediumtext,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE IF NOT EXISTS `performance_monitoring_ressources` (
  `id` int NOT NULL AUTO_INCREMENT,
  `tenant_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `location_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `device_name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT NULL,
  `type` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `performance_data` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `policies` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `author` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` mediumtext COLLATE utf8mb4_unicode_ci,
  `antivirus_settings` mediumtext COLLATE utf8mb4_unicode_ci,
  `antivirus_exclusions` mediumtext COLLATE utf8mb4_unicode_ci,
  `antivirus_scan_jobs` mediumtext COLLATE utf8mb4_unicode_ci,
  `antivirus_controlled_folder_access_folders` mediumtext COLLATE utf8mb4_unicode_ci,
  `sensors` mediumtext COLLATE utf8mb4_unicode_ci,
  `jobs` mediumtext COLLATE utf8mb4_unicode_ci,
  `operating_system` enum('Windows','Linux','macOS') COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `scripts` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `author` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `platform` enum('Windows','System') COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `shell` enum('PowerShell','MySQL') COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `script` mediumtext COLLATE utf8mb4_unicode_ci,
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `sensors` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `author` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `category` int DEFAULT NULL,
  `sub_category` int DEFAULT NULL,
  `severity` int DEFAULT NULL,
  `script_id` int DEFAULT NULL,
  `script_action_id` int DEFAULT NULL,
  `json` mediumtext COLLATE utf8mb4_unicode_ci,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `servers` (
  `id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(255) DEFAULT NULL,
  `ip_address` varchar(255) DEFAULT NULL,
  `domain` varchar(255) DEFAULT NULL,
  `os` varchar(255) DEFAULT NULL,
  `hearthbeat` datetime DEFAULT NULL,
  `appsettings` mediumtext,
  `cpu_usage` int DEFAULT NULL,
  `ram_usage` int DEFAULT NULL,
  `disk_usage` int DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS `settings` (
  `id` int NOT NULL AUTO_INCREMENT,
  `db_version` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `files_api_key` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `smtp` mediumtext COLLATE utf8mb4_unicode_ci,
  `package_provider_url` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `support_history` (
  `id` int NOT NULL AUTO_INCREMENT,
  `device_id` int DEFAULT NULL,
  `username` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `description` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `telegram_notifications` (
  `id` int NOT NULL AUTO_INCREMENT,
  `bot_name` mediumtext,
  `bot_token` mediumtext,
  `chat_id` mediumtext,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `author` varchar(255) DEFAULT NULL,
  `description` varchar(255) DEFAULT NULL,
  `severity` int DEFAULT '0',
  `tenants` mediumtext,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE IF NOT EXISTS `tenants` (
  `id` int NOT NULL AUTO_INCREMENT,
  `guid` varchar(255) DEFAULT NULL,
  `name` varchar(255) DEFAULT 'U3RhbmRhcmQ=',
  `description` varchar(255) DEFAULT NULL,
  `author` varchar(255) DEFAULT NULL,
  `date` datetime DEFAULT '2000-01-01 00:00:00',
  `company` varchar(255) DEFAULT NULL,
  `contact_person_one` varchar(255) DEFAULT NULL,
  `contact_person_two` varchar(255) DEFAULT NULL,
  `contact_person_three` varchar(255) DEFAULT NULL,
  `contact_person_four` varchar(255) DEFAULT NULL,
  `contact_person_five` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;";

                await Classes.MySQL.Handler.Execute_Command(script);

                Logging.Handler.Debug("Execute_Installation_Script", "Result", "Installation script executed successfully.");
                Console.WriteLine("Installation script executed successfully.");

                // Delete old settings
                await Classes.MySQL.Handler.Execute_Command("DELETE FROM settings;");

                // Add new settings
                await Classes.MySQL.Handler.Execute_Command("INSERT INTO settings (db_version, files_api_key, smtp) VALUES ('" + Application_Settings.version + "', '" + files_api_key + "', '" + smtp + "');");

                // Add admin user
                string password = BCrypt.Net.BCrypt.HashPassword("admin");
                string permissions = @"{
  ""dashboard_enabled"": true,
  ""devices_authorized_enabled"": true,
  ""devices_general"": true,
  ""devices_software"": true,
  ""devices_task_manager"": true,
  ""devices_antivirus"": true,
  ""devices_events"": true,
  ""devices_remote_shell"": true,
  ""devices_remote_file_browser"": true,
  ""devices_remote_control"": true,
  ""devices_deauthorize"": true,
  ""devices_move"": true,
  ""devices_unauthorized_enabled"": true,
  ""devices_unauthorized_authorize"": true,
  ""tenants_enabled"": true,
  ""tenants_add"": true,
  ""tenants_manage"": true,
  ""tenants_edit"": true,
  ""tenants_delete"": true,
  ""tenants_locations_add"": true,
  ""tenants_locations_manage"": true,
  ""tenants_locations_edit"": true,
  ""tenants_locations_delete"": true,
  ""tenants_groups_add"": true,
  ""tenants_groups_edit"": true,
  ""tenants_groups_delete"": true,
  ""automation_enabled"": true,
  ""automation_add"": true,
  ""automation_edit"": true,
  ""automation_delete"": true,
  ""policies_enabled"": true,
  ""policies_add"": true,
  ""policies_manage"": true,
  ""policies_edit"": true,
  ""policies_delete"": true,
  ""collections_enabled"": true,
  ""collections_antivirus_controlled_folder_access_enabled"": true,
  ""collections_antivirus_controlled_folder_access_add"": true,
  ""collections_antivirus_controlled_folder_access_manage"": true,
  ""collections_antivirus_controlled_folder_access_edit"": true,
  ""collections_antivirus_controlled_folder_access_delete"": true,
  ""collections_antivirus_controlled_folder_access_processes_add"": true,
  ""collections_antivirus_controlled_folder_access_processes_edit"": true,
  ""collections_antivirus_controlled_folder_access_processes_delete"": true,
  ""collections_sensors_enabled"": true,
  ""collections_sensors_add"": true,
  ""collections_sensors_edit"": true,
  ""collections_sensors_delete"": true,
  ""collections_scripts_enabled"": true,
  ""collections_scripts_add"": true,
  ""collections_scripts_edit"": true,
  ""collections_scripts_delete"": true,
  ""collections_jobs_enabled"": true,
  ""collections_jobs_add"": true,
  ""collections_jobs_edit"": true,
  ""collections_jobs_delete"": true,
  ""collections_files_enabled"": true,
  ""collections_files_add"": true,
  ""collections_files_edit"": true,
  ""collections_files_delete"": true,
  ""collections_files_netlock"": true,
  ""events_enabled"": true,
  ""users_enabled"": true,
  ""users_add"": true,
  ""users_manage"": true,
  ""users_edit"": true,
  ""users_delete"": true,
  ""settings_enabled"": true,
  ""settings_notifications_enabled"": true,
  ""settings_notifications_mail_enabled"": true,
  ""settings_notifications_mail_add"": true,
  ""settings_notifications_mail_smtp"": true,
  ""settings_notifications_mail_test"": true,
  ""settings_notifications_mail_edit"": true,
  ""settings_notifications_mail_delete"": true,
  ""settings_notifications_microsoft_teams_enabled"": true,
  ""settings_notifications_microsoft_teams_add"": true,
  ""settings_notifications_microsoft_teams_test"": true,
  ""settings_notifications_microsoft_teams_edit"": true,
  ""settings_notifications_microsoft_teams_delete"": true,
  ""settings_notifications_telegram_enabled"": true,
  ""settings_notifications_telegram_add"": true,
  ""settings_notifications_telegram_test"": true,
  ""settings_notifications_telegram_edit"": true,
  ""settings_notifications_telegram_delete"": true,
  ""settings_notifications_ntfysh_enabled"": true,
  ""settings_notifications_ntfysh_add"": true,
  ""settings_notifications_ntfysh_test"": true,
  ""settings_notifications_ntfysh_edit"": true,
  ""settings_notifications_ntfysh_delete"": true,
  ""settings_system_enabled"": true,
  ""settings_protocols_enabled"": true
}";

                await Classes.MySQL.Handler.Execute_Command("INSERT INTO accounts (username, password, reset_password, role, permissions, tenants) VALUES ('admin', '" + password + "', 1, 'Administrator', '" + permissions + "', '[]');");

                Logging.Handler.Debug("Execute_Installation_Script", "Result", "Admin user added successfully.");
                Console.WriteLine("Admin user added successfully.");
                Console.WriteLine("Username: admin");
                Console.WriteLine("Password: admin");

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Execute_Installation_Script", "Result", ex.ToString());
                return false;
            }
        }
    }
}
