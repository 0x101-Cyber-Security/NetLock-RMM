using NetLock_RMM_Agent_Comm;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Global.Helper;
using Windows.Workers;

namespace Global.Offline_Mode
{
    internal class Handler
    {
        public static void Policy()
        {
            Device_Worker.sync_active = true;
            bool error = false;

            Logging.Debug("Offline_Mode.Handler.Policy", "Start", "");

            try
            {
                using (SQLiteConnection db_conn = new SQLiteConnection(Application_Settings.NetLock_Data_Database_String))
                {
                    db_conn.Open();

                    SQLiteCommand scdCommand = new SQLiteCommand("SELECT * FROM policy;", db_conn);
                    SQLiteDataReader reader = scdCommand.ExecuteReader();

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
                            Logging.Error("Offline_Mode.Handler.Policy", "Read failed (antivirus_settings_json). Because of the following error, online settings will be requested.", ex.ToString());
                        }

                        //policy_antivirus_exclusions_json
                        try
                        {
                            Windows_Worker.policy_antivirus_exclusions_json = reader["antivirus_exclusions_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Offline_Mode.Handler.Policy", "Read failed (antivirus_exclusions_json). Because of the following error, online settings will be requested.", ex.ToString());
                        }

                        //antivirus_scan_jobs_json
                        try
                        {
                            Windows_Worker.policy_antivirus_scan_jobs_json = reader["antivirus_scan_jobs_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Offline_Mode.Handler.Policy", "Read failed (antivirus_scan_jobs_json). Because of the following error, online settings will be requested.", ex.ToString());
                        }

                        //antivirus_controlled_folder_access_folders_json
                        try
                        {
                            Windows_Worker.policy_antivirus_controlled_folder_access_folders_json = reader["antivirus_controlled_folder_access_folders_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Offline_Mode.Handler.Policy", "Read failed (antivirus_controlled_folder_access_folders_json). Because of the following error, online settings will be requested.", ex.ToString());
                        }

                        //antivirus_controlled_folder_access_ruleset_json
                        try
                        {
                            Windows_Worker.policy_antivirus_controlled_folder_access_ruleset_json = reader["antivirus_controlled_folder_access_ruleset_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Offline_Mode.Handler.Policy", "Read failed (antivirus_controlled_folder_access_ruleset_json). Because of the following error, online settings will be requested.", ex.ToString());
                        }

                        //sensors_json
                        try
                        {
                            Windows_Worker.policy_sensors_json = reader["sensors_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Offline_Mode.Handler.Policy", "Read failed (sensors_json). Because of the following error, online settings will be requested.", ex.Message);
                        }

                        //jobs_json
                        try
                        {
                            Windows_Worker.policy_jobs_json = reader["jobs_json"].ToString();
                        }
                        catch (Exception ex)
                        {
                            error = true;
                            Logging.Error("Offline_Mode.Handler.Policy", "Read failed (jobs_json). Because of the following error, online settings will be requested.", ex.Message);
                        }
                    }

                    reader.Close();
                    db_conn.Close();
                    db_conn.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Offline_Mode.Handler.Policy", "Read. Because of the following error, online settings will be requested.", ex.Message);
                error = true;
            }

            //If any error occured. Get Online Settings
            if (error)
            {
                Logging.Error("Offline_Mode.Handler.Policy", "A clean service restart will be performed.", "");
                Initialization.Health.Clean_Service_Restart();
            }

            Device_Worker.sync_active = false;

            Logging.Debug("Offline_Mode.Handler.Policy", "Stop", "");
        }
    }
}
