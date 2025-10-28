using NetLock_RMM_Agent_Comm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Global.Helper;
using Microsoft.Data.Sqlite;

namespace Global.Offline_Mode
{
    internal class Handler
    {
        public static void Policy()
        {
            Device_Worker.sync_active = true;
            bool error = false;

            Logging.Debug("Global.Offline_Mode.Handler.Policy", "Start", "");

            try
            {
                using (SqliteConnection db_conn = new SqliteConnection(Application_Settings.NetLock_Data_Database_String))
                {
                    db_conn.Open();

                    SqliteCommand scdCommand = new SqliteCommand("SELECT * FROM policy;", db_conn);
                    SqliteDataReader reader = scdCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        //policy_antivirus_settings_json
                        try
                        {
                            Windows_Worker.policy_antivirus_settings_json = reader["antivirus_settings_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Global.Offline_Mode.Handler.Policy", "Read failed (antivirus_settings_json). Because of the following error, online settings will be requested.", ex.ToString());
                        }

                        //policy_antivirus_exclusions_json
                        try
                        {
                            Windows_Worker.policy_antivirus_exclusions_json = reader["antivirus_exclusions_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Global.Offline_Mode.Handler.Policy", "Read failed (antivirus_exclusions_json). Because of the following error, online settings will be requested.", ex.ToString());
                        }

                        //antivirus_scan_jobs_json
                        try
                        {
                            Windows_Worker.policy_antivirus_scan_jobs_json = reader["antivirus_scan_jobs_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Global.Offline_Mode.Handler.Policy", "Read failed (antivirus_scan_jobs_json). Because of the following error, online settings will be requested.", ex.ToString());
                        }

                        //antivirus_controlled_folder_access_folders_json
                        try
                        {
                            Windows_Worker.policy_antivirus_controlled_folder_access_folders_json = reader["antivirus_controlled_folder_access_folders_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Global.Offline_Mode.Handler.Policy", "Read failed (antivirus_controlled_folder_access_folders_json). Because of the following error, online settings will be requested.", ex.ToString());
                        }

                        //antivirus_controlled_folder_access_ruleset_json
                        try
                        {
                            Windows_Worker.policy_antivirus_controlled_folder_access_ruleset_json = reader["antivirus_controlled_folder_access_ruleset_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Global.Offline_Mode.Handler.Policy", "Read failed (antivirus_controlled_folder_access_ruleset_json). Because of the following error, online settings will be requested.", ex.ToString());
                        }

                        //sensors_json
                        try
                        {
                            Device_Worker.policy_sensors_json = reader["sensors_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Global.Offline_Mode.Handler.Policy", "Read failed (sensors_json). Because of the following error, online settings will be requested.", ex.Message);
                        }

                        //jobs_json
                        try
                        {
                            Device_Worker.policy_jobs_json = reader["jobs_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Global.Offline_Mode.Handler.Policy", "Read failed (jobs_json). Because of the following error, online settings will be requested.", ex.Message);
                        }
                        
                        // tray_icon_settings_json
                        try
                        {
                            Device_Worker.policy_tray_icon_settings_json = reader["tray_icon_settings_json"].ToString();
                            
                            // Write to disk for the tray icon process to read it
                            File.WriteAllText(Application_Paths.tray_icon_settings_json_path, Encryption.String_Encryption.Encrypt(Device_Worker.policy_tray_icon_settings_json, Application_Settings.NetLock_Local_Encryption_Key));
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Global.Offline_Mode.Handler.Policy", "Read failed (tray_icon_settings_json). Because of the following error, online settings will be requested.", ex.Message);
                        }
                        
                        // agent_settings_json
                        try
                        {
                            Device_Worker.policy_agent_settings_json = reader["agent_settings_json"].ToString();
                            
                            // Write the agent settings json to disk for the remote process to read it
                            File.WriteAllText(Application_Paths.agent_settings_json_path, Encryption.String_Encryption.Encrypt(Device_Worker.policy_agent_settings_json, Application_Settings.NetLock_Local_Encryption_Key));
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Global.Offline_Mode.Handler.Policy", "Read failed (agent_settings_json). Because of the following error, online settings will be requested.", ex.Message);
                        }
                    }

                    reader.Close();
                    db_conn.Close();
                    db_conn.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Global.Offline_Mode.Handler.Policy", "Read. Because of the following error, online settings will be requested.", ex.Message);
                error = true;
            }

            //If any error occured. Get Online Settings
            if (error)
            {
                Logging.Error("Global.Offline_Mode.Handler.Policy", "A clean service restart will be performed.", "");

                // Should be reworked for linux. Maybe rebuilding the database?
                if (OperatingSystem.IsWindows())
                    Initialization.Health.Clean_Service_Restart();
            }

            Device_Worker.sync_active = false;

            Logging.Debug("Global.Offline_Mode.Handler.Policy", "Stop", "");
        }

        public class AgentSettings
        {
            public bool AutoUpdateEnabled { get; set; }
            public int SyncInterval { get; set; }
        }
        
        public static void LoadAgentSettings()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Device_Worker.policy_agent_settings_json))
                    throw new Exception("policy_agent_settings_json is null or empty");

                var agentSettings = JsonSerializer.Deserialize<AgentSettings>(Device_Worker.policy_agent_settings_json);

                if (agentSettings != null)
                {
                    Device_Worker.agentAutoUpdateEnabled = agentSettings.AutoUpdateEnabled;
                    Device_Worker.sync_timer.Interval = agentSettings.SyncInterval * 60 * 1000;
                    Logging.Debug("Global.Offline_Mode.Handler.LoadAgentSettings", "Agent settings loaded", $"AutoUpdateEnabled: {Device_Worker.agentAutoUpdateEnabled}, SyncInterval: {Device_Worker.sync_timer.Interval}");
                }
                else
                {
                    Device_Worker.sync_timer .Interval = 30 * 60 * 1000; // Default to 30 minutes
                }
            }
            catch (Exception e)
            {
                Logging.Error("Global.Offline_Mode.Handler.LoadAgentSettings", "Error loading agent settings, using defaults", e.ToString()); 
                Device_Worker.sync_timer.Interval = 30 * 60 * 1000; // Default to 30 minutes
            }
        }
    }
}
