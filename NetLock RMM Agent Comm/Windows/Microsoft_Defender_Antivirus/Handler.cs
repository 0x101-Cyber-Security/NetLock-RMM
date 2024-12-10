using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System.Timers;
using Global.Helper;
using System.IO;
using NetLock_RMM_Agent_Comm;

namespace Windows.Microsoft_Defender_Antivirus
{
    internal class Handler
    {
        public static void Initalization()
        {
            //If enabled, apply managed settings
            try
            {
                if (!String.IsNullOrEmpty(Windows_Worker.policy_antivirus_settings_json) && !String.IsNullOrEmpty(Windows_Worker.policy_antivirus_scan_jobs_json) && !String.IsNullOrEmpty(Windows_Worker.policy_antivirus_exclusions_json))
                {
                    bool enabled = false;
                    bool security_center_tray = false;

                    using (JsonDocument document = JsonDocument.Parse(Windows_Worker.policy_antivirus_settings_json))
                    {
                        JsonElement enabled_element = document.RootElement.GetProperty("enabled");
                        enabled = Convert.ToBoolean(enabled_element.ToString());

                        JsonElement security_center_tray_element = document.RootElement.GetProperty("security_center_tray");
                        security_center_tray = Convert.ToBoolean(security_center_tray_element.ToString());
                    }

                    if (enabled)
                    {
                        //Check if tray icon should be displayed
                        if (!security_center_tray)
                            Kill_Security_Center_Tray_Icon();


                        Windows_Worker.microsoft_defender_antivirus_notifications_json = Get_Notifications_Json();

                        Set_Settings.Do();
                        Eventlog_Crawler.Do();
                        Scan_Jobs_Scheduler.Check_Execution();
                    }
                    else //If not, restore windows defender standard config
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Microsoft_Defender_AntiVirus.Handler.Initalization", "General Error", ex.ToString());
            }
        }

        public static void Kill_Security_Center_Tray_Icon()
        {
            if (Process.GetProcessesByName("SecurityHealthSystray").Length > 0)
            {
                Logging.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Handler.Initalization", "Check security center tray icon presence", "Is present. Kill it...");

                try
                {
                    Process cmd_process = new Process();
                    cmd_process.StartInfo.UseShellExecute = true;
                    cmd_process.StartInfo.FileName = "cmd.exe";
                    cmd_process.StartInfo.Arguments = "/c taskkill /F /IM SecurityHealthSystray.exe";
                    cmd_process.Start();
                    cmd_process.WaitForExit();

                    Logging.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Handler.Initalization", "Check security center tray icon presence", "Killed successfully.");
                }
                catch (Exception ex)
                {
                    Logging.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Handler.Initalization", "Check security center tray icon presence", "Couldn't kill it: " + ex.ToString());
                }
            }
        }

        public static async void Events_Tick(object source, ElapsedEventArgs e)
        {
            Eventlog_Crawler.EDR();

            // NetLock legacy code. Will be worked in future

            //Only do if sensor management ruleset is assigned
            /*if (NetLock_Agent_Service.sensor_management_ruleset != "LQ==")
            {
                Logging.Logging.incident_response("Microsoft_Defender_AntiVirus.Handler.MSDAV_Events_Tick", "status", "ruleset assigned, execute");

                //crawler runs already, return
                if (NetLock_Agent_Service.microsoft_defender_antivirus_crawling)
                {
                    Logging.Logging.incident_response("Microsoft_Defender_AntiVirus.Handler.MSDAV_Events_Tick", "status", "already crawling, abort");
                    return;
                }

                NetLock_Agent_Service.microsoft_defender_antivirus_crawling = true;

                Eventlog_Crawler.Eventlog_Crawler.EDR();

                NetLock_Agent_Service.microsoft_defender_antivirus_crawling = false;
            }
            else
                Logging.Logging.incident_response("Microsoft_Defender_AntiVirus.Handler.MSDAV_Events_Tick", "status", "no ruleset assigned");
            */
        }

        public static async void Check_Hourly_Sig_Updates_Tick(object source, ElapsedEventArgs e)
        {
            Check_Hourly_Sig_Updates();
        }

        public static async void Scan_Job_Scheduler_Tick(object source, ElapsedEventArgs e)
        {
            Scan_Jobs_Scheduler.Check_Execution();
        }

        public static void Check_Hourly_Sig_Updates()
        {
            try
            {
                bool check_hourly_signatures = false;

                using (JsonDocument document = JsonDocument.Parse(Windows_Worker.policy_antivirus_settings_json))
                {
                    JsonElement check_hourly_signatures_element = document.RootElement.GetProperty("check_hourly_signatures");
                    check_hourly_signatures = Convert.ToBoolean(check_hourly_signatures_element.ToString());
                }

                if (check_hourly_signatures)
                {
                    Logging.Microsoft_Defender_Antivirus("Microsoft_Defender_Antivirus.Check_Hourly_Sig_Updates", "Execute", "Before");
                    Helper.PowerShell.Execute_Command("Microsoft_Defender_Antivirus.Check_Hourly_Sig_Updates", "Update-MpSignature", 60);
                    Logging.Microsoft_Defender_Antivirus("Microsoft_Defender_Antivirus.Check_Hourly_Sig_Updates", "Execute", "After");
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Microsoft_Defender_AntiVirus.Handler.Microsoft_Defender_Check_Hourly_Sig_Updates", "General Error", ex.ToString());
            }
        }

        public static void Backup_Eventlog()
        {
            try
            {
                string path = @"C:\Windows\System32\winevt\Logs\Microsoft-Windows-Windows Defender%4Operational.evtx";
                string backup_path = Application_Paths.program_data_microsoft_defender_antivirus_eventlog_backup;

                if (File.Exists(path))
                {
                    File.Delete(backup_path);
                    File.Copy(path, backup_path);
                }

                Logging.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Handler.Backup_Eventlog", "Status", "Done.");
            }
            catch (Exception ex)
            {
                Logging.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Handler.Backup_Eventlog", "Couldnt backup it.", ex.ToString());
            }
        }

        public static string Get_Notifications_Json()
        {
            try
            {
                bool notifications_netlock_mail = false;
                bool notifications_netlock_microsoft_teams = false;
                bool notifications_netlock_telegram = false;
                bool notifications_netlock_ntfy_sh = false;

                using (JsonDocument document = JsonDocument.Parse(Windows_Worker.policy_antivirus_settings_json))
                {
                    JsonElement notifications_netlock_mail_element = document.RootElement.GetProperty("notifications_netlock_mail");
                    notifications_netlock_mail = notifications_netlock_mail_element.GetBoolean();

                    JsonElement notifications_netlock_microsoft_teams_element = document.RootElement.GetProperty("notifications_netlock_microsoft_teams");
                    notifications_netlock_microsoft_teams = notifications_netlock_microsoft_teams_element.GetBoolean();

                    JsonElement notifications_netlock_telegram_element = document.RootElement.GetProperty("notifications_netlock_telegram");
                    notifications_netlock_telegram = notifications_netlock_telegram_element.GetBoolean();

                    JsonElement notifications_netlock_ntfy_sh_element = document.RootElement.GetProperty("notifications_netlock_ntfy_sh");
                    notifications_netlock_ntfy_sh = notifications_netlock_ntfy_sh_element.GetBoolean();
                }

                // Create notifications_json
                string notifications_json = "{\"mail\":" + notifications_netlock_mail.ToString().ToLower() + ",\"microsoft_teams\":" + notifications_netlock_microsoft_teams.ToString().ToLower() + ",\"telegram\":" + notifications_netlock_telegram.ToString().ToLower() + ",\"ntfy_sh\":" + notifications_netlock_ntfy_sh.ToString().ToLower() + "}";

                Logging.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Handler.Get_Notifications_Json", "Notifications Json", notifications_json);

                return notifications_json;
            }
            catch (Exception ex)
            {
                Logging.Error("Microsoft_Defender_AntiVirus.Handler.Get_Notifications_Json", "General Error", ex.ToString());
                return String.Empty;
            }
        }
    }
}
