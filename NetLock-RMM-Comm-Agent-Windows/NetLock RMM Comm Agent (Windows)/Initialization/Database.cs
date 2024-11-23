using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;

namespace NetLock_RMM_Comm_Agent_Windows.Initialization
{
    internal class Database
    {
        public static void NetLock_Data_Setup()
        {
            string data_db_path = Application_Paths.program_data_netlock_policy_database;

            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                //Delete Data DB
                while (File.Exists(data_db_path))
                {
                    try
                    {
                        File.Delete(data_db_path);

                        Logging.Handler.Debug("Initialization.Database.NetLock_Data_Setup", "check_db", "DB deleted.");
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Initialization.Database.NetLock_Data_Setup", "check_db", "Failed deleting DB: " + ex.Message);
                    }

                    Thread.Sleep(500);
                }

                Thread.Sleep(2500);

                //Create Data DB
                try
                {
                    if (File.Exists(data_db_path) == false)
                    {
                        SQLiteConnection.CreateFile(data_db_path);
                        Logging.Handler.Debug("Initialization.Database.NetLock_Data_Setup", "create_db (data)", "DB created.");
                    }
                    else
                        Logging.Handler.Debug("Initialization.Database.NetLock_Data_Setup", "create_db (data)", "DB still existing.");
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Initialization.Database.NetLock_Data_Setup", "create_db (data)", ex.Message);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();

                Thread.Sleep(2500);

                //Connect to Data DB
                try
                {
                    using (SQLiteConnection db_conn = new SQLiteConnection(Application_Settings.NetLock_Data_Database_String))
                    {
                        db_conn.Open();

                        //Create Client Settings Table
                        string policy_table_sql = "CREATE TABLE policy(" +
                            "id INTEGER PRIMARY KEY, " +
                            "antivirus_settings_json TEXT NULL DEFAULT NULL," +
                            "antivirus_exclusions_json TEXT NULL DEFAULT NULL," +
                            "antivirus_scan_jobs_json TEXT NULL DEFAULT NULL," +
                            "antivirus_controlled_folder_access_folders_json TEXT NULL DEFAULT NULL," +
                            "antivirus_controlled_folder_access_ruleset_json TEXT NULL DEFAULT NULL," +
                            "sensors_json TEXT NULL DEFAULT NULL," +
                            "jobs_json TEXT NULL DEFAULT NULL" +

                            ");";

                        SQLiteCommand policy_table_command = new SQLiteCommand(policy_table_sql, db_conn);
                        policy_table_command.ExecuteNonQuery();

                        /*
                        //Create Application Control Table
                        string application_control_ruleset_table_sql = "CREATE TABLE application_control_ruleset(" +
                            "id INTEGER PRIMARY KEY, " +
                            "content TEXT NULL DEFAULT NULL);";

                        SQLiteCommand application_control_ruleset_table_command = new SQLiteCommand(application_control_ruleset_table_sql, db_conn);
                        application_control_ruleset_table_command.ExecuteNonQuery();

                        //Create Device Control Whitelist Table
                        string device_control_whitelist_table_sql = "CREATE TABLE device_control_whitelist(" +
                            "id INTEGER PRIMARY KEY, " +
                            "device_type TEXT NULL DEFAULT NULL, " +
                            "device_id TEXT NULL DEFAULT NULL);";

                        SQLiteCommand device_control_whitelist_table_command = new SQLiteCommand(device_control_whitelist_table_sql, db_conn);
                        device_control_whitelist_table_command.ExecuteNonQuery();

                        //Create Microsoft Defender AntiVirus Controlled Folder Access Whitelist Table
                        string msdav_cfa_ruleset_table_sql = "CREATE TABLE msdav_cfa_ruleset(" +
                            "id INTEGER PRIMARY KEY, " +
                            "file_path TEXT NULL DEFAULT NULL);";

                        SQLiteCommand msdav_cfa_ruleset_table_command = new SQLiteCommand(msdav_cfa_ruleset_table_sql, db_conn);
                        msdav_cfa_ruleset_table_command.ExecuteNonQuery();

                        //Create Sensor Management ruleset table
                        string sensor_management_ruleset_table_sql = "CREATE TABLE sensor_management_ruleset(" +
                            "id INTEGER PRIMARY KEY, " +
                            "name TEXT NULL DEFAULT NULL, " +
                            "description TEXT NULL DEFAULT NULL, " +
                            "type TEXT NULL DEFAULT NULL, " +
                            "content TEXT NULL DEFAULT NULL);";

                        SQLiteCommand sensor_management_ruleset_table_command = new SQLiteCommand(sensor_management_ruleset_table_sql, db_conn);
                        sensor_management_ruleset_table_command.ExecuteNonQuery();

                        //Create Windows Defender Firewall ruleset table
                        string wdfw_ruleset_table_sql = "CREATE TABLE wdfw_ruleset(" +
                            "id INTEGER PRIMARY KEY, " +
                            "content TEXT NULL DEFAULT NULL, " +
                            "rule_id TEXT NULL DEFAULT NULL);";

                        SQLiteCommand wdfw_ruleset_table_command = new SQLiteCommand(wdfw_ruleset_table_sql, db_conn);
                        wdfw_ruleset_table_command.ExecuteNonQuery();


                        //Create environment users table
                        string environment_users_table_sql = "CREATE TABLE environment_users(" +
                            "id INTEGER PRIMARY KEY, " +
                            "username TEXT NULL DEFAULT NULL);";

                        SQLiteCommand environment_users_table_command = new SQLiteCommand(environment_users_table_sql, db_conn);
                        environment_users_table_command.ExecuteNonQuery();
                        */

                        db_conn.Close();
                        db_conn.Dispose();

                        Logging.Handler.Debug("Initialization.Database.NetLock_Data_Setup", "check_db", "Filled DB info.");
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Initialization.Database.NetLock_Data_Setup", "create_application_control_ruleset", ex.Message);
                    NetLock_Data_Setup();
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Initialization.Database.NetLock_Data_Setup", "check_db", ex.Message);
                NetLock_Data_Setup();
            }
        }

        public static void NetLock_Events_Setup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            string report_db_path = Application_Paths.program_data_netlock_events_database;

            //Create Reports DB
            try
            {
                if (!File.Exists(report_db_path))
                {
                    SQLiteConnection.CreateFile(report_db_path);
                    Logging.Handler.Debug("Database_Setup", "create_db (events)", "DB created.");

                    Thread.Sleep(2500);

                    using (SQLiteConnection db_conn = new SQLiteConnection(Application_Settings.NetLock_Events_Database_String))
                    {
                        db_conn.Open();

                        //Create Security Events Table
                        string events_table_sql = "CREATE TABLE events(" +
                            "id INTEGER PRIMARY KEY, " +
                            "severity TEXT NULL DEFAULT NULL, " +
                            "reported_by TEXT NULL DEFAULT NULL, " +
                            "event TEXT NULL DEFAULT NULL, " +
                            "description TEXT NULL DEFAULT NULL, " +
                            "notification_json TEXT NULL DEFAULT NULL," +
                            "type TEXT NULL DEFAULT NULL, " +
                            "language TEXT NULL DEFAULT NULL," +
                            "status TEXT NULL DEFAULT NULL);";

                        SQLiteCommand command = new SQLiteCommand(events_table_sql, db_conn);
                        command.ExecuteNonQuery();

                        db_conn.Close();
                        db_conn.Dispose();

                        Logging.Handler.Debug("Initialization.Database.NetLock_Events_Setup", "check_db (events)", "Created events table.");
                    }
                }
                else
                    Logging.Handler.Debug("Initialization.Database.NetLock_Events_Setup", "create_db (events)", "Rep DB still existing.");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Initialization.Database.NetLock_Events_Setup", "create_db general (events)", ex.Message);
            }
        }
    }
}
