using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static NetLock_RMM_Comm_Agent_Windows.Online_Mode.Handler;

namespace NetLock_RMM_Comm_Agent_Windows.Device_Information
{
    internal class Network
    {
        public static string Network_Adapter_Information()
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
                        string network_adapterJson = JsonConvert.SerializeObject(network_adapterInfo, Formatting.Indented);
                        network_adapterJsonList.Add(network_adapterJson);
                    }
                }

                string network_adapters_json = "[" + string.Join("," + Environment.NewLine, network_adapterJsonList) + "]";
                Logging.Handler.Device_Information("Device_Information.Network.Network_Adapter_Information", "network_adapters_json", network_adapters_json);
                return network_adapters_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Network.Network_Adapter_Information", "Error", ex.ToString());
                return "[]";
            }
        }

        public static bool Ping(string address, int timeout)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(address, timeout);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Fehler beim Senden der Ping-Anfrage
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            Logging.Handler.Device_Information("Device_Information.Network.Ping", "address pingable", address + " (" + pingable.ToString() + ")");

            return pingable;
        }
    }
}
