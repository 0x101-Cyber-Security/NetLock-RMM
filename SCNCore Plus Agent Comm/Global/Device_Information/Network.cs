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
using System.Net;
using Linux.Helper;
using MacOS.Helper;

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
                    string output = Linux.Helper.Bash.Execute_Script("Network_Adapter_Information", false, "ip link show");

                    // Split the output into individual network adapters
                    var networkAdapters = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var adapter in networkAdapters)
                    {
                        string[] adapterDetails = adapter.Split(":");
                        string adapterName = adapterDetails[1].Trim();

                        // Run the 'ip' command to get IP address and other details for each adapter
                        string output2 = Linux.Helper.Bash.Execute_Script("", false, $"ip addr show {adapterName}"); 

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
                    // List for saving the JSON data for each network adapter
                    List<string> network_adapterJsonList = new List<string>();

                    // Execute the 'ifconfig' command to get the network adapter information
                    string output = MacOS.Helper.Zsh.Execute_Script("Network_Adapter_Information", false, "ifconfig -a");

                    // Divide the output into individual adapter sections
                    var networkAdapters = output.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var adapter in networkAdapters)
                    {
                        // Extract the name of the network adapter
                        string[] lines = adapter.Split('\n');
                        string adapterName = lines[0].Split(':')[0].Trim();

                        // Adapter type
                        string adapterType = "N/A";

                        try
                        {
                            string networksetupOutput = MacOS.Helper.Zsh.Execute_Script("Network_Adapter_Information", false, "networksetup -listallhardwareports");
                            var match = Regex.Match(networksetupOutput, $"Hardware Port: (.+?)\\s+Device: {adapterName}");
                            if (match.Success)
                            {
                                adapterType = match.Groups[1].Value;
                            }
                        }
                        catch
                        {
                            adapterType = "Unknown"; // Fallback for errors
                        }

                        // Initialise variables
                        string ipv4Address = "N/A";
                        string ipv6Address = "N/A";
                        string macAddress = "N/A";

                        // Extract IPv4 address
                        var ipv4Match = Regex.Match(adapter, @"inet\s+(\d+\.\d+\.\d+\.\d+)");
                        if (ipv4Match.Success)
                        {
                            ipv4Address = ipv4Match.Groups[1].Value;
                        }

                        // Extract IPv6 address
                        var ipv6Match = Regex.Match(adapter, @"inet6\s+([a-fA-F0-9:]+)");
                        if (ipv6Match.Success)
                        {
                            ipv6Address = ipv6Match.Groups[1].Value;
                        }

                        // Extract MAC address
                        var macMatch = Regex.Match(adapter, @"ether\s+([a-fA-F0-9:]+)");
                        if (macMatch.Success)
                        {
                            macAddress = macMatch.Groups[1].Value;
                        }

                        // Extract link speed (using networksetup or system_profiler)
                        string link_speed = "N/A";
                        //var linkSpeedMatch = MacOS.Helper.Zsh.Execute_Script("Network_Adapter_Information", false, $"networksetup -getmedia {adapterName}");
                        //link_speed = linkSpeedMatch.Trim();

                        // DHCP status can be checked using ipconfig (if available)
                        string dhcp_enabled = "N/A";

                        var dhcpMatch = Zsh.Execute_Script("Network_Adapter_Information", false, $"ipconfig getpacket {adapterName}");
                        if (!string.IsNullOrEmpty(dhcpMatch) && dhcpMatch.Contains("dhcp"))
                            dhcp_enabled = "True";
                        else
                            dhcp_enabled = "False";

                        // Subnet mask extraction (ifconfig example)
                        string subnet_mask = "N/A";
                        var subnetMatch = Zsh.Execute_Script("Network_Adapter_Information", false, $"ifconfig {adapterName}");
                        var subnetMaskMatch = Regex.Match(subnetMatch, @"netmask\s+([a-fA-F0-9:]+)");

                        if (subnetMaskMatch.Success)
                            subnet_mask = subnetMaskMatch.Groups[1].Value;

                        // Send and receive stats can be gathered from netstat or other tools
                        string sending = "N/A";
                        string receive = "N/A";

                        //var netstatOutput = MacOS.Helper.Zsh.Execute_Script("Network_Adapter_Information", false, "netstat -i");

                        sending = "N/A"; // Requires statistics tools such as 'netstat'
                        receive = "N/A"; // Requires statistics tools such as 'netstat'

                        // Create network adapter object
                        Network_Adapters network_adapterInfo = new Network_Adapters
                        {
                            name = adapterName ?? "N/A",
                            description = adapterName ?? "N/A",
                            manufacturer = "N/A", // not available on MacOS
                            type = adapterType, // Can be further specified if required
                            link_speed = link_speed, // Link Speed requires advanced tools such as 'networksetup'
                            service_name = adapterName ?? "N/A",
                            dns_domain = "N/A", // Can be supplemented by further commands such as `scutil --dns`.
                            dns_hostname = Dns.GetHostName(),
                            dhcp_enabled = dhcp_enabled, // Requires advanced parsing or tools
                            ipv4_address = ipv4Address ?? "N/A",
                            ipv6_address = ipv6Address ?? "N/A",
                            subnet_mask = subnet_mask, // Can be supplemented by further commands such as `ifconfig`.
                            mac_address = macAddress ?? "N/A",
                            sending = "N/A", // Requires statistics tools such as 'netstat'
                            receive = "N/A" // Requires statistics tools such as 'netstat'
                        };

                        // Create JSON of the network adapter and add to the list
                        string network_adapterJson = JsonSerializer.Serialize(network_adapterInfo, new JsonSerializerOptions { WriteIndented = true });
                        network_adapterJsonList.Add(network_adapterJson);
                    }

                    // Complete output of the network adapter information
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

        public static bool Firewall_Status()
        {
            try
            {
                if (OperatingSystem.IsLinux())
                {
                    // Check whether UFW is installed
                    string ufwCheck = Bash.Execute_Script("Check_UFW_Installed", false, "command -v ufw");

                    if (string.IsNullOrWhiteSpace(ufwCheck))
                    {
                        // UFW is not installed
                        Logging.Debug("Linux.Helper.Linux.Firewall_Status", "UFW is not installed on this system.", "");
                        return false;
                    }

                    // Check the status of the firewall
                    string firewallStatus = Bash.Execute_Script("Check_Firewall_Status", false, "ufw status");

                    // Check whether UFW is active
                    if (firewallStatus.Contains("active"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    // Firewall states:
                    // 0 = Off
                    // 1 = On for specific services
                    // 2 = On for essential services

                    // Check if the firewall is enabled
                    string firewallStatus = Zsh.Execute_Script("Firewall_Status", false, "defaults read /Library/Preferences/com.apple.alf globalstate");

                    if (firewallStatus.Contains("1"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("MacOS.Helper.MacOS.Firewall_Status", "Error checking firewall status", ex.ToString());
                return false;
            }
        }
    }
}
