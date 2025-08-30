using Global.Helper;
using NetLock_RMM_Agent_Comm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Global.Online_Mode.Handler;

namespace Windows.Helper
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
                Logging.Error("NetLock_RMM_Comm_Agent_Windows.Helper.Windows.Windows_Version", "Collect windows product name & version", ex.Message);
                return "-";
            }
        }

        public static string Antivirus_Products()
        {
            try
            {
                // Liste zum Sammeln der aktuellen Antivirus-Produkte
                List<Antivirus_Products> currentProducts = new List<Antivirus_Products>();
                List<string> antivirus_productsJsonList = new List<string>();

                if (OperatingSystem.IsWindows())
                {
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

                            // Objekt zur Vergleichsliste hinzufügen
                            currentProducts.Add(antivirus_productsInfo);

                            // Serialize the antivirus product object into a JSON string and add it to the list
                            string antivirus_productJson = JsonSerializer.Serialize(antivirus_productsInfo, new JsonSerializerOptions { WriteIndented = true });
                            antivirus_productsJsonList.Add(antivirus_productJson);
                        }
                    }
                }
                else
                {
                    Logging.Debug("Device_Information.Windows.Antivirus_Products", "Operating system is not Windows", "");
                    // If the operating system is not Windows, return an empty list
                    return "[]";
                }

                // Return the list of antivirus products as a JSON array
                string antivirus_products_json = "[" + string.Join("," + Environment.NewLine, antivirus_productsJsonList) + "]";
                Logging.Device_Information("Device_Information.Windows.Antivirus_Products", "antivirus_products_json", antivirus_products_json);

                // Objektbasierter Vergleich statt String-Vergleich
                bool hasChanges = false;
                
                Console.WriteLine("Checking for antivirus product changes...");
                
                if (string.IsNullOrEmpty(Device_Worker.antivirusProductsJson) || Device_Worker.antivirusProductsJson == "[]")
                {
                    hasChanges = true;
                    Logging.Device_Information("Device_Information.Windows.Antivirus_Products", "Erste Erfassung oder leere vorherige Daten", "");
                    Console.WriteLine("Antivirus_Products: Erste Erfassung oder leere vorherige Daten");
                }
                else
                {
                    try
                    {
                        Console.WriteLine("Antivirus_Products: Versuche, vorherige Antivirenprodukte aus JSON zu deserialisieren...");
                        
                        // Deserialize den gespeicherten JSON zu einer Liste von Objekten
                        var previousProducts = JsonSerializer.Deserialize<List<Antivirus_Products>>(
                            Device_Worker.antivirusProductsJson.StartsWith("[") ? 
                            Device_Worker.antivirusProductsJson : 
                            "[]") ?? new List<Antivirus_Products>();

                        Console.WriteLine($"Antivirus_Products: Vorherige Produkte geladen: {previousProducts.Count}, aktuelle Produkte: {currentProducts.Count}");

                        // Prüfe, ob Anzahl unterschiedlich ist
                        if (previousProducts.Count != currentProducts.Count)
                        {
                            hasChanges = true;
                            Logging.Device_Information("Device_Information.Windows.Antivirus_Products", 
                                "Änderungen erkannt: Unterschiedliche Anzahl von Antivirenprodukten", 
                                $"Vorher: {previousProducts.Count}, Aktuell: {currentProducts.Count}");
                            Console.WriteLine("Antivirus_Products: Anzahl der Antivirenprodukte hat sich geändert.");
                        }
                        else
                        {
                            Console.WriteLine("Antivirus_Products: Anzahl der Antivirenprodukte unverändert, prüfe Details...");
                            
                            // Erstelle ein Dictionary für schnellen Zugriff und Vergleich
                            var currentDict = new Dictionary<string, Antivirus_Products>();
                            foreach (var product in currentProducts)
                            {
                                // Verwende GUID als eindeutigen Schlüssel
                                string key = product.instance_guid ?? "N/A";
                                if (!currentDict.ContainsKey(key))
                                    currentDict[key] = product;
                            }

                            // Vergleiche jedes vorherige Produkt mit den aktuellen
                            foreach (var product in previousProducts)
                            {
                                string key = product.instance_guid ?? "N/A";
                                
                                // Wenn ein vorheriges Produkt nicht mehr vorhanden ist oder sich wichtige Eigenschaften geändert haben
                                if (!currentDict.TryGetValue(key, out var currentProduct) ||
                                    product.display_name != currentProduct.display_name ||
                                    product.path_to_signed_product_exe != currentProduct.path_to_signed_product_exe ||
                                    product.path_to_signed_reporting_exe != currentProduct.path_to_signed_reporting_exe ||
                                    product.product_state != currentProduct.product_state)
                                {
                                    hasChanges = true;
                                    Logging.Device_Information("Device_Information.Windows.Antivirus_Products", 
                                        "Änderungen erkannt bei Antivirenprodukt", product.display_name);
                                    Console.WriteLine($"Antivirus_Products: Produkt geändert oder entfernt: {product.display_name}");
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Bei Deserialisierungsproblemen als Änderung behandeln
                        hasChanges = true;
                        Logging.Error("Device_Information.Windows.Antivirus_Products", 
                            "Fehler beim Vergleich", ex.ToString());
                        Console.WriteLine($"Antivirus_Products: Fehler beim Deserialisieren oder Vergleichen: {ex.Message}");
                    }
                }

                if (!hasChanges)
                {
                    Logging.Debug("Device_Information.Windows.Antivirus_Products", "Antivirus products have not changed", "");
                    Console.WriteLine("Antivirus_Products: Keine Änderungen erkannt.");
                    return "-";
                }
                else
                {
                    Console.WriteLine("Antivirus_Products: Neue Daten gefunden, aktualisiere antivirusProductsJson.");
                    Device_Worker.antivirusProductsJson = antivirus_products_json;
                    return antivirus_products_json;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Windows.Antivirus_Products", "Collect antivirus products", ex.Message);
                return "[]";
            }
        }

        public static string Antivirus_Information()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    string antivirus_information_json = "{}";
                    Antivirus_Information currentAntivirusInfo = null;

                    // Get the antivirus information from Windows Defender
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("Root\\Microsoft\\Windows\\Defender", "SELECT * FROM MSFT_MpComputerStatus"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            // Create antivirus information object
                            currentAntivirusInfo = new Antivirus_Information
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
                            antivirus_information_json = JsonSerializer.Serialize(currentAntivirusInfo, new JsonSerializerOptions { WriteIndented = true });
                            Logging.Device_Information("Device_Information.Windows.Antivirus_Information", "antivirus_information_json", antivirus_information_json);
                        }
                    }

                    // Wenn kein Antiviren-Objekt gefunden wurde
                    if (currentAntivirusInfo == null)
                    {
                        Logging.Device_Information("Device_Information.Windows.Antivirus_Information", "Keine Antiviren-Informationen gefunden", "");
                        return "{}";
                    }

                    // Objektbasierter Vergleich statt String-Vergleich
                    bool hasChanges = false;
                    
                    Console.WriteLine("Checking for antivirus information changes...");
                    
                    if (string.IsNullOrEmpty(Device_Worker.antivirusInformationJson) || Device_Worker.antivirusInformationJson == "{}")
                    {
                        hasChanges = true;
                        Logging.Device_Information("Device_Information.Windows.Antivirus_Information", "Erste Erfassung oder leere vorherige Daten", "");
                        Console.WriteLine("Antivirus_Information: Erste Erfassung oder leere vorherige Daten");
                    }
                    else
                    {
                        try
                        {
                            Console.WriteLine("Antivirus_Information: Versuche, vorherige Antiviren-Information aus JSON zu deserialisieren...");
                            
                            // Deserialize den gespeicherten JSON zu einem Objekt
                            var previousAntivirusInfo = JsonSerializer.Deserialize<Antivirus_Information>(Device_Worker.antivirusInformationJson);

                            if (previousAntivirusInfo == null)
                            {
                                hasChanges = true;
                                Logging.Device_Information("Device_Information.Windows.Antivirus_Information", "Vorherige Daten konnten nicht deserialisiert werden", "");
                            }
                            else
                            {
                                Console.WriteLine("Antivirus_Information: Vergleiche Antiviren-Informationen...");
                                
                                // Überprüfe auf Änderungen bei wichtigen Eigenschaften
                                if (previousAntivirusInfo.amengineversion != currentAntivirusInfo.amengineversion ||
                                    previousAntivirusInfo.amproductversion != currentAntivirusInfo.amproductversion ||
                                    previousAntivirusInfo.amserviceenabled != currentAntivirusInfo.amserviceenabled ||
                                    previousAntivirusInfo.amserviceversion != currentAntivirusInfo.amserviceversion ||
                                    previousAntivirusInfo.antispywareenabled != currentAntivirusInfo.antispywareenabled ||
                                    previousAntivirusInfo.antispywaresignatureversion != currentAntivirusInfo.antispywaresignatureversion ||
                                    previousAntivirusInfo.antivirusenabled != currentAntivirusInfo.antivirusenabled ||
                                    previousAntivirusInfo.antivirussignatureversion != currentAntivirusInfo.antivirussignatureversion ||
                                    previousAntivirusInfo.behaviormonitorenabled != currentAntivirusInfo.behaviormonitorenabled ||
                                    previousAntivirusInfo.ioavprotectionenabled != currentAntivirusInfo.ioavprotectionenabled ||
                                    previousAntivirusInfo.istamperprotected != currentAntivirusInfo.istamperprotected ||
                                    previousAntivirusInfo.nisenabled != currentAntivirusInfo.nisenabled ||
                                    previousAntivirusInfo.nisengineversion != currentAntivirusInfo.nisengineversion ||
                                    previousAntivirusInfo.nissignatureversion != currentAntivirusInfo.nissignatureversion ||
                                    previousAntivirusInfo.onaccessprotectionenabled != currentAntivirusInfo.onaccessprotectionenabled ||
                                    previousAntivirusInfo.realtimetprotectionenabled != currentAntivirusInfo.realtimetprotectionenabled)
                                {
                                    hasChanges = true;
                                    Logging.Device_Information("Device_Information.Windows.Antivirus_Information", 
                                        "Änderungen an Antiviren-Einstellungen oder Versionen erkannt", "");
                                    Console.WriteLine("Antivirus_Information: Änderungen an Konfiguration oder Versionen erkannt");
                                }
                                // Optional: Separat auf Signatur-Update-Zeitstempel prüfen
                                else if (previousAntivirusInfo.antispywaresignaturelastupdated != currentAntivirusInfo.antispywaresignaturelastupdated ||
                                         previousAntivirusInfo.antivirussignaturelastupdated != currentAntivirusInfo.antivirussignaturelastupdated ||
                                         previousAntivirusInfo.nissignaturelastupdated != currentAntivirusInfo.nissignaturelastupdated)
                                {
                                    hasChanges = true;
                                    Logging.Device_Information("Device_Information.Windows.Antivirus_Information", 
                                        "Signatur-Updates erkannt", "");
                                    Console.WriteLine("Antivirus_Information: Neue Signatur-Updates erkannt");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Bei Deserialisierungsproblemen als Änderung behandeln
                            hasChanges = true;
                            Logging.Error("Device_Information.Windows.Antivirus_Information", 
                                "Fehler beim Vergleich", ex.ToString());
                            Console.WriteLine($"Antivirus_Information: Fehler beim Deserialisieren oder Vergleichen: {ex.Message}");
                        }
                    }

                    if (!hasChanges)
                    {
                        Logging.Device_Information("Device_Information.Windows.Antivirus_Information", "No changes in antivirus information detected.", "");
                        Console.WriteLine("Antivirus_Information: Keine Änderungen erkannt.");
                        return "-";
                    }
                    else
                    {
                        Console.WriteLine("Antivirus_Information: Neue Daten gefunden, aktualisiere antivirusInformationJson.");
                        Device_Worker.antivirusInformationJson = antivirus_information_json;
                        return antivirus_information_json;
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Windows.Antivirus_Information", "Collect antivirus information", ex.Message);
                    return "{}";
                }
            }
            else
            {
                Logging.Debug("Device_Information.Windows.Antivirus_Information", "Operating system is not Windows", "");
                return "{}";
            }
        }
    }
}
