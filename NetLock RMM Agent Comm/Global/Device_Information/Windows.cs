using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using NetLock_RMM_Comm_Agent_Windows.Helper;
using Newtonsoft.Json;
using static NetLock_RMM_Comm_Agent_Windows.Online_Mode.Handler;


namespace NetLock_RMM_Comm_Agent_Windows.Device_Information
{
    internal class Windows
    {
        public static string Windows_Version()
        {
            string operating_system = "-";

            try
            {
                bool windows11 = false;

                string _CurrentBuild = Registry.HKLM_Read_Value("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "CurrentBuild");
                string _ProductName = Registry.HKLM_Read_Value("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "ProductName");
                string _DisplayVersion = Registry.HKLM_Read_Value("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "DisplayVersion");

                int CurrentBuild = Convert.ToInt32(_CurrentBuild);

                // If the build number is greater than or equal to 22000, it is Windows 11. Older windows 11 versions had wrong product names, stating they are windows 11. Knowlege from NetLock Legacy
                if (CurrentBuild > 22000 || CurrentBuild == 22000)
                    windows11 = true;

                if (windows11 == true)
                    operating_system = "Windows 11" + " (" + _DisplayVersion + ")";
                else
                    operating_system = _ProductName + " (" + _DisplayVersion + ")";

                if (operating_system == null)
                    operating_system = "";

                return operating_system;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("NetLock_RMM_Comm_Agent_Windows.Helper.Windows.Windows_Version", "Collect windows product name & version", ex.Message);
                return "-";
            }
        }

        public static string Antivirus_Products()
        {
            try
            {   // Create a list of JSON strings for each antivirus product
                List<string> antivirus_productsJsonList = new List<string>();

                // Get the antivirus products from the AntiVirusProduct class in the SecurityCenter2 namespace (Windows Security Center), which is available since Windows 10 version 1703 (Creators Update) and Windows Server 2016 (Windows 10 version 1607)
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT * FROM AntiVirusProduct"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        // Create antivirus product JSON object
                        Antivirus_Products antivirus_productsInfo = new Antivirus_Products
                        {
                            display_name = obj["displayName"]?.ToString() ?? "N/A",
                            instance_guid = obj["instanceGuid"]?.ToString() ?? "N/A",
                            path_to_signed_product_exe = obj["pathToSignedProductExe"]?.ToString() ?? "N/A",
                            path_to_signed_reporting_exe = obj["pathToSignedReportingExe"]?.ToString() ?? "N/A",
                            product_state = obj["productState"]?.ToString() ?? "N/A",
                            timestamp = obj["timestamp"]?.ToString() ?? "N/A",
                        };

                        // Serialize the antivirus product object into a JSON string and add it to the list
                        string network_adapterJson = JsonConvert.SerializeObject(antivirus_productsInfo, Formatting.Indented);
                        antivirus_productsJsonList.Add(network_adapterJson);
                    }
                }

                // Return the list of antivirus products as a JSON array
                string antivirus_products_json = "[" + string.Join("," + Environment.NewLine, antivirus_productsJsonList) + "]";
                Logging.Handler.Device_Information("Device_Information.Windows.Antivirus_Products", "antivirus_products_json", antivirus_products_json);
                return antivirus_products_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Windows.Antivirus_Products", "Collect antivirus products", ex.Message);
                return "[]";
            }
        }

        public static string Antivirus_Information()
        {
            try
            {
                string antivirus_information_json = "{}";

                // Get the antivirus information from the AntiVirusProduct class in the SecurityCenter2 namespace (Windows Security Center), which is available since Windows 10 version 1703 (Creators Update) and Windows Server 2016 (Windows 10 version 1607)
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("Root\\Microsoft\\Windows\\Defender", "SELECT * FROM MSFT_MpComputerStatus"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        // Create antivirus information object
                        Antivirus_Information antivirus_information = new Antivirus_Information
                        {
                            amengineversion = obj["AMEngineVersion"]?.ToString() ?? "N/A",
                            amproductversion = obj["AMProductVersion"]?.ToString() ?? "N/A",
                            amserviceenabled = Convert.ToBoolean(obj["AMServiceEnabled"]),
                            amserviceversion = obj["AMServiceVersion"]?.ToString() ?? "N/A",
                            antispywareenabled = Convert.ToBoolean(obj["AntispywareEnabled"]),
                            antispywaresignaturelastupdated = obj["AntispywareSignatureLastUpdated"]?.ToString() ?? "N/A",
                            antispywaresignatureversion = obj["AntispywareSignatureVersion"]?.ToString() ?? "N/A",
                            antivirusenabled = Convert.ToBoolean(obj["AntivirusEnabled"]),
                            antivirussignaturelastupdated = obj["AntivirusSignatureLastUpdated"]?.ToString() ?? "N/A",
                            antivirussignatureversion = obj["AntivirusSignatureVersion"]?.ToString() ?? "N/A",
                            behaviormonitorenabled = Convert.ToBoolean(obj["BehaviorMonitorEnabled"]),
                            ioavprotectionenabled = Convert.ToBoolean(obj["IoavProtectionEnabled"]),
                            istamperprotected = Convert.ToBoolean(obj["IsTamperProtected"]),
                            nisenabled = Convert.ToBoolean(obj["NISEnabled"]),
                            nisengineversion = obj["NISEngineVersion"]?.ToString() ?? "N/A",
                            nissignaturelastupdated = obj["NISSignatureLastUpdated"]?.ToString() ?? "N/A",
                            nissignatureversion = obj["NISSignatureVersion"]?.ToString() ?? "N/A",
                            onaccessprotectionenabled = Convert.ToBoolean(obj["OnAccessProtectionEnabled"]),
                            realtimetprotectionenabled = Convert.ToBoolean(obj["RealTimeProtectionEnabled"]),
                        };
    
                        // Serialize the antivirus information object into a JSON string
                        antivirus_information_json = JsonConvert.SerializeObject(antivirus_information, Formatting.Indented);
                        Logging.Handler.Device_Information("Device_Information.Windows.Antivirus_Information", "antivirus_information_json", antivirus_information_json);
                    }
                }

                // Return the antivirus information as a JSON object
                return antivirus_information_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Windows.Antivirus_Information", "Collect antivirus information", ex.Message);
                return "{}";
            }
        }
    }
}
