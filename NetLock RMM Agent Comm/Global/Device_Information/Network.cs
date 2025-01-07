using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Global.Online_Mode.Handler;
using Global.Helper;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Global.Device_Information
{
    internal class Network
    {
        public static string Network_Adapter_Information()
        {
            string network_adapters_json = String.Empty;

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // Create a list of JSON strings for each network adapter
                    List<string> network_adapterJsonList = new List<string>();

                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_NetworkAdapterConfiguration")) //  WHERE IPEnabled = 'TRUE'
                    {
                        // Get the network adapter's configuration
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            string description_Win32_PerfFormattedData_Tcpip_NetworkInterface = obj["Description"]?.ToString()?.Replace("(", "[")?.Replace(")", "]") ?? "N/A"; // We need to convert it, because they seem to mask names different in the two classes

                            string BytesSentPersec = string.Empty;
                            string BytesReceivedPersec = string.Empty;
                            string manufacturer = string.Empty;
                            string type = string.Empty;
                            string link_speed = string.Empty;

                            // Get the network adapter's sending and receiving data from the Win32_PerfFormattedData_Tcpip_NetworkInterface class
                            using (ManagementObjectSearcher searcher1 = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PerfFormattedData_Tcpip_NetworkInterface WHERE Name = '" + description_Win32_PerfFormattedData_Tcpip_NetworkInterface + "'"))
                            {
                                foreach (ManagementObject obj1 in searcher1.Get())
                                {
                                    BytesSentPersec = obj1["BytesSentPersec"]?.ToString() ?? "N/A";
                                    BytesReceivedPersec = obj1["BytesReceivedPersec"]?.ToString() ?? "N/A";

                                    BytesSentPersec = Math.Floor(Convert.ToDouble(BytesSentPersec ?? "0") * 8 / 1000).ToString();
                                    BytesReceivedPersec = Math.Floor(Convert.ToDouble(BytesReceivedPersec ?? "0") * 8 / 1000).ToString();
                                }
                            }

                            // Get the network adapter's type, link speed and Manufacturer the Win32_NetworkAdapter class
                            using (ManagementObjectSearcher searcher2 = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_NetworkAdapter WHERE Description = '" + obj["Description"].ToString() + "'"))
                            {
                                foreach (ManagementObject obj2 in searcher2.Get())
                                {
                                    manufacturer = obj2["Manufacturer"]?.ToString() ?? "N/A";
                                    type = obj2["AdapterType"]?.ToString() ?? "N/A";
                                    link_speed = obj2["Speed"]?.ToString() ?? string.Empty;

                                    // Convert bits to MBit/s (1 MBit = 1,000,000 bits)
                                    if (!String.IsNullOrEmpty(link_speed))
                                    {
                                        link_speed = (Convert.ToDouble(link_speed) / 1000000).ToString();
                                    }
                                    else
                                        link_speed = "N/A";
                                }
                            }

                            // Create a network_adapter object
                            Network_Adapters network_adapterInfo = new Network_Adapters
                            {
                                name = obj["Caption"]?.ToString() ?? "N/A",
                                description = obj["Description"]?.ToString() ?? "N/A",
                                manufacturer = manufacturer ?? "N/A",
                                type = type ?? "N/A",
                                link_speed = link_speed ?? "N/A",
                                service_name = obj["ServiceName"]?.ToString() ?? "N/A",
                                dns_domain = obj["DNSDomain"]?.ToString() ?? "N/A",
                                dns_hostname = obj["DNSHostName"]?.ToString() ?? "N/A",
                                dhcp_enabled = obj["DHCPEnabled"]?.ToString() ?? "N/A",
                                dhcp_server = obj["DHCPServer"]?.ToString() ?? "N/A",
                                ipv4_address = ((string[])obj["IPAddress"])?.FirstOrDefault() ?? "N/A",
                                ipv6_address = ((string[])obj["IPAddress"])?.Skip(1).FirstOrDefault() ?? "N/A",
                                subnet_mask = ((string[])obj["IPSubnet"])?.FirstOrDefault() ?? "N/A",
                                mac_address = obj["MACAddress"]?.ToString() ?? "N/A",
                                sending = BytesSentPersec?.ToString() ?? "N/A",
                                receive = BytesReceivedPersec?.ToString() ?? "N/A",
                            };


                            // Serialize the network_adapter object into a JSON string and add it to the list
                            string network_adapterJson = JsonSerializer.Serialize(network_adapterInfo, new JsonSerializerOptions { WriteIndented = true });
                            network_adapterJsonList.Add(network_adapterJson);
                        }
                    }

                    network_adapters_json = "[" + string.Join("," + Environment.NewLine, network_adapterJsonList) + "]";
                    Logging.Device_Information("Device_Information.Network.Network_Adapter_Information", "network_adapters_json", network_adapters_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Network.Network_Adapter_Information", "Error", ex.ToString());
                    return "[]";
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    // Create a list of JSON strings for each network adapter
                    List<string> network_adapterJsonList = new List<string>();

                    // Run the 'ip link' command to get a list of network adapters
                    string output = Linux.Helper.Bash.Execute_Command("ip link show");

                    // Split the output into individual network adapters
                    var networkAdapters = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var adapter in networkAdapters)
                    {
                        string[] adapterDetails = adapter.Split(":");
                        string adapterName = adapterDetails[1].Trim();

                        // Run the 'ip' command to get IP address and other details for each adapter
                        string output2 = Linux.Helper.Bash.Execute_Command($"ip addr show {adapterName}");

                        // Parse the necessary information from the second command output
                        string ipv4Address = "N/A";
                        string ipv6Address = "N/A";
                        string macAddress = "N/A";
                        string linkSpeed = "N/A"; // Note: This may require additional tools like ethtool for speed

                        // Example parsing for IPv4 address
                        var ipv4Match = Regex.Match(output2, @"inet\s+(\d+\.\d+\.\d+\.\d+)");
                        if (ipv4Match.Success)
                        {
                            ipv4Address = ipv4Match.Groups[1].Value;
                        }

                        // Example parsing for IPv6 address
                        var ipv6Match = Regex.Match(output2, @"inet6\s+([a-fA-F0-9:]+)");
                        if (ipv6Match.Success)
                        {
                            ipv6Address = ipv6Match.Groups[1].Value;
                        }

                        // Example parsing for MAC address
                        var macMatch = Regex.Match(output2, @"link/ether\s+([a-fA-F0-9:]+)");
                        if (macMatch.Success)
                        {
                            macAddress = macMatch.Groups[1].Value;
                        }

                        // Create a network_adapter object
                        Network_Adapters network_adapterInfo = new Network_Adapters
                        {
                            name = adapterName ?? "N/A",
                            description = adapterName ?? "N/A", // Description may need more specific extraction
                            manufacturer = "N/A", // This could be added using additional tools or commands
                            type = "N/A", // Adapter type is not easily available in standard Linux commands
                            link_speed = linkSpeed ?? "N/A", // Link speed can be fetched using ethtool (optional)
                            service_name = "N/A", // Linux typically doesn't provide this in the same way as Windows
                            dns_domain = "N/A", // DNS domain info isn't typically available directly
                            dns_hostname = "N/A", // Hostname is often separate from adapter details
                            dhcp_enabled = "N/A", // DHCP status can be checked using network manager or other tools
                            ipv4_address = ipv4Address ?? "N/A",
                            ipv6_address = ipv6Address ?? "N/A",
                            subnet_mask = "N/A", // This could be parsed from the 'ip addr' output
                            mac_address = macAddress ?? "N/A",
                            sending = "N/A", // Linux doesn't provide this directly in the 'ip' output
                            receive = "N/A", // Same as sending, this would need a separate tool for statistics
                        };

                        // Serialize the network_adapter object into a JSON string and add it to the list
                        string network_adapterJson = JsonSerializer.Serialize(network_adapterInfo, new JsonSerializerOptions { WriteIndented = true });
                        network_adapterJsonList.Add(network_adapterJson);
                    }

                    network_adapters_json = "[" + string.Join("," + Environment.NewLine, network_adapterJsonList) + "]";
                    Logging.Device_Information("Device_Information.Network.Network_Adapter_Information", "network_adapters_json", network_adapters_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Network.Network_Adapter_Information", "Error", ex.ToString());
                    return "[]";
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    // Liste für JSON-Strings der Netzwerkadapter
                    List<string> network_adapterJsonList = new List<string>();

                    // Führe den Befehl 'ifconfig' aus, um die Liste der Netzwerkadapter zu erhalten
                    string output = MacOS.Helper.Zsh.Execute_Command("ifconfig -l");

                    // Splitte die Ausgabe in einzelne Netzwerkadapter
                    var networkAdapters = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var adapter in networkAdapters)
                    {
                        string adapterName = adapter.Trim();

                        // Führe den Befehl 'ifconfig <adapterName>' aus, um Details für jeden Adapter zu erhalten
                        string output2 = MacOS.Helper.Zsh.Execute_Command($"ifconfig {adapterName}");

                        // Variablen für die benötigten Informationen
                        string ipv4Address = "N/A";
                        string ipv6Address = "N/A";
                        string macAddress = "N/A";
                        string linkSpeed = "N/A"; // macOS hat keine direkte Möglichkeit, die Link-Geschwindigkeit zu ermitteln
                        string subnetMask = "N/A";

                        // IPv4-Adresse extrahieren
                        var ipv4Match = Regex.Match(output2, @"inet\s+(\d+\.\d+\.\d+\.\d+)");
                        if (ipv4Match.Success)
                        {
                            ipv4Address = ipv4Match.Groups[1].Value;
                        }

                        // IPv6-Adresse extrahieren
                        var ipv6Match = Regex.Match(output2, @"inet6\s+([a-fA-F0-9:]+)");
                        if (ipv6Match.Success)
                        {
                            ipv6Address = ipv6Match.Groups[1].Value;
                        }

                        // MAC-Adresse extrahieren
                        var macMatch = Regex.Match(output2, @"ether\s+([a-fA-F0-9:]+)");
                        if (macMatch.Success)
                        {
                            macAddress = macMatch.Groups[1].Value;
                        }

                        // Subnetzmaske extrahieren
                        var subnetMaskMatch = Regex.Match(output2, @"netmask\s+([a-fA-F0-9]+)");
                        if (subnetMaskMatch.Success)
                        {
                            subnetMask = subnetMaskMatch.Groups[1].Value;
                        }

                        // Erstelle ein Network_Adapters-Objekt
                        Network_Adapters network_adapterInfo = new Network_Adapters
                        {
                            name = adapterName ?? "N/A",
                            description = adapterName ?? "N/A", // Beschreibung kann weiter aus anderen Quellen abgerufen werden
                            manufacturer = "N/A", // Weitere Tools erforderlich, um den Hersteller zu ermitteln
                            type = "N/A", // Adaptertyp nicht direkt verfügbar
                            link_speed = linkSpeed ?? "N/A", // Link-Geschwindigkeit (optional, benötigt spezielle Tools)
                            service_name = "N/A", // macOS bietet dies in ähnlicher Weise nicht wie Windows
                            dns_domain = "N/A", // DNS-Domain nicht direkt verfügbar
                            dns_hostname = "N/A", // Hostname ist häufig separat
                            dhcp_enabled = "N/A", // DHCP-Status kann über 'networksetup' überprüft werden
                            ipv4_address = ipv4Address ?? "N/A",
                            ipv6_address = ipv6Address ?? "N/A",
                            subnet_mask = subnetMask ?? "N/A",
                            mac_address = macAddress ?? "N/A",
                            sending = "N/A", // Sendeinformationen sind hier nicht direkt verfügbar
                            receive = "N/A", // Empfangen ebenfalls nicht ohne zusätzliche Tools
                        };

                        // Serialisiere das Network_Adapters-Objekt in einen JSON-String und füge es der Liste hinzu
                        string network_adapterJson = JsonSerializer.Serialize(network_adapterInfo, new JsonSerializerOptions { WriteIndented = true });
                        network_adapterJsonList.Add(network_adapterJson);
                    }

                    // Kombiniere alle JSON-Strings zu einem großen JSON-Array
                    network_adapters_json = "[" + string.Join("," + Environment.NewLine, network_adapterJsonList) + "]";
                    Logging.Device_Information("Device_Information.Network.Network_Adapter_Information", "network_adapters_json", network_adapters_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Network.Network_Adapter_Information", "Error", ex.ToString());
                    return "[]";
                }
            }
            else
            {
                return "[]";
            }

            return network_adapters_json;
        }

        public static bool Ping(string address, int timeout)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentNullException(nameof(address));

            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(address, timeout);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException ex)
            {
                Logging.Error("Device_Information.Network.Ping", "Ping failed", ex.ToString());
            }
            catch (PlatformNotSupportedException ex)
            {
                Logging.Error("Device_Information.Network.Ping", "Platform not supported", ex.ToString());
            }
            finally
            {
                pinger?.Dispose();
            }

            Logging.Device_Information("Device_Information.Network.Ping", "Address pingable", $"{address} ({pingable})");
            return pingable;
        }
    }
}
