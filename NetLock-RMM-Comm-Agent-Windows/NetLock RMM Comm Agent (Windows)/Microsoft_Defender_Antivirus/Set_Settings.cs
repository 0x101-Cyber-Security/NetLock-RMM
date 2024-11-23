using Microsoft.Win32;
using NetLock_Agent.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetLock_RMM_Comm_Agent_Windows.Microsoft_Defender_Antivirus
{    
    //Part of NetLock Legacy. Needs code quality improvements in future. Code has been adjusted to work with new NetLock RMM

    internal class Set_Settings
    {
        public class Exclusion_Item
        {
            public string id { get; set; }
            public string date { get; set; }
            public string type { get; set; }
            public string exclusion { get; set; }
            public string description { get; set; }
        }

        public class Directory_Item
        {
            public string folder { get; set; }
            public string date { get; set; }
            public string description { get; set; }
        }

        public class Processes
        {
            public string name { get; set; }
            public string date { get; set; }
            public string description { get; set; }
            public string author { get; set; }
            public string process_path { get; set; }
            public string company { get; set; }
            public string product { get; set; }
            public string copyright { get; set; }
            public string brand { get; set; }
            public string product_version { get; set; }
            public string file_version { get; set; }
            public string file_sha1 { get; set; }
            public string file_md5 { get; set; }
            public string file_sha256 { get; set; }
            public string file_sha512 { get; set; }
            public string cert_owner { get; set; }
            public string cert_issuer { get; set; }
            public string cert_begin_date { get; set; }
            public string cert_end_date { get; set; }
            public string cert_public_key { get; set; }
            public string cert_serial_key { get; set; }
            public string cert_sha1 { get; set; }
        }

        public static void Do()
        {
            bool security_center = false;
            bool allow_metered_updates = false;
            bool delete_quarantine_six_months = false;
            string scan_direction = "0";
            bool file_hash_computing = false;
            bool block_at_first_seen = false;
            bool scan_mails = false;
            bool net_scan_network_files = false;
            bool net_filter_incoming_connections = false;
            bool net_datagram_processing = false;
            bool parser_tls = false;
            bool parser_ssh = false;
            bool parser_http = false;
            bool parser_dns = false;
            bool parser_dnsovertcp = false;
            bool controlled_folder_access_enabled = false;

            using (JsonDocument document = JsonDocument.Parse(Service.policy_antivirus_settings_json))
            {
                JsonElement security_center_element = document.RootElement.GetProperty("security_center");
                security_center = Convert.ToBoolean(security_center_element.ToString());

                // allow_metered_updates
                JsonElement allow_metered_updates_element = document.RootElement.GetProperty("allow_metered_updates");
                allow_metered_updates = Convert.ToBoolean(allow_metered_updates_element.ToString());

                // delete_quarantine_six_months
                JsonElement delete_quarantine_six_months_element = document.RootElement.GetProperty("delete_quarantine_six_months");
                delete_quarantine_six_months = Convert.ToBoolean(delete_quarantine_six_months_element.ToString());

                // scan_direction
                JsonElement scan_direction_element = document.RootElement.GetProperty("scan_direction");
                scan_direction = scan_direction_element.ToString();

                // file_hash_computing
                JsonElement file_hash_computing_element = document.RootElement.GetProperty("file_hash_computing");
                file_hash_computing = Convert.ToBoolean(file_hash_computing_element.ToString());

                // block_at_first_seen
                JsonElement block_at_first_seen_element = document.RootElement.GetProperty("block_at_first_seen");
                block_at_first_seen = Convert.ToBoolean(block_at_first_seen_element.ToString());

                // scan_mails
                JsonElement scan_mails_element = document.RootElement.GetProperty("scan_mails");
                scan_mails = Convert.ToBoolean(scan_mails_element.ToString());

                // net_scan_network_files
                JsonElement net_scan_network_files_element = document.RootElement.GetProperty("net_scan_network_files");
                net_scan_network_files = Convert.ToBoolean(net_scan_network_files_element.ToString());

                // net_filter_incoming_connections
                JsonElement net_filter_incoming_connections_element = document.RootElement.GetProperty("net_filter_incoming_connections");
                net_filter_incoming_connections = Convert.ToBoolean(net_filter_incoming_connections_element.ToString());

                // net_datagram_processing
                JsonElement net_datagram_processing_element = document.RootElement.GetProperty("net_datagram_processing");
                net_datagram_processing = Convert.ToBoolean(net_datagram_processing_element.ToString());

                // parser_tls
                JsonElement parser_tls_element = document.RootElement.GetProperty("parser_tls");
                parser_tls = Convert.ToBoolean(parser_tls_element.ToString());

                // parser_ssh
                JsonElement parser_ssh_element = document.RootElement.GetProperty("parser_ssh");
                parser_ssh = Convert.ToBoolean(parser_ssh_element.ToString());

                // parser_http
                JsonElement parser_http_element = document.RootElement.GetProperty("parser_http");
                parser_http = Convert.ToBoolean(parser_http_element.ToString());

                // parser_dns
                JsonElement parser_dns_element = document.RootElement.GetProperty("parser_dns");
                parser_dns = Convert.ToBoolean(parser_dns_element.ToString());

                // parser_dnsovertcp
                JsonElement parser_dnsovertcp_element = document.RootElement.GetProperty("parser_dnsovertcp");
                parser_dnsovertcp = Convert.ToBoolean(parser_dnsovertcp_element.ToString());

                // controlled_folder_access_enabled
                JsonElement controlled_folder_access_enabled_element = document.RootElement.GetProperty("controlled_folder_access_enabled");
                controlled_folder_access_enabled = Convert.ToBoolean(controlled_folder_access_enabled_element.ToString());

            }

            //windows_defender_antivirus_g_ui_security_center
            if (security_center && Check_Settings.Microsoft_Defender_Antivirus_G_Ui_Security_Center() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_g_ui_security_center", "Set-MpPreference -UILockdown $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_g_ui_security_center", "False");
            }
            else if (security_center == false && Check_Settings.Microsoft_Defender_Antivirus_G_Ui_Security_Center() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_g_ui_security_center", "Set-MpPreference -UILockdown $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_g_ui_security_center", "True");
            }

            //windows_defender_antivirus_g_u_allw_metrd_updates
            if (allow_metered_updates && Check_Settings.Microsoft_Defender_Antivirus_G_U_Allw_Metrd_Updates() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_g_u_allw_metrd_updates", "Set-MpPreference -MeteredConnectionUpdates $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_g_u_allw_metrd_updates", "True");
            }
            else if (allow_metered_updates == false && Check_Settings.Microsoft_Defender_Antivirus_G_U_Allw_Metrd_Updates() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_g_u_allw_metrd_updates", "Set-MpPreference -MeteredConnectionUpdates $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_g_u_allw_metrd_updates", "False");
            }

            //windows_defender_antivirus_g_m_del_quaran_six_months
            if (delete_quarantine_six_months && Check_Settings.Microsoft_Defender_Antivirus_G_M_Del_Quaran_Six_Months() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_g_m_del_quaran_six_months", "Set-MpPreference -QuarantinePurgeItemsAfterDelay 180", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_g_m_del_quaran_six_months", "True");
            }
            else if (delete_quarantine_six_months == false && Check_Settings.Microsoft_Defender_Antivirus_G_M_Del_Quaran_Six_Months() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_g_m_del_quaran_six_months", "Set-MpPreference -QuarantinePurgeItemsAfterDelay 9999", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_g_m_del_quaran_six_months", "False");
            }

            //windows_defender_antivirus_s_real_time_pro_direction
            if (Check_Settings.Microsoft_Defender_Antivirus_Scan_Direction() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_real_time_pro_direction", "Set-MpPreference -RealTimeScanDirection " + scan_direction, 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_real_time_pro_direction", "Update");
            }

            //windows_defender_antivirus_s_fs_file_hash_computation
            if (file_hash_computing && Check_Settings.Microsoft_Defender_Antivirus_S_Fs_File_Hash_Computation() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_fs_file_hash_computation", "Set-MpPreference -EnableFileHashComputation $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_fs_file_hash_computation", "True");
            }
            else if (file_hash_computing == false && Check_Settings.Microsoft_Defender_Antivirus_S_Fs_File_Hash_Computation() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_fs_file_hash_computation", "Set-MpPreference -EnableFileHashComputation $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_fs_file_hash_computation", "False");
            }

            //windows_defender_antivirus_s_fs_block_at_first_seen // tempoary disabled due to possiuble fp
            /*
            if (block_at_first_seen && Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Block_At_First_Seen() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_fs_block_at_first_seen", "Set-MpPreference -DisableBlockAtFirstSeen $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_fs_block_at_first_seen", "False");
            }
            else if (block_at_first_seen == false && Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Block_At_First_Seen() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_fs_block_at_first_seen", "Set-MpPreference -DisableBlockAtFirstSeen $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_fs_block_at_first_seen", "True");
            }
            */

            //windows_defender_antivirus_s_fs_scan_mails
            if (scan_mails && Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Scan_Mails() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_fs_scan_mails", "Set-MpPreference -DisableEmailScanning $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_fs_scan_mails", "False");
            }
            else if (scan_mails == false && Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Scan_Mails() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_fs_scan_mails", "Set-MpPreference -DisableEmailScanning $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_fs_scan_mails", "True");
            }

            //windows_defender_antivirus_s_net_scan_network_files
            if (net_scan_network_files && Check_Settings.Microsoft_Defender_Antivirus_S_Net_Scan_Network_Files() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_net_scan_network_files", "Set-MpPreference -DisableScanningNetworkFiles $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_net_scan_network_files", "False");
            }
            else if (net_scan_network_files == false && Check_Settings.Microsoft_Defender_Antivirus_S_Net_Scan_Network_Files() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_net_scan_network_files", "Set-MpPreference -DisableScanningNetworkFiles $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_net_scan_network_files", "True");
            }

            //windows_defender_antivirus_s_net_filter_incoming_connections
            if (net_filter_incoming_connections && Check_Settings.Microsoft_Defender_Antivirus_S_Net_Filter_Incoming_Connections() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_net_filter_incoming_connections", "Set-MpPreference -DisableInboundConnectionFiltering $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_net_filter_incoming_connections", "False");
            }
            else if (net_filter_incoming_connections == false && Check_Settings.Microsoft_Defender_Antivirus_S_Net_Filter_Incoming_Connections() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_net_filter_incoming_connections", "Set-MpPreference -DisableInboundConnectionFiltering $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_net_filter_incoming_connections", "True");
            }

            //windows_defender_antivirus_s_net_datagram_processing
            if (net_datagram_processing && Check_Settings.Microsoft_Defender_Antivirus_S_Net_Datagram_Processing() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_net_datagram_processing", "Set-MpPreference -DisableDatagramProcessing $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_net_datagram_processing", "False");
            }
            else if (net_datagram_processing == false && Check_Settings.Microsoft_Defender_Antivirus_S_Net_Datagram_Processing() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_net_datagram_processing", "Set-MpPreference -DisableDatagramProcessing $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_net_datagram_processing", "True");
            }

            //windows_defender_antivirus_s_parser_tls
            if (parser_tls && Check_Settings.Microsoft_Defender_Antivirus_S_Parser_TLS() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_parser_tls", "Set-MpPreference -DisableTlsParsing $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_parser_tls", "False");
            }
            else if (parser_tls == false && Check_Settings.Microsoft_Defender_Antivirus_S_Parser_TLS() == false)
            {
                PowerShell.Execute_Command("wicrosoft_defender_antivirus_s_parser_tls", "Set-MpPreference -DisableTlsParsing $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_parser_tls", "True");
            }

            //windows_defender_antivirus_s_parser_ssh
            if (parser_ssh && Check_Settings.Microsoft_Defender_Antivirus_S_Parser_SSH() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_parser_ssh", "Set-MpPreference -DisableSshParsing $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_parser_ssh", "False");
            }
            else if (parser_ssh == false && Check_Settings.Microsoft_Defender_Antivirus_S_Parser_SSH() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_parser_ssh", "Set-MpPreference -DisableSshParsing $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_parser_ssh", "True");
            }

            //windows_defender_antivirus_s_parser_http
            if (parser_http && Check_Settings.Microsoft_Defender_Antivirus_S_Parser_HTTP() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_parser_http", "Set-MpPreference -DisableHttpParsing $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_parser_http", "False");
            }
            else if (parser_http == false && Check_Settings.Microsoft_Defender_Antivirus_S_Parser_HTTP() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_parser_http", "Set-MpPreference -DisableHttpParsing $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_parser_http", "True");
            }

            //windows_defender_antivirus_s_parser_dns
            if (parser_dns && Check_Settings.Microsoft_Defender_Antivirus_S_Parser_DNS() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_parser_dns", "Set-MpPreference -DisableDnsParsing $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_parser_dns", "False");
            }
            else if (parser_dns == false && Check_Settings.Microsoft_Defender_Antivirus_S_Parser_DNS() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_parser_dns", "Set-MpPreference -DisableDnsParsing $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_parser_dns", "True");
            }

            //windows_defender_antivirus_s_parser_dnsovertcp
            if (parser_dnsovertcp && Check_Settings.Microsoft_Defender_Antivirus_S_Parser_DNSOverTCP() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_parser_dnsovertcp", "Set-MpPreference -DisableDnsOverTcpParsing $false", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_parser_dnsovertcp", "False");
            }
            else if (parser_dnsovertcp == false && Check_Settings.Microsoft_Defender_Antivirus_S_Parser_DNSOverTCP() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_s_parser_dnsovertcp", "Set-MpPreference -DisableDnsOverTcpParsing $true", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_s_parser_dnsovertcp", "True");
            }

            //Set Windows Defender File and Path Exclusions
            try
            {
                //List<string> exclusion_list = new List<string> { };
                List<string> exclusion_temp_list = new List<string> { };

                List<Exclusion_Item> exclusionItems = JsonSerializer.Deserialize<List<Exclusion_Item>>(Service.policy_antivirus_exclusions_json);

                // We first delete all existing exclusions that are not on the current exclusions list, to make sure there is no persistence
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths"))
                {
                    foreach (string ValueOfName in key.GetValueNames())
                    {
                        // Überprüfe, ob ein Exclusion_Item mit dem Wert des Namens in der Liste vorhanden ist
                        if (exclusionItems.Any(item => (item.exclusion == ValueOfName) && (item.type == "file" || item.type == "directory")))
                        {
                            Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_sexc_file", "Exists: " + ValueOfName);
                            exclusion_temp_list.Add(ValueOfName);
                        }
                        else
                        {
                            // Lösche den Registryeintrag, da er nicht in der aktuellen Ausschlussliste vorhanden ist
                            PowerShell.Execute_Command("microsoft_defender_antivirus_sexc_file", "Remove-MpPreference -ExclusionPath '" + ValueOfName + "' -Force", 30);
                            Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_sexc_file", "Remove: " + ValueOfName);
                        }
                    }
                }

                //And here we re-add current exclusions
                foreach (var exclusion in exclusionItems)
                {
                    Logging.Handler.Microsoft_Defender_Antivirus("foreach", "", exclusion.exclusion);

                    if (!exclusion_temp_list.Contains(exclusion.exclusion) && exclusion.type == "file" || exclusion.type == "directory")
                    {
                        PowerShell.Execute_Command("microsoft_defender_antivirus_sexc_file", "Add-MpPreference -ExclusionPath '" + exclusion.exclusion.Replace(@"\\", @"\") + "' -Force", 30);
                        Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_sexc_file", "Add: " + exclusion.exclusion.Replace(@"\\", @"\"));
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_sexc_file", "General error" + ex.ToString());
            }

            //Set Windows Defender File Extension Exclusions
            try
            {
                //List<string> exclusion_list = new List<string> { };
                List<string> exclusion_temp_list = new List<string> { };

                List<Exclusion_Item> exclusionItems = JsonSerializer.Deserialize<List<Exclusion_Item>>(Service.policy_antivirus_exclusions_json);

                // We first delete all existing exclusions that are not on the current exclusions list, to make sure there is no persistence
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Exclusions\Extensions"))
                {
                    foreach (string ValueOfName in key.GetValueNames())
                    {
                        // Überprüfe, ob ein Exclusion_Item mit dem Wert des Namens in der Liste vorhanden ist
                        if (exclusionItems.Any(item => (item.exclusion == ValueOfName) && (item.type == "extension")))
                        {
                            exclusion_temp_list.Add(ValueOfName);
                        }
                        else
                        {
                            // Lösche den Registryeintrag, da er nicht in der aktuellen Ausschlussliste vorhanden ist
                            PowerShell.Execute_Command("microsoft_defender_antivirus_sexc_file_ext", "Remove-MpPreference -ExclusionExtension  '" + ValueOfName + "' -Force", 30);
                            Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_sexc_file_ext", "Remove: " + ValueOfName);
                        }
                    }
                }

                //And here we re-add current exclusions
                foreach (var exclusion in exclusionItems)
                {
                    if (!exclusion_temp_list.Contains(exclusion.exclusion) && exclusion.type == "extension")
                    {
                        PowerShell.Execute_Command("microsoft_defender_antivirus_sexc_file_ext", "Add-MpPreference -ExclusionExtension  '" + exclusion.exclusion.Replace(@"\\", @"\") + "' -Force", 30);
                        Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_sexc_file_ext", "Add: " + exclusion.exclusion.Replace(@"\\", @"\"));
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_sexc_file_ext", "General error" + ex.ToString());
            }

            //Set Windows Defender Process Exclusions
            try
            {
                //List<string> exclusion_list = new List<string> { };
                List<string> exclusion_temp_list = new List<string> { };

                List<Exclusion_Item> exclusionItems = JsonSerializer.Deserialize<List<Exclusion_Item>>(Service.policy_antivirus_exclusions_json);

                // We first delete all existing exclusions that are not on the current exclusions list, to make sure there is no persistence
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Exclusions\Processes"))
                {
                    foreach (string ValueOfName in key.GetValueNames())
                    {
                        // Überprüfe, ob ein Exclusion_Item mit dem Wert des Namens in der Liste vorhanden ist
                        if (exclusionItems.Any(item => (item.exclusion == ValueOfName) && (item.type == "process")))
                        {
                            exclusion_temp_list.Add(ValueOfName);
                        }
                        else
                        {
                            // Lösche den Registryeintrag, da er nicht in der aktuellen Ausschlussliste vorhanden ist
                            PowerShell.Execute_Command("microsoft_defender_antivirus_sexc_process", "Remove-MpPreference -ExclusionProcess  '" + ValueOfName + "' -Force", 30);
                            Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_sexc_process", "Remove: " + ValueOfName);
                        }
                    }
                }

                //And here we re-add current exclusions
                foreach (var exclusion in exclusionItems)
                {
                    if (!exclusion_temp_list.Contains(exclusion.exclusion) && exclusion.type == "process")
                    {
                        PowerShell.Execute_Command("microsoft_defender_antivirus_sexc_process", "Add-MpPreference -ExclusionProcess  '" + exclusion.exclusion.Replace(@"\\", @"\") + "' -Force", 30);
                        Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_sexc_process", "Add: " + exclusion.exclusion.Replace(@"\\", @"\"));
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_sexc_process", "General error" + ex.ToString());
            }

            //microsoft_defender_antivirus_contr_folder_acc_enabled
            if (controlled_folder_access_enabled && Check_Settings.Microsoft_Defender_AntiVirus_Contr_Folder_Acc_Enabled() == false)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_contr_folder_acc_enabled", "Set-MpPreference -EnableControlledFolderAccess Enabled", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_contr_folder_acc_enabled", "True");
            }
            else if (controlled_folder_access_enabled == false && Check_Settings.Microsoft_Defender_AntiVirus_Contr_Folder_Acc_Enabled() == true)
            {
                PowerShell.Execute_Command("microsoft_defender_antivirus_contr_folder_acc_enabled", "Set-MpPreference -EnableControlledFolderAccess Disabled", 30);
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_contr_folder_acc_enabled", "False");
            }

            //Set Windows Defender Controlled Folder Access Directories
            try
            {
                //List<string> exclusion_list = new List<string> { };
                List<string> directory_temp_list = new List<string> { };

                List<Directory_Item> directoryItems = JsonSerializer.Deserialize<List<Directory_Item>>(Service.policy_antivirus_controlled_folder_access_folders_json);

                // We first delete all existing exclusions that are not on the current exclusions list, to make sure there is no persistence
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access\ProtectedFolders"))
                {
                    foreach (string ValueOfName in key.GetValueNames())
                    {
                        // Überprüfe, ob ein Exclusion_Item mit dem Wert des Namens in der Liste vorhanden ist
                        if (directoryItems.Any(item => item.folder == ValueOfName))
                        {
                            directory_temp_list.Add(ValueOfName);
                        }
                        else
                        {
                            // Lösche den Registryeintrag, da er nicht in der aktuellen Ausschlussliste vorhanden ist
                            PowerShell.Execute_Command("microsoft_defender_antivirus_contr_folder_acc_dirs", "Remove-MpPreference -ControlledFolderAccessProtectedFolders '" + ValueOfName + "' -Force", 30);
                            Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_contr_folder_acc_dirs", "Remove: " + ValueOfName);
                        }
                    }
                }

                //And here we re-add current exclusions
                foreach (var exclusion in directoryItems)
                {
                    if (!directory_temp_list.Contains(exclusion.folder))
                    {
                        PowerShell.Execute_Command("microsoft_defender_antivirus_contr_folder_acc_dirs", "Add-MpPreference -ControlledFolderAccessProtectedFolders '" + exclusion.folder.Replace(@"\\", @"\") + "' -Force", 30);
                        Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_contr_folder_acc_dirs", "Add: " + exclusion.folder.Replace(@"\\", @"\"));
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_contr_folder_acc_dirs", "General error" + ex.ToString());
            }

            //Set Windows Defender Controlled Folder Access Applications
            try
            {
                List<string> process_list = new List<string> { };
                List<string> process_temp_list = new List<string> { };

                List<Processes> processItems = JsonSerializer.Deserialize<List<Processes>>(Service.policy_antivirus_controlled_folder_access_ruleset_json);

                //Add each whitelisted process to the list
                foreach (var process in processItems)
                    process_list.Add(process.process_path);

                //Add the client itself to the directory list
                process_list.Add(Application_Paths.netlock_service_exe);
                process_list.Add(Application_Paths.netlock_installer_exe);
                
                //We first delete all existing exclusions that are not on the current exclusions list, to make sure there is no persistence
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access\AllowedApplications"))
                {
                    foreach (string ValueOfName in key.GetValueNames())
                    {
                        if (process_list.Contains(ValueOfName)) //If the registry apps is on the current exclusions list, make sure it wont be double added
                            process_temp_list.Add(ValueOfName);
                        else //If it is not on the current apps list, delete it from registry.
                        {
                            PowerShell.Execute_Command("microsoft_defender_antivirus_contr_folder_acc_apps", "Remove-MpPreference -ControlledFolderAccessAllowedApplications '" + ValueOfName + "' -Force", 30);
                            Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_contr_folder_acc_apps", "Remove: " + ValueOfName);
                        }
                    }
                }

                //And here we re-add current apps
                foreach (var process in process_list)
                {
                    if (process_temp_list.Contains(process) == false)
                    {
                        PowerShell.Execute_Command("microsoft_defender_antivirus_contr_folder_acc_apps", "Add-MpPreference -ControlledFolderAccessAllowedApplications '" + process.Replace(@"\\", @"\") + "' -Force", 30);
                        Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_contr_folder_acc_apps", "Add: " + process.Replace(@"\\", @"\"));
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Set_Settings.Do", "microsoft_defender_antivirus_contr_folder_acc_apps", "General error" + ex.ToString());
            }
        }
    }
}
