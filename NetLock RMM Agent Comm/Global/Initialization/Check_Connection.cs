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
using System.Runtime.InteropServices;
using NetLock_RMM_Agent_Comm.Windows;
using Global.Helper;
using System.Runtime.CompilerServices;

namespace Global.Initialization
{
    internal class Check_Connection
    {
        public static async Task Check_Servers()
        {
            try
            {
                // Check connections communication server. Split communication_servers with "," and check each server
                List<string> values = new List<string>(Configuration.Agent.communication_servers.Split(','));

                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "communication_servers"))
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            Windows_Worker.communication_server = value;
                            Windows_Worker.communication_server_status = true;
                        }

                        Logging.Debug("Initialization.Check_Connection.Check_Servers", "Communication server connection successful.", "");
                        break;
                    }
                    else
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            Windows_Worker.communication_server_status = false;
                        }
                        
                        Logging.Error("Initialization.Check_Connection.Check_Servers", "Communication server connection failed.", "");
                    }
                }

                // Check connections to remote server. Split remote_servers with "," and check each server
                values = new List<string>(Configuration.Agent.remote_servers.Split(','));

                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "remote_servers"))
                    { 
                        if (OperatingSystem.IsWindows())
                        {
                            Windows_Worker.remote_server = value;
                            Windows_Worker.remote_server_status = true;
                        }

                        Logging.Debug("Initialization.Check_Connection.Check_Servers", "Remote server connection successful.", "");
                        break;
                    }
                    else
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            Windows_Worker.remote_server_status = false;
                        }

                        Logging.Error("Initialization.Check_Connection.Check_Servers", "Remote server connection failed.", "");
                    }
                }

                // Check connections to update server. Split update_servers with "," and check each server
                values = new List<string>(Configuration.Agent.update_servers.Split(','));

                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "update_servers"))
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            Windows_Worker.update_server = value;
                            Windows_Worker.update_server_status = true;
                        }
                        
                        Logging.Debug("Initialization.Check_Connection.Check_Servers", "Update server connection successful.", "");
                        break;
                    }
                    else
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            Windows_Worker.update_server_status = false;
                        }
                        
                        Logging.Error("Initialization.Check_Connection.Check_Servers", "Update server connection failed.", "");
                    }
                }

                // Check connections to trust server. Split trust_servers with "," and check each server
                values = new List<string>(Configuration.Agent.trust_servers.Split(','));
                
                foreach (var value in values)
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "trust_servers"))
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            Windows_Worker.trust_server = value;
                            Windows_Worker.trust_server_status = true;
                        }

                        Logging.Debug("Initialization.Check_Connection.Check_Servers", "Trust server connection successful.", "");
                        break;
                    }
                    else
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            Windows_Worker.trust_server_status = false;
                        }

                        Logging.Error("Initialization.Check_Connection.Check_Servers", "Trust server connection failed.", "");
                    }
                }

                // Check connections to file server. Split file_servers with "," and check each server
                values = new List<string>(Configuration.Agent.file_servers.Split(','));

                foreach (var value in values) 
                {
                    // Remove whitespace
                    value.Trim();

                    if (await Hostname_IP_Port(value, "file_servers"))
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            Windows_Worker.file_server = value;
                            Windows_Worker.file_server_status = true;
                        }

                        Logging.Debug("Initialization.Check_Connection.Check_Servers", "File server connection successful.", "");
                        break;
                    }
                    else
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            Windows_Worker.file_server_status = false;
                        }

                        Logging.Error("Initialization.Check_Connection.Check_Servers", "File server connection failed.", "");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Initialization.Check_Connection.Check_Servers", "Failed", ex.ToString());
            }
        }

        public static async Task<bool> Hostname_IP_Port(string server, string description)
        {
            Logging.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Hostname_IP_Port", "Server: " + server + " Description: " + description);

            string server_regex = @"([a-zA-Z0-9\-\.]+):\d+";
            string server_port_regex = @"(?<=:)(\d+)";

            // Extract server name
            Match server_match = Regex.Match(server, server_regex);
            Match port_match = Regex.Match(server, server_port_regex);

            // Output server name
            if (server_match.Success)
            {
                Logging.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Server", server_match.Groups[0].Value);
            }
            else
            {
                Logging.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Server", "Server could not be extracted.");
            }

            // Output port
            if (port_match.Success)
            {
                Logging.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Port", "" + port_match.Groups[0].Value);
            }
            else
            {
                Logging.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Port", "Port could not be extracted.");
            }

            // Check main communication server
            try
            {
                using (var client = new TcpClient())
                {
                    // Check if main communication server is set
                    if (server_match.Success && port_match.Success)
                    {
                        Logging.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Hostname_IP_Port", "Server: " + server_match.Groups[1].Value + " Port: " + port_match.Groups[1].Value);

                        // Connect to the main communication server
                        await client.ConnectAsync(server_match.Groups[1].Value, Convert.ToInt32(port_match.Groups[0].Value));

                        Logging.Debug("Initialization.Check_Connection.Hostname_IP_Port", "Hostname_IP_Port", "Connection to communication server successful");

                        return true;
                    }
                    else
                    {
                        Logging.Error("Initialization.Check_Connection.Hostname_IP_Port", "Hostname_IP_Port", "Server or port empty.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Initialization.Check_Connection.Hostname_IP_Port", "Hostname_IP_Port", ex.ToString());
            }

            return false;
        }
    }
}
