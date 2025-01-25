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

namespace Helper
{
    internal class Check_Connection
    {
        public static async Task<string> Check_Servers(string mode, string servers)
        {
            try
            {
                // Check connection to servers. Split communication_servers with "," and check each server 
                List<string> values = new List<string>(servers.Split(','));

                string server = String.Empty;

                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, mode))
                    {
                        Logging.Handler.Debug("Initialization.Check_Connection.Check_Servers", "Server connection successful.", "");

                        server = value;
                    }
                    else
                    {
                        Logging.Handler.Error("Initialization.Check_Connection.Check_Servers", "Server connection failed.", "");
                    }
                }

                return server;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Initialization.Check_Connection.Check_Servers", "Failed", ex.ToString());
                return String.Empty;
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
