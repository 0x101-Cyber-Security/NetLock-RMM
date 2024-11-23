using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Comm_Agent_Windows.Helper
{
    internal class Network
    {
        public static string Get_Local_IP_Address()
        {
            try
            {
                // Retrieve all network interfaces of the system
                NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                // Select the first network interface that has a valid IPv4 address
                NetworkInterface ipv4Interface = networkInterfaces.FirstOrDefault(
                    nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                           nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                           nic.GetIPProperties().GatewayAddresses.Any() &&
                           nic.GetIPProperties().UnicastAddresses.Any(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork));

                if (ipv4Interface != null)
                {
                    // Return the first valid IPv4 address of the selected network adapter
                    return ipv4Interface.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        .Address.ToString();
                }
                else
                {
                    return "-";
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("NetLock_RMM_Comm_Agent_Windows.Helper.Network", "Get_Local_IP_Address", ex.ToString());
                return "-";
            }
        }
    }
}
