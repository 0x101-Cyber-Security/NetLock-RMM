using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Threading;
using NetLock_Agent.Helper;
using Microsoft.Win32;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace NetLock_RMM_Comm_Agent_Windows.Microsoft_Defender_Antivirus
{
    internal class Eventlog_Crawler
    {
        public static void Do()
        {
            Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Eventlog_Crawler.Do", "Start", "");
            
            //EDR crawler running, wait a little and retry
            /*while (Service.microsoft_defender_antivirus_events_crawling)
            {
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Eventlog_Crawler.Do", "read values", "");
                Thread.Sleep(1000);
            }

            Service.microsoft_defender_antivirus_events_crawling = true;*/

            Check_Registry();

            MALWAREPROTECTION_RTP_ENABLED();
            MALWAREPROTECTION_RTP_DISABLED();
            MALWAREPROTECTION_SIGNATURE_UPDATED();
            MALWAREPROTECTION_STATE_MALWARE_DETECTED();
            MALWAREPROTECTION_MALWARE_ACTION_TAKEN();
            MALWAREPROTECTION_MALWARE_DETECTED();
            MALWAREPROTECTION_SCAN_COMPLETED();
            MALWAREPROTECTION_SCAN_CANCELLED();
            MALWAREPROTECTION_SCAN_PAUSED();
            MALWAREPROTECTION_SCAN_FAILED();
            MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED();
            MALWAREPROTECTION_QUARANTINE_RESTORE();
            MALWAREPROTECTION_QUARANTINE_DELETE();
            MALWAREPROTECTION_MALWARE_HISTORY_DELETE();
            MALWAREPROTECTION_BEHAVIOR_DETECTED();
            MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN();
            MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK();
            MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED();
            MALWAREPROTECTION_SIGNATURE_REVERSION();
            MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE();
            MALWAREPROTECTION_PLATFORM_UPDATE_FAILED();
            MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE();
            MALWAREPROTECTION_OS_EXPIRING();
            MALWAREPROTECTION_OS_EOL();
            MALWAREPROTECTION_PROTECTION_EOL();
            MALWAREPROTECTION_RTP_FEATURE_FAILURE();
            MALWAREPROTECTION_RTP_FEATURE_RECOVERED();
            MALWAREPROTECTION_RTP_FEATURE_CONFIGURED();
            //MALWAREPROTECTION_CONFIG_CHANGED(); Spams config
            MALWAREPROTECTION_ENGINE_FAILURE();
            MALWAREPROTECTION_ANTISPYWARE_ENABLED();
            MALWAREPROTECTION_ANTISPYWARE_DISABLED();
            MALWAREPROTECTION_ANTIVIRUS_ENABLED();
            MALWAREPROTECTION_ANTIVIRUS_DISABLED();
            TAMPER_PROTECTION_BLOCKED_CHANGES();
            MALWAREPROTECTION_EXPIRATION_WARNING_STATE();
            MALWAREPROTECTION_DISABLED_EXPIRED_STATE();
            CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION();
            //Delete_Eventlog();

            //Service.microsoft_defender_antivirus_events_crawling = false;

            Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Eventlog_Crawler.Do", "Stop", "");
        }

        public static void EDR()
        {
            Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Eventlog_Crawler.EDR", "Start", "");

            Check_Registry();

            //Decrypted reg keys for performance reason
            MALWAREPROTECTION_STATE_MALWARE_DETECTED();
            MALWAREPROTECTION_MALWARE_ACTION_TAKEN();
            MALWAREPROTECTION_MALWARE_DETECTED();
            MALWAREPROTECTION_BEHAVIOR_DETECTED();
            MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN();

            Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Eventlog_Crawler.EDR", "Stop", "");
        }

        public static void Delete_Eventlog()
        {
            string path = @"C:\Windows\System32\winevt\Logs\Microsoft-Windows-Windows Defender%4Operational.evtx";

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.Delete_Eventlog", "Couldnt delete event log.", ex.ToString());
            }
        }

        public static void Check_Registry()
        {
            try
            {
                //Check if registry values can be read, if not, rebuild
                try
                {
                    // MALWAREPROTECTION_RTP_ENABLED
                    string MALWAREPROTECTION_RTP_ENABLED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_ENABLED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_RTP_ENABLED))
                    {
                        MALWAREPROTECTION_RTP_ENABLED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_ENABLED", MALWAREPROTECTION_RTP_ENABLED);
                    }

                    // MALWAREPROTECTION_RTP_DISABLED
                    string MALWAREPROTECTION_RTP_DISABLED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_DISABLED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_RTP_DISABLED))
                    {
                        MALWAREPROTECTION_RTP_DISABLED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_DISABLED", MALWAREPROTECTION_RTP_DISABLED);
                    }

                    // MALWAREPROTECTION_SIGNATURE_UPDATED
                    string MALWAREPROTECTION_SIGNATURE_UPDATED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_UPDATED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_SIGNATURE_UPDATED))
                    {
                        MALWAREPROTECTION_SIGNATURE_UPDATED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_UPDATED", MALWAREPROTECTION_SIGNATURE_UPDATED);
                    }

                    // MALWAREPROTECTION_STATE_MALWARE_DETECTED
                    string MALWAREPROTECTION_STATE_MALWARE_DETECTED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_STATE_MALWARE_DETECTED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_STATE_MALWARE_DETECTED))
                    {
                        MALWAREPROTECTION_STATE_MALWARE_DETECTED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_STATE_MALWARE_DETECTED", MALWAREPROTECTION_STATE_MALWARE_DETECTED);
                    }

                    // MALWAREPROTECTION_MALWARE_ACTION_TAKEN
                    string MALWAREPROTECTION_MALWARE_ACTION_TAKEN = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_ACTION_TAKEN");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_MALWARE_ACTION_TAKEN))
                    {
                        MALWAREPROTECTION_MALWARE_ACTION_TAKEN = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_ACTION_TAKEN", MALWAREPROTECTION_MALWARE_ACTION_TAKEN);
                    }

                    // MALWAREPROTECTION_MALWARE_DETECTED
                    string MALWAREPROTECTION_MALWARE_DETECTED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_DETECTED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_MALWARE_DETECTED))
                    {
                        MALWAREPROTECTION_MALWARE_DETECTED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_DETECTED", MALWAREPROTECTION_MALWARE_DETECTED);
                    }

                    // MALWAREPROTECTION_SCAN_COMPLETED
                    string MALWAREPROTECTION_SCAN_COMPLETED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_COMPLETED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_SCAN_COMPLETED))
                    {
                        MALWAREPROTECTION_SCAN_COMPLETED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_COMPLETED", MALWAREPROTECTION_SCAN_COMPLETED);
                    }

                    // MALWAREPROTECTION_SCAN_CANCELLED
                    string MALWAREPROTECTION_SCAN_CANCELLED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_CANCELLED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_SCAN_CANCELLED))
                    {
                        MALWAREPROTECTION_SCAN_CANCELLED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_CANCELLED", MALWAREPROTECTION_SCAN_CANCELLED);
                    }

                    // MALWAREPROTECTION_SCAN_PAUSED
                    string MALWAREPROTECTION_SCAN_PAUSED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_PAUSED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_SCAN_PAUSED))
                    {
                        MALWAREPROTECTION_SCAN_PAUSED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_PAUSED", MALWAREPROTECTION_SCAN_PAUSED);
                    }

                    // MALWAREPROTECTION_SCAN_FAILED
                    string MALWAREPROTECTION_SCAN_FAILED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_FAILED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_SCAN_FAILED))
                    {
                        MALWAREPROTECTION_SCAN_FAILED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_FAILED", MALWAREPROTECTION_SCAN_FAILED);
                    }

                    // MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED
                    string MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED))
                    {
                        MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED", MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED);
                    }

                    // MALWAREPROTECTION_QUARANTINE_RESTORE
                    string MALWAREPROTECTION_QUARANTINE_RESTORE = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_QUARANTINE_RESTORE");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_QUARANTINE_RESTORE))
                    {
                        MALWAREPROTECTION_QUARANTINE_RESTORE = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_QUARANTINE_RESTORE", MALWAREPROTECTION_QUARANTINE_RESTORE);
                    }

                    // MALWAREPROTECTION_QUARANTINE_DELETE
                    string MALWAREPROTECTION_QUARANTINE_DELETE = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_QUARANTINE_DELETE");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_QUARANTINE_DELETE))
                    {
                        MALWAREPROTECTION_QUARANTINE_DELETE = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_QUARANTINE_DELETE", MALWAREPROTECTION_QUARANTINE_DELETE);
                    }

                    // MALWAREPROTECTION_MALWARE_HISTORY_DELETE
                    string MALWAREPROTECTION_MALWARE_HISTORY_DELETE = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_HISTORY_DELETE");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_MALWARE_HISTORY_DELETE))
                    {
                        MALWAREPROTECTION_MALWARE_HISTORY_DELETE = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_HISTORY_DELETE", MALWAREPROTECTION_MALWARE_HISTORY_DELETE);
                    }

                    // MALWAREPROTECTION_BEHAVIOR_DETECTED
                    string MALWAREPROTECTION_BEHAVIOR_DETECTED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_BEHAVIOR_DETECTED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_BEHAVIOR_DETECTED))
                    {
                        MALWAREPROTECTION_BEHAVIOR_DETECTED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_BEHAVIOR_DETECTED", MALWAREPROTECTION_BEHAVIOR_DETECTED);
                    }

                    // MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN
                    string MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN))
                    {
                        MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN", MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN);
                    }

                    // MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK
                    string MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK))
                    {
                        MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK", MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK);
                    }

                    // MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED
                    string MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED))
                    {
                        MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED", MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED);
                    }

                    // MALWAREPROTECTION_SIGNATURE_REVERSION
                    string MALWAREPROTECTION_SIGNATURE_REVERSION = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_REVERSION");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_SIGNATURE_REVERSION))
                    {
                        MALWAREPROTECTION_SIGNATURE_REVERSION = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_REVERSION", MALWAREPROTECTION_SIGNATURE_REVERSION);
                    }

                    // MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE
                    string MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE))
                    {
                        MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE", MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE);
                    }

                    // MALWAREPROTECTION_PLATFORM_UPDATE_FAILED
                    string MALWAREPROTECTION_PLATFORM_UPDATE_FAILED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PLATFORM_UPDATE_FAILED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_PLATFORM_UPDATE_FAILED))
                    {
                        MALWAREPROTECTION_PLATFORM_UPDATE_FAILED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PLATFORM_UPDATE_FAILED", MALWAREPROTECTION_PLATFORM_UPDATE_FAILED);
                    }

                    // MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE
                    string MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE))
                    {
                        MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE", MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE);
                    }

                    // MALWAREPROTECTION_OS_EXPIRING
                    string MALWAREPROTECTION_OS_EXPIRING = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_OS_EXPIRING");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_OS_EXPIRING))
                    {
                        MALWAREPROTECTION_OS_EXPIRING = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_OS_EXPIRING", MALWAREPROTECTION_OS_EXPIRING);
                    }

                    // MALWAREPROTECTION_OS_EOL
                    string MALWAREPROTECTION_OS_EOL = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_OS_EOL");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_OS_EOL))
                    {
                        MALWAREPROTECTION_OS_EOL = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_OS_EOL", MALWAREPROTECTION_OS_EOL);
                    }

                    // MALWAREPROTECTION_PROTECTION_EOL
                    string MALWAREPROTECTION_PROTECTION_EOL = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PROTECTION_EOL");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_PROTECTION_EOL))
                    {
                        MALWAREPROTECTION_PROTECTION_EOL = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PROTECTION_EOL", MALWAREPROTECTION_PROTECTION_EOL);
                    }

                    // MALWAREPROTECTION_RTP_FEATURE_FAILURE
                    string MALWAREPROTECTION_RTP_FEATURE_FAILURE = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_FAILURE");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_RTP_FEATURE_FAILURE))
                    {
                        MALWAREPROTECTION_RTP_FEATURE_FAILURE = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_FAILURE", MALWAREPROTECTION_RTP_FEATURE_FAILURE);
                    }

                    // MALWAREPROTECTION_RTP_FEATURE_RECOVERED
                    string MALWAREPROTECTION_RTP_FEATURE_RECOVERED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_RECOVERED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_RTP_FEATURE_RECOVERED))
                    {
                        MALWAREPROTECTION_RTP_FEATURE_RECOVERED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_RECOVERED", MALWAREPROTECTION_RTP_FEATURE_RECOVERED);
                    }

                    // MALWAREPROTECTION_RTP_FEATURE_CONFIGURED
                    string MALWAREPROTECTION_RTP_FEATURE_CONFIGURED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_CONFIGURED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_RTP_FEATURE_CONFIGURED))
                    {
                        MALWAREPROTECTION_RTP_FEATURE_CONFIGURED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_CONFIGURED", MALWAREPROTECTION_RTP_FEATURE_CONFIGURED);
                    }

                    // MALWAREPROTECTION_CONFIG_CHANGED
                    string MALWAREPROTECTION_CONFIG_CHANGED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_CONFIG_CHANGED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_CONFIG_CHANGED))
                    {
                        MALWAREPROTECTION_CONFIG_CHANGED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_CONFIG_CHANGED", MALWAREPROTECTION_CONFIG_CHANGED);
                    }

                    // MALWAREPROTECTION_ENGINE_FAILURE
                    string MALWAREPROTECTION_ENGINE_FAILURE = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ENGINE_FAILURE");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_ENGINE_FAILURE))
                    {
                        MALWAREPROTECTION_ENGINE_FAILURE = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ENGINE_FAILURE", MALWAREPROTECTION_ENGINE_FAILURE);
                    }

                    // MALWAREPROTECTION_ANTISPYWARE_ENABLED
                    string MALWAREPROTECTION_ANTISPYWARE_ENABLED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTISPYWARE_ENABLED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_ANTISPYWARE_ENABLED))
                    {
                        MALWAREPROTECTION_ANTISPYWARE_ENABLED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTISPYWARE_ENABLED", MALWAREPROTECTION_ANTISPYWARE_ENABLED);
                    }

                    // MALWAREPROTECTION_ANTISPYWARE_DISABLED
                    string MALWAREPROTECTION_ANTISPYWARE_DISABLED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTISPYWARE_DISABLED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_ANTISPYWARE_DISABLED))
                    {
                        MALWAREPROTECTION_ANTISPYWARE_DISABLED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTISPYWARE_DISABLED", MALWAREPROTECTION_ANTISPYWARE_DISABLED);
                    }

                    // MALWAREPROTECTION_ANTIVIRUS_ENABLED
                    string MALWAREPROTECTION_ANTIVIRUS_ENABLED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTIVIRUS_ENABLED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_ANTIVIRUS_ENABLED))
                    {
                        MALWAREPROTECTION_ANTIVIRUS_ENABLED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTIVIRUS_ENABLED", MALWAREPROTECTION_ANTIVIRUS_ENABLED);
                    }

                    // MALWAREPROTECTION_ANTIVIRUS_DISABLED
                    string MALWAREPROTECTION_ANTIVIRUS_DISABLED = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTIVIRUS_DISABLED");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_ANTIVIRUS_DISABLED))
                    {
                        MALWAREPROTECTION_ANTIVIRUS_DISABLED = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTIVIRUS_DISABLED", MALWAREPROTECTION_ANTIVIRUS_DISABLED);
                    }

                    // TAMPER_PROTECTION_BLOCKED_CHANGES
                    string TAMPER_PROTECTION_BLOCKED_CHANGES = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "TAMPER_PROTECTION_BLOCKED_CHANGES");

                    if (string.IsNullOrEmpty(TAMPER_PROTECTION_BLOCKED_CHANGES))
                    {
                        TAMPER_PROTECTION_BLOCKED_CHANGES = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "TAMPER_PROTECTION_BLOCKED_CHANGES", TAMPER_PROTECTION_BLOCKED_CHANGES);
                    }

                    // MALWAREPROTECTION_EXPIRATION_WARNING_STATE
                    string MALWAREPROTECTION_EXPIRATION_WARNING_STATE = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_EXPIRATION_WARNING_STATE");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_EXPIRATION_WARNING_STATE))
                    {
                        MALWAREPROTECTION_EXPIRATION_WARNING_STATE = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_EXPIRATION_WARNING_STATE", MALWAREPROTECTION_EXPIRATION_WARNING_STATE);
                    }

                    // MALWAREPROTECTION_DISABLED_EXPIRED_STATE
                    string MALWAREPROTECTION_DISABLED_EXPIRED_STATE = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_DISABLED_EXPIRED_STATE");

                    if (string.IsNullOrEmpty(MALWAREPROTECTION_DISABLED_EXPIRED_STATE))
                    {
                        MALWAREPROTECTION_DISABLED_EXPIRED_STATE = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_DISABLED_EXPIRED_STATE", MALWAREPROTECTION_DISABLED_EXPIRED_STATE);
                    }

                    // CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION
                    string CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION");

                    if (string.IsNullOrEmpty(CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION))
                    {
                        CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION = DateTime.Now.ToString();
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION", CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION);
                    }

                    Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Eventlog_Crawler.Check_Registry", "Read values.", "Done.");
                }
                catch (Exception ex)
                {
                    Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Eventlog_Crawler.Check_Registry", "General error", ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.Check_Registry", "Failed", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_SCAN_COMPLETED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_COMPLETED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1001 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_SCAN_COMPLETED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());

                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Scan job completed.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_SCAN_COMPLETED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Scanauftrag fertiggestellt.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_SCAN_COMPLETED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_COMPLETED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_SCAN_COMPLETED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_RTP_ENABLED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_ENABLED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5000 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_RTP_ENABLED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Real-time protection enabled.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_RTP_ENABLED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Echtzeitschutz aktiviert.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_RTP_ENABLED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);

                        //Trigger trayicon notification | NetLock legacy code. Will be worked on in the future.
                        /*if (NetLock_Agent_Service.tray_icon_notifications_antivirus)
                            if (NetLock_Agent_Service.os_language != 0)
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Real-time protection enabled.", 0, 3000);
                            else
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Echtzeitschutz aktiviert.", 0, 3000);
                        */
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_ENABLED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_RTP_ENABLED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_RTP_DISABLED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_DISABLED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5001 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_RTP_DISABLED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_RTP_DISABLED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Real-time protection disabled.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Echtzeitschutz deaktiviert.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);

                        //Trigger trayicon notification | NetLock legacy code. Will be worked on in the future.
                        /*
                        if (NetLock_Agent_Service.tray_icon_notifications_antivirus)
                            if (NetLock_Agent_Service.os_language != 0)
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Real-time protection disabled.", 2, 3000);
                            else
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Echtzeitschutz deaktiviert.", 2, 3000);
                        */
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_DISABLED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_RTP_DISABLED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_SIGNATURE_UPDATED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_UPDATED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 2000 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_SIGNATURE_UPDATED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Signatures updated.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_SIGNATURE_UPDATED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Signaturen aktualisiert.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_SIGNATURE_UPDATED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                        
                        //Trigger trayicon notification | NetLock legacy code. Will be worked on in the future.
                        /*
                        if (NetLock_Agent_Service.tray_icon_notifications_antivirus)
                            if (NetLock_Agent_Service.os_language != 0)
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Virus definitions updated.", 0, 3000);
                            else
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Virendefinitionen aktualisiert.", 0, 3000);
                        */
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_UPDATED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_SIGNATURE_UPDATED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_STATE_MALWARE_DETECTED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_STATE_MALWARE_DETECTED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1116 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_STATE_MALWARE_DETECTED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_STATE_MALWARE_DETECTED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Malware detected.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Malware gefunden.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_STATE_MALWARE_DETECTED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_STATE_MALWARE_DETECTED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_MALWARE_ACTION_TAKEN()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_ACTION_TAKEN");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1116 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_MALWARE_ACTION_TAKEN" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_MALWARE_ACTION_TAKEN", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Malware detected. Actions taken.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Malware gefunden. Aktionen durchgeführt.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                        
                        //Trigger trayicon notification | NetLock legacy code. Will be worked on in the future.
                        /*if (NetLock_Agent_Service.tray_icon_notifications_antivirus)
                            if (NetLock_Agent_Service.os_language != 0)
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Virus found. Actions performed.", 2, 3000);
                            else
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Virus gefunden. Aktionen durchgeführt.", 2, 3000);
                        */
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_ACTION_TAKEN", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_MALWARE_ACTION_TAKEN", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_MALWARE_DETECTED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_DETECTED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1116 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_MALWARE_DETECTED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_MALWARE_DETECTED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Malware detected.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Malware gefunden.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);

                        //Extract detection information
                        var threat_name = Regex.Matches(eventRecord.FormatDescription(), @"Name: *.*");
                        var threat_id = Regex.Matches(eventRecord.FormatDescription(), @"ID: *.*");

                        //Trigger sensor management | NetLock legacy code. Will be worked on in the future. Part of automatic incident response.
                        //Logging.Handler.log_sensor_management("Sensor_Management.EDR.Antivirus.Malware_Detected.Do", "trigger sensor management", "threat_name: " + threat_name[0].ToString() + "threat_id: " + threat_id[0].ToString() + "details: " + details);
                        //Sensor_Management.EDR.Antivirus.Malware_Detected.Do(threat_name[0].ToString(), threat_id[0].ToString(), details);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_DETECTED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_MALWARE_DETECTED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_SCAN_CANCELLED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_CANCELLED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1002 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_SCAN_CANCELLED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_SCAN_CANCELLED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "A scan job stopped before it was completed.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "Ein Scanauftrag wurde gestoppt, bevor er abgeschlossen war.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);                        
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_CANCELLED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_SCAN_CANCELLED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_SCAN_PAUSED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_PAUSED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1003 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_SCAN_PAUSED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_SCAN_PAUSED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "An antimalware scan has stopped.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "Ein Antischadsoftwarescan wurde angehalten.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_PAUSED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_SCAN_PAUSED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_SCAN_FAILED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_FAILED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1005 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_SCAN_FAILED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_SCAN_FAILED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "Antimalware scan failed.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "Antischadsoftwarescan fehlgeschlagen.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SCAN_FAILED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_SCAN_FAILED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1008 || eventRecord.Id == 1118 || eventRecord.Id == 1119 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Action failed.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Aktion fehlgeschlagen.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_FAILED_00_MALWAREPROTECTION_STATE_MALWARE_ACTION_CRITICALLY_FAILED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_QUARANTINE_RESTORE()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_QUARANTINE_RESTORE");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1009 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_QUARANTINE_RESTORE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_QUARANTINE_RESTORE", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "An item has been recovered from quarantine.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "Ein Element wurde aus der Quarantäne wiederhergestellt.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);    
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_QUARANTINE_RESTORE", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_QUARANTINE_RESTORE", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_QUARANTINE_DELETE()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_QUARANTINE_DELETE");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1011 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_QUARANTINE_DELETE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_QUARANTINE_DELETE", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "An item was deleted from quarantine.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Ein Element wurde aus der Quarantäne gelöscht.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);    
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_QUARANTINE_DELETE", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_QUARANTINE_DELETE", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_MALWARE_HISTORY_DELETE()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_HISTORY_DELETE");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1013 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_MALWARE_HISTORY_DELETE", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Deleted the history of malware.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_MALWARE_HISTORY_DELETE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Der Verlauf von Malware wurde gelöscht.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_MALWARE_HISTORY_DELETE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_MALWARE_HISTORY_DELETE", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_MALWARE_HISTORY_DELETE", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_BEHAVIOR_DETECTED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_BEHAVIOR_DETECTED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1015 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_BEHAVIOR_DETECTED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_BEHAVIOR_DETECTED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Suspicious behavior detected.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Verdächtiges Verhalten erkannt.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                        
                        //Trigger trayicon notification | NetLock legacy code. Will be worked on in the future.
                        /*
                        if (NetLock_Agent_Service.tray_icon_notifications_antivirus)
                            if (NetLock_Agent_Service.os_language != 0)
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Suspicious behavior detected.", 2, 3000);
                            else
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Verdächtiges Verhalten erkannt.", 2, 3000);
                        */
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_BEHAVIOR_DETECTED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_BEHAVIOR_DETECTED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1117 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Actions taken.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Aktionen durchgeführt.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_STATE_MALWARE_ACTION_TAKEN", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1127 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "Controlled folder access prevented changes to memory.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "Kontrollierter Ordnerzugriff verhinderte Änderungen am Speicher.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                        
                        //Trigger trayicon notification | NetLock legacy code. Will be worked on in the future.
                        /*
                        if (NetLock_Agent_Service.tray_icon_notifications_antivirus)
                            if (NetLock_Agent_Service.os_language != 0)
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Unauthorized access to folder blocked.", 1, 3000);
                            else
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Unbefugter Zugriff auf Ordner blockiert.", 1, 3000);
                        */
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_FOLDER_GUARD_SECTOR_BLOCK", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 2001 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "AntiVirus signature update failed.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "Update der AntiVirus Signaturen fehlgeschlagen.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_SIGNATURE_UPDATE_FAILED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_SIGNATURE_REVERSION()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_REVERSION");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 2004 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_SIGNATURE_REVERSION" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_SIGNATURE_REVERSION", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "AntiVirus signature update failed. Trying to load old signatures.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "Update der AntiVirus Signaturen fehlgeschlagen. Versuche alte Signaturen zu laden.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_SIGNATURE_REVERSION", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_SIGNATURE_REVERSION", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 2005 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "AntiVirus module update failed. Trying to load old version.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "Update des AntiVirus-Moduls fehlgeschlagen. Versuché alte Version zu laden.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_ENGINE_UPDATE_PLATFORMOUTOFDATE", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_PLATFORM_UPDATE_FAILED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PLATFORM_UPDATE_FAILED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 2006 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_PLATFORM_UPDATE_FAILED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_PLATFORM_UPDATE_FAILED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "The platform update failed.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "Das Plattform-Update ist fehlgeschlagen.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PLATFORM_UPDATE_FAILED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_PLATFORM_UPDATE_FAILED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 2007 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "The platform will soon be outdated.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "Die Plattform wird bald veraltet sein.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_PLATFORM_ALMOSTOUTOFDATE", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_OS_EXPIRING()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_OS_EXPIRING");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 2040 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_OS_EXPIRING", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "Antimalware support for this OS version will end soon.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_OS_EXPIRING" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "Der AntiVirus Support für diese Betriebssystemversion endet bald.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_OS_EXPIRING" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_OS_EXPIRING", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_OS_EXPIRING", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_OS_EOL()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_OS_EOL");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 2041 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_OS_EOL" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_OS_EOL", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Antimalware support for this operating system has ended.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Der AntiVirus Support für dieses Betriebssystem wurde eingestellt.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_OS_EOL", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_OS_EOL", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_PROTECTION_EOL()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PROTECTION_EOL");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 2042 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_PROTECTION_EOL" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_PROTECTION_EOL", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "The antimalware engine no longer supports this operating system and no longer protects your system from malware.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Die Antimalware-Engine unterstützt dieses Betriebssystem nicht mehr und schützt Ihr System nicht mehr vor Malware.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_PROTECTION_EOL", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_PROTECTION_EOL", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_RTP_FEATURE_FAILURE()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_FAILURE");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 3002 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_RTP_FEATURE_FAILURE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_RTP_FEATURE_FAILURE", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "Real-time protection encountered an error.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "Beim Echtzeitschutz ist ein Fehler aufgetreten.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_FAILURE", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_RTP_FEATURE_FAILURE", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_RTP_FEATURE_RECOVERED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_RECOVERED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 3007 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_RTP_FEATURE_RECOVERED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "Real-time protection recovered from failure.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_RTP_FEATURE_RECOVERED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("1", "Microsoft Defender Antivirus", "Der Echtzeitschutz wurde nach einem Fehler wiederhergestellt.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_RTP_FEATURE_RECOVERED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_RECOVERED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_RTP_FEATURE_RECOVERED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_RTP_FEATURE_CONFIGURED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_CONFIGURED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5004 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_RTP_FEATURE_CONFIGURED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "The real-time protection configuration has been changed.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_RTP_FEATURE_CONFIGURED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Die Konfiguration des Echtzeitschutzes wurde geändert.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_RTP_FEATURE_CONFIGURED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_RTP_FEATURE_CONFIGURED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_RTP_FEATURE_CONFIGURED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_CONFIG_CHANGED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_CONFIG_CHANGED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5007 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_CONFIG_CHANGED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "The configuration of the antimalware platform has changed.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_CONFIG_CHANGED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Die Konfiguration der Antimalware-Plattform hat sich geändert.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_CONFIG_CHANGED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_CONFIG_CHANGED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_CONFIG_CHANGED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_ENGINE_FAILURE()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ENGINE_FAILURE");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5008 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_ENGINE_FAILURE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_ENGINE_FAILURE", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "The antimalware engine encountered an error.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "Die Antimalware-Engine hat einen Fehler festgestellt.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ENGINE_FAILURE", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_ENGINE_FAILURE", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_ANTISPYWARE_ENABLED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTISPYWARE_ENABLED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5009 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_ANTISPYWARE_ENABLED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Scanning for malware and other potentially unwanted software is enabled.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_ANTISPYWARE_ENABLED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Das Scannen nach Malware und anderer potenziell unerwünschter Software ist aktiviert.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_ANTISPYWARE_ENABLED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTISPYWARE_ENABLED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_ANTISPYWARE_ENABLED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_ANTISPYWARE_DISABLED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTISPYWARE_DISABLED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5010 && last_event < eventRecord.TimeCreated)
                    {

                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_ANTISPYWARE_DISABLED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_ANTISPYWARE_DISABLED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Scanning for malware and other potentially unwanted software is disabled.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Das Scannen nach Malware und anderer potenziell unerwünschter Software ist deaktiviert.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTISPYWARE_DISABLED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_ANTISPYWARE_DISABLED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_ANTIVIRUS_ENABLED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTIVIRUS_ENABLED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5011 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_ANTIVIRUS_ENABLED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Virus scanning is enabled.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_ANTIVIRUS_ENABLED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("0", "Microsoft Defender Antivirus", "Der Virenscan ist aktiviert.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_ANTIVIRUS_ENABLED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTIVIRUS_ENABLED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_ANTIVIRUS_ENABLED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_ANTIVIRUS_DISABLED()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTIVIRUS_DISABLED");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5012 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_ANTIVIRUS_DISABLED" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_ANTIVIRUS_DISABLED", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Virus scanning is disabled.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Der Virenscan ist deaktiviert.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_ANTIVIRUS_DISABLED", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_ANTIVIRUS_DISABLED", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void TAMPER_PROTECTION_BLOCKED_CHANGES()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "TAMPER_PROTECTION_BLOCKED_CHANGES");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5013 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: TAMPER_PROTECTION_BLOCKED_CHANGES" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.TAMPER_PROTECTION_BLOCKED_CHANGES", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Tamper protection blocked a change to microsoft defender antivirus.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Der Manipulationsschutz hat eine Änderung an Microsoft Defender Antivirus blockiert.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "TAMPER_PROTECTION_BLOCKED_CHANGES", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.TAMPER_PROTECTION_BLOCKED_CHANGES", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_EXPIRATION_WARNING_STATE()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_EXPIRATION_WARNING_STATE");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5100 && last_event < eventRecord.TimeCreated)
                    {
                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_EXPIRATION_WARNING_STATE", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "The antimalware platform is about to expire.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_EXPIRATION_WARNING_STATE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("2", "Microsoft Defender Antivirus", "Die Antimalware-Plattform läuft bald ab.", "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_EXPIRATION_WARNING_STATE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription(), Service.microsoft_defender_antivirus_notifications_json, 0, 1);    
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_EXPIRATION_WARNING_STATE", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_EXPIRATION_WARNING_STATE", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void MALWAREPROTECTION_DISABLED_EXPIRED_STATE()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_DISABLED_EXPIRED_STATE");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 5101 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: MALWAREPROTECTION_DISABLED_EXPIRED_STATE" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.MALWAREPROTECTION_DISABLED_EXPIRED_STATE", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "The antimalware platform has expired.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Die Antimalware-Plattform ist abgelaufen.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "MALWAREPROTECTION_DISABLED_EXPIRED_STATE", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.MALWAREPROTECTION_DISABLED_EXPIRED_STATE", "Couldnt read event log.", ex.ToString());
            }
        }

        public static void CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION()
        {
            try
            {
                string decrypted_datetime = Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION");
                DateTime last_event = DateTime.Parse(decrypted_datetime);

                EventLogQuery query = new EventLogQuery("Microsoft-Windows-Windows Defender/Operational", PathType.LogName, "*");
                EventLogReader reader = new EventLogReader(query);
                EventRecord eventRecord;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (eventRecord.Id == 1123 && last_event < eventRecord.TimeCreated)
                    {
                        string details = "Timestamp: " + eventRecord.TimeCreated + Environment.NewLine + "Sensor: CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION" + Environment.NewLine + Environment.NewLine + eventRecord.FormatDescription();

                        Logging.Handler.Microsoft_Defender_Antivirus("Helper.Eventlog_Reader.Microsoft_Defender_AntiVirus.CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION", "Event", eventRecord.TimeCreated + Environment.NewLine + eventRecord.FormatDescription());
                        
                        if (Service.language == "en-US")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Application action blocked.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 0);
                        else if (Service.language == "de-DE")
                            Events.Logger.Insert_Event("3", "Microsoft Defender Antivirus", "Anwendungsaktion blockiert.", details, Service.microsoft_defender_antivirus_notifications_json, 0, 1);

                        //Trigger trayicon notification | NetLock legacy code. Will be worked on in the future.
                        /*
                        if (NetLock_Agent_Service.tray_icon_notifications_antivirus)
                            if (NetLock_Agent_Service.os_language != 0)
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Unauthorized access to folder blocked.", 1, 3000);
                            else
                                Tray_Icon.Communication.Send_Data(0, "Antivirus", "Unbefugter Zugriff auf Ordner blockiert.", 1, 3000);
                        */
                    }
                }

                //Set time of last events
                Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_microsoft_defender_antivirus_reg_path, "CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION", DateTime.Now.ToString());
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Eventlog_Crawler.CONTROLLED_FOLDER_ACTIONS_BLOCKED_ACTION", "Couldnt read event log.", ex.ToString());
            }
        }
    }
}
