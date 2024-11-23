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
using System.Threading;

namespace NetLock_RMM_Comm_Agent_Windows.Initialization
{
    internal class Check_Connection
    {
        public static async Task Check_Servers()
        {
            try
            {
                // Check connections communication server. Split communication_servers with "," and check each server
                List<string> values = new List<string>(Service.communication_servers.Split(','));

                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "communication_servers"))
                    {
                        Service.communication_server = value;
                        Service.communication_server_status = true;
                        Logging.Handler.Debug("Initialization.Check_Connection.Check_Servers", "Communication server connection successful.", "");
                        break;
                    }
                    else
                    {
                        Service.communication_server_status = false;
                        Logging.Handler.Error("Initialization.Check_Connection.Check_Servers", "Communication server connection failed.", "");
                    }
                }

                // Check connections to remote server. Split remote_servers with "," and check each server
                values = new List<string>(Service.remote_servers.Split(','));

                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "remote_servers"))
                    {
                        Service.remote_server = value;
                        Service.remote_server_status = true;
                        Logging.Handler.Debug("Initialization.Check_Connection.Check_Servers", "Remote server connection successful.", "");
                        break;
                    }
                    else
                    {
                        Service.remote_server_status = false;
                        Logging.Handler.Error("Initialization.Check_Connection.Check_Servers", "Remote server connection failed.", "");
                    }
                }

                // Check connections to update server. Split update_servers with "," and check each server
                values = new List<string>(Service.update_servers.Split(','));

                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "update_servers"))
                    {
                        Service.update_server = value;
                        Service.update_server_status = true;
                        Logging.Handler.Debug("Initialization.Check_Connection.Check_Servers", "Update server connection successful.", "");
                        break;
                    }
                    else
                    {
                        Service.update_server_status = false;
                        Logging.Handler.Error("Initialization.Check_Connection.Check_Servers", "Update server connection failed.", "");
                    }
                }

                // Check connections to trust server. Split trust_servers with "," and check each server
                values = new List<string>(Service.trust_servers.Split(','));
                
                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "trust_servers"))
                    {
                        Service.trust_server = value;
                        Service.trust_server_status = true;
                        Logging.Handler.Debug("Initialization.Check_Connection.Check_Servers", "Trust server connection successful.", "");
                        break;
                    }
                    else
                    {
                        Service.trust_server_status = false;
                        Logging.Handler.Error("Initialization.Check_Connection.Check_Servers", "Trust server connection failed.", "");
                    }
                }

                // Check connections to file server. Split file_servers with "," and check each server
                values = new List<string>(Service.file_servers.Split(','));

                foreach (var value in values) 
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "file_servers"))
                    {
                        Service.file_server = value;
                        Service.file_server_status = true;
                        Logging.Handler.Debug("Initialization.Check_Connection.Check_Servers", "File server connection successful.", "");
                        break;
                    }
                    else
                    {
                        Service.file_server_status = false;
                        Logging.Handler.Error("Initialization.Check_Connection.Check_Servers", "File server connection failed.", "");
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
            Logging.Handler.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Hostname_IP_Port", "Server: " + server + " Description: " + description);

            string server_regex = @"([a-zA-Z0-9\-\.]+):\d+";
            string server_port_regex = @"(?<=:)(\d+)";

            // Extract server name
            Match server_match = Regex.Match(server, server_regex);
            Match port_match = Regex.Match(server, server_port_regex);

            // Output server name
            if (server_match.Success)
            {
                Logging.Handler.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Server", server_match.Groups[0].Value);
            }
            else
            {
                Logging.Handler.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Server", "Server could not be extracted.");
            }

            // Output port
            if (port_match.Success)
            {
                Logging.Handler.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Port", "" + port_match.Groups[0].Value);
            }
            else
            {
                Logging.Handler.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Port", "Port could not be extracted.");
            }

            // Check main communication server
            try
            {
                using (var client = new TcpClient())
                {
                    // Check if main communication server is set
                    if (server_match.Success && port_match.Success)
                    {
                        Logging.Handler.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Hostname_IP_Port", "Server: " + server_match.Groups[1].Value + " Port: " + port_match.Groups[1].Value);

                        // Connect to the main communication server
                        await client.ConnectAsync(server_match.Groups[1].Value, Convert.ToInt32(port_match.Groups[0].Value));

                        Logging.Handler.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Hostname_IP_Port", "Connection to communication server successful");

                        return true;
                    }
                    else
                    {
                        Logging.Handler.Error("Initialization.Check_Connection.Hostname_IP_Port", "Hostname_IP_Port", "Server or port empty.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Initialization.Check_Connection.Hostname_IP_Port", "Hostname_IP_Port", ex.ToString());
            }

            return false;
        }
    }
}
