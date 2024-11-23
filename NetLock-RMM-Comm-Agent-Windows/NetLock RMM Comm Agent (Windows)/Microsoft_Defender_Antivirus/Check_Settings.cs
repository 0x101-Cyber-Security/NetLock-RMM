using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NetLock_RMM_Comm_Agent_Windows.Microsoft_Defender_Antivirus
{    
    //Part of NetLock Legacy. Needs code quality improvements in future.

    internal class Check_Settings
    {
        public static bool Microsoft_Defender_Antivirus_G_Ui_Security_Center()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\UX Configuration");
                registry_result = Convert.ToBoolean(regkey.GetValue("UILockdown"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Windows_Defender_Antivirus_G_Ui_Security_Center", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_G_Ui_Security_Center", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_G_U_Allw_Metrd_Updates()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Signature Updates");
                registry_result = Convert.ToBoolean(regkey.GetValue("MeteredConnectionUpdates"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_G_U_Allw_Metrd_Updates", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_G_U_Allw_Metrd_Updates", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_G_M_Del_Quaran_Six_Months()
        {
            try
            {
                string registry_result = null;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Quarantine");
                registry_result = regkey.GetValue("PurgeItemsAfterDelay").ToString();
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_G_M_Del_Quaran_Six_Months", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result == "180")
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_G_M_Del_Quaran_Six_Months", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_Scan_Direction()
        {
            try
            {
                string scan_direction = String.Empty;

                using (JsonDocument document = JsonDocument.Parse(Service.policy_antivirus_settings_json))
                {
                    JsonElement scan_direction_element = document.RootElement.GetProperty("scan_direction");
                    scan_direction = scan_direction_element.ToString();
                }

                string registry_result = null;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection");
                registry_result = regkey.GetValue("RealTimeScanDirection").ToString();
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_Scan_Direction", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result == scan_direction)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_Scan_Direction", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Fs_File_Hash_Computation()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\MpEngine");
                registry_result = Convert.ToBoolean(regkey.GetValue("EnableFileHashComputation"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Fs_File_Hash_Computation", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Fs_File_Hash_Computation", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Fs_Block_At_First_Seen()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Spynet");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableBlockAtFirstSeen"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Block_At_First_Seen", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Block_At_First_Seen", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Fs_Scan_Archives()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Scan");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableArchiveScanning"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Scan_Archives", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Scan_Archives", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Fs_Scan_Scripts()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Real-Time Protection");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableScriptScanning"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Scan_Scripts", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Scan_Scripts", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Fs_Scan_Mails()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Scan");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableEmailScanning"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Scan_Mails", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Fs_Scan_Mails", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Net_Scan_Network_Files()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Scan");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableScanningNetworkFiles"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Net_Scan_Network_Files", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Net_Scan_Network_Files", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Net_Filter_Incoming_Connections()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableInboundConnectionFiltering"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Net_Filter_Incoming_Connections", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Net_Filter_Incoming_Connections", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Net_Datagram_Processing()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\NIS\Consumers\IPS");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableDatagramProcessing"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Net_Datagram_Processing", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Net_Datagram_Processing", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Parser_TLS()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableTlsParsing"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_TLS", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_TLS", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Parser_RDP()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableRdpParsing"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_RDP", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_RDP", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Parser_SSH()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableSshParsing"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_SSH", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_SSH", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Parser_HTTP()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableHttpParsing"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_HTTP", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_HTTP", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Parser_DNS()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableDnsParsing"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_DNS", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_DNS", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_S_Parser_DNSOverTCP()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableDnsOverTcpParsing"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_DNSOverTCP", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_DNSOverTCP", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_Antivirus_Sexc_File()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Exclusions\Paths");
                registry_result = Convert.ToBoolean(regkey.GetValue("DisableDnsOverTcpParsing"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_DNSOverTCP", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_Antivirus_S_Parser_DNSOverTCP", "Read registry", ex.ToString());
                return false;
            }
        }

        public static bool Microsoft_Defender_AntiVirus_Contr_Folder_Acc_Enabled()
        {
            try
            {
                bool registry_result = false;

                RegistryKey regkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access");
                registry_result = Convert.ToBoolean(regkey.GetValue("EnableControlledFolderAccess"));
                regkey.Close();

                Logging.Handler.Microsoft_Defender_Antivirus("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_AntiVirus_Contr_Folder_Acc_Enabled", "Read registry", registry_result.ToString());

                //Return Result
                if (registry_result)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Microsoft_Defender_AntiVirus.Check_Settings.Microsoft_Defender_AntiVirus_Contr_Folder_Acc_Enabled", "Read registry", ex.ToString());
                return false;
            }
        }
    }
}
