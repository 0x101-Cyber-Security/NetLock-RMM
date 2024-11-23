using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime;
using System.Security.Policy;
using System.Text.RegularExpressions;
using NetLock_RMM_Health_Agent_Windows;

namespace Helper
{
    internal class Check_Connection
    {
        public static async Task Check_Servers()
        {
            try
            {
                // Check connections to update server. Split update_servers with "," and check each server
                List<string> values = new List<string>(NetLock_RMM_Health_Agent_Windows.Service.update_servers.Split(','));

                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "update_servers"))
                    {
                        NetLock_RMM_Health_Agent_Windows.Service.update_server = value;
                        NetLock_RMM_Health_Agent_Windows.Service.update_server_status = true;
                        Logging.Handler.Debug("Initialization.Check_Connection.Check_Servers", "Update server connection successful.", "");
                        break;
                    }
                    else
                    {
                        NetLock_RMM_Health_Agent_Windows.Service.update_server_status = false;
                        Logging.Handler.Error("Initialization.Check_Connection.Check_Servers", "Update server connection failed.", "");
                    }
                }

                // Check connections to trust server. Split trust_servers with "," and check each server
                values = new List<string>(NetLock_RMM_Health_Agent_Windows.Service.trust_servers.Split(','));

                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "trust_servers"))
                    {
                        NetLock_RMM_Health_Agent_Windows.Service.trust_server = value;
                        NetLock_RMM_Health_Agent_Windows.Service.trust_server_status = true;
                        Logging.Handler.Debug("Initialization.Check_Connection.Check_Servers", "Trust server connection successful.", "");
                        break;
                    }
                    else
                    {
                        NetLock_RMM_Health_Agent_Windows.Service.trust_server_status = false;
                        Logging.Handler.Error("Initialization.Check_Connection.Check_Servers", "Trust server connection failed.", "");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Initialization.Check_Connection.Check_Servers", "Failed", ex.ToString());
            }
        }

        public static async Task<bool> Hostname_IP_Port(string server, string description)
        {
            Console.WriteLine("[" + DateTime.Now + "] - [Helper.Check_Connection.Hostname_IP_Port] -> Server: " + server + " Description: " + description);
            Logging.Handler.Debug("Check_Connection", "Hostname_IP_Port", "Server: " + server + " Description: " + description);

            string communication_server_regex = @"([a-zA-Z0-9\-\.]+):\d+";
            string communication_server_port_regex = @"(?<=:)(\d+)";

            // Extract server name
            Match server_match = Regex.Match(server, communication_server_regex);
            Match port_match = Regex.Match(server, communication_server_port_regex);
         
            // Output server name
            if (server_match.Success)
            {
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Check_Connection.Hostname_IP_Port] -> Server: " + server_match.Groups[0].Value);
                Logging.Handler.Debug("Check_Connection", "Hostname_IP_Port", "Server: " + server_match.Groups[0].Value);
            }
            else
            {
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Check_Connection.Hostname_IP_Port] -> Server could not be extracted.");
                Logging.Handler.Debug("Check_Connection", "Hostname_IP_Port", "Server could not be extracted.");
            }

            // Output port
            if (port_match.Success) 
            {
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Check_Connection.Hostname_IP_Port] -> Port: " + port_match.Groups[0].Value);
                Logging.Handler.Debug("Check_Connection", "Hostname_IP_Port", "Port: " + port_match.Groups[0].Value);
            }
            else
            {
                Console.WriteLine("[" + DateTime.Now + "] - [Helper.Check_Connection.Hostname_IP_Port] -> Port could not be extracted.");
                Logging.Handler.Debug("Check_Connection", "Hostname_IP_Port", "Port could not be extracted.");
            }

            // Check main communication server
            try
            {
                using (var client = new TcpClient())
                {
                    // Check if main communication server is set
                    if (server_match.Success && port_match.Success)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Helper.Check_Connection.Hostname_IP_Port] -> Server: " + server_match.Groups[1].Value + " Port: " + port_match.Groups[0].Value);
                        Logging.Handler.Debug("Check_Connection", "Hostname_IP_Port", "Server: " + server_match.Groups[1].Value + " Port: " + port_match.Groups[1].Value);

                        // Connect to the main communication server
                        await client.ConnectAsync(server_match.Groups[1].Value, Convert.ToInt32(port_match.Groups[0].Value));
                        
                        Logging.Handler.Debug("Check_Connection", "Hostname_IP_Port", "Connection to communication server successful");

                        return true;
                    }
                    else
                    {
                        Logging.Handler.Error("Check_Connection", "Hostname_IP_Port", "Server or port empty.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Check_Connection", "Hostname_IP_Port", ex.ToString());
            }

            return false;
        }
    }
}
