using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

using static NetLock_RMM_Server.Agent.Windows.Authentification;

namespace NetLock_RMM_Server.SignalR
{
    public class CommandHub : Hub
    {
        // Verbindungswerte werden aus der appsettings.json geladen und defaulten zu sinnvollen Werten
        private static readonly int MAX_CONNECTION_ATTEMPTS = 
            GetAppSetting<int>("SignalR:MaxConnectionAttempts", 5);
        
        private static readonly int CONNECTION_ATTEMPT_DELAY_MS = 
            GetAppSetting<int>("SignalR:ConnectionAttemptDelayMs", 5000);
        
        public class Device_Identity
        {
            public string? agent_version { get; set; }
            public string? device_name { get; set; }
            public string? location_guid { get; set; }
            public string? tenant_guid { get; set; }
            public string? access_key { get; set; }
            public string? hwid { get; set; }
            public string? platform { get; set; }
            public string? ip_address_internal { get; set; }
            public string? operating_system { get; set; }
            public string? domain { get; set; }
            public string? antivirus_solution { get; set; }
            public string? firewall_status { get; set; }
            public string? architecture { get; set; }
            public string? last_boot { get; set; }
            public string? timezone { get; set; }
            public string? cpu { get; set; }
            public string? cpu_usage { get; set; }
            public string? mainboard { get; set; }
            public string? gpu { get; set; }
            public string? ram { get; set; }
            public string? ram_usage { get; set; }
            public string? tpm { get; set; }
            public string? environment_variables { get; set; }
            public string? last_active_user { get; set; }
        }

        public class Admin_Identity
        {
            public string token { get; set; }
            //public string api_key { get; set; }
        }

        public class Target_Device
        {
            public string device_id { get; set; }
            public string device_name { get; set; }
            public string location_guid { get; set; } 
            public string tenant_guid { get; set; }
        }

        public class Command
        {
            public int type { get; set; }
            public bool wait_response { get; set; }
            public string powershell_code { get; set; } 
            public int file_browser_command { get; set; } 
            public string file_browser_path { get; set; } 
            public string file_browser_path_move { get; set; } 
            public string file_browser_file_content { get; set; } 
            public string file_browser_file_guid { get; set; }
            public string remote_control_username { get; set; }
            public string remote_control_screen_index { get; set; }
            public string remote_control_mouse_action { get; set; }
            public string remote_control_mouse_xyz { get; set; }
            public string remote_control_keyboard_input { get; set; }
            public string remote_control_keyboard_content { get; set; }
            public string command { get; set; } // used for service, task manager, screen capture
        }
         
        public class Root_Entity
        {
            public Device_Identity? device_identity { get; set; }
            public Admin_Identity? admin_identity { get; set; }
            public Target_Device? target_device { get; set; }
            public Command? command { get; set; }
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                Logging.Handler.Debug("SignalR CommandHub", "OnConnectedAsync", "Client connected");

                var clientId = Context.ConnectionId;

                // Extract the device identity from the request headers
                var deviceIdentityEncoded = Context.GetHttpContext().Request.Headers["Device-Identity"];
                var adminIdentityEncoded = Context.GetHttpContext().Request.Headers["Admin-Identity"];

                if (string.IsNullOrEmpty(deviceIdentityEncoded) && string.IsNullOrEmpty(adminIdentityEncoded))
                {
                    Logging.Handler.Debug("SignalR CommandHub", "OnConnectedAsync", "Neither Device-Identity nor Admin-Identity was provided.");
                    Context.Abort();
                    await Task.CompletedTask;
                    return;
                }

                string decodedIdentityJson = string.Empty;

                if (!string.IsNullOrEmpty(deviceIdentityEncoded))
                {
                    decodedIdentityJson = Uri.UnescapeDataString(deviceIdentityEncoded);
                    Logging.Handler.Debug("SignalR CommandHub", "OnConnectedAsync", "Device identity: " + decodedIdentityJson);

                    var deviceIdentity = JsonSerializer.Deserialize<Device_Identity>(decodedIdentityJson);
                    if (deviceIdentity == null)
                    {
                        Logging.Handler.Error("SignalR CommandHub", "OnConnectedAsync", "Failed to deserialize device identity");
                        Context.Abort();
                        await Task.CompletedTask;
                        return;
                    }

                    // Verbesserte Verbindungslogik: Prüfe auf existierende Verbindungen
                    string deviceClientId = await Get_Device_ClientId(deviceIdentity.device_name, deviceIdentity.location_guid, deviceIdentity.tenant_guid);

                    // Wenn eine alte Verbindung existiert, entferne sie
                    if (!String.IsNullOrEmpty(deviceClientId))
                    {
                        Logging.Handler.Debug("SignalR CommandHub", "OnConnectedAsync", $"Device {deviceIdentity.device_name} already connected with ID {deviceClientId}. Replacing connection.");

                        // Protokolliere Verbindungswechsel mit mehr Informationen
                        Logging.Handler.Debug("SignalR CommandHub", "OnConnectedAsync", 
                            $"Connection replacement for device {deviceIdentity.device_name}: Old ID={deviceClientId}, New ID={clientId}");
                        
                        // Entferne alte Verbindung
                        CommandHubSingleton.Instance.RemoveClientConnection(deviceClientId);
                    }

                    // Verbindungszählerbegrenzung implementieren
                    int currentConnections = CommandHubSingleton.Instance._clientConnections.Count;
                    if (currentConnections > 1800) // Sicherheitsgrenze bei 1800 Verbindungen
                    {
                        Logging.Handler.Warning("SignalR CommandHub", "OnConnectedAsync", 
                            $"High connection count: {currentConnections}. Consider scaling your server.");
                    }
                }
                else if (!string.IsNullOrEmpty(adminIdentityEncoded))
                {
                    decodedIdentityJson = Uri.UnescapeDataString(adminIdentityEncoded);
                    Logging.Handler.Debug("SignalR CommandHub", "OnConnectedAsync", "Admin identity: " + decodedIdentityJson);
                }

                // Save clientId and any other relevant data in the Singleton's data structure
                CommandHubSingleton.Instance.AddClientConnection(clientId, decodedIdentityJson);

                // Check uptime monitoring
                await Uptime_Monitoring.Handler.Do(decodedIdentityJson, true);

                // Stabile Verbindungsmeldung an den Client senden
                await Clients.Client(clientId).SendAsync("ConnectionEstablished", new { status = "connected", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "OnConnectedAsync", ex.ToString());
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                Logging.Handler.Debug("SignalR CommandHub", "OnDisconnectedAsync", "Client disconnected");

                var clientId = Context.ConnectionId;

                // Get the identity JSON
                CommandHubSingleton.Instance._clientConnections.TryGetValue(clientId, out string identityJson);

                // Check uptime monitoring
                await Uptime_Monitoring.Handler.Do(identityJson, false);

                // Remove the client from the data structure when it logs out
                CommandHubSingleton.Instance.RemoveClientConnection(clientId);

                // Remove the client from the admin commands dictionary
                foreach (var adminCommand in CommandHubSingleton.Instance._adminCommands.ToList())
                {
                    if (adminCommand.Value == clientId)
                    {
                        CommandHubSingleton.Instance.RemoveAdminCommand(adminCommand.Key);
                    }
                }

                // Optimierte Logging - nur detaillierte Logs bei Bedarf
                if (Logging.Handler.IsDebugVerboseEnabled())
                {
                    foreach (var client in CommandHubSingleton.Instance._clientConnections)
                    {
                        Logging.Handler.Debug("SignalR CommandHub", "OnDisconnectedAsync", $"Connected clients: {client.Key}, {client.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "OnDisconnectedAsync", ex.ToString());
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task<string> Get_Device_ClientId(string device_name, string location_guid, string tenant_guid)
        {
            try
            {
                Logging.Handler.Debug("SignalR CommandHub", "Get_Device_ClientID", $"Device: {device_name}, Location: {location_guid}, Tenant: {tenant_guid}");

                // Optimierte Suche durch Einschränkung der Logausgabe 
                // Nur bei niedrigerem Log-Level alle Clients auflisten
                if (Logging.Handler.IsDebugVerboseEnabled())
                {
                    foreach (var client in CommandHubSingleton.Instance._clientConnections)
                    {
                        Logging.Handler.Debug("SignalR CommandHub", "Get_Device_ClientID", $"Connected client: {client.Key}, {client.Value}");
                    }
                }

                var clientId = CommandHubSingleton.Instance._clientConnections.FirstOrDefault(x =>
                {
                    try
                    {
                        var rootData = JsonSerializer.Deserialize<Root_Entity>(x.Value);
                        return rootData?.device_identity != null &&
                               rootData.device_identity.device_name == device_name &&
                               rootData.device_identity.location_guid == location_guid &&
                               rootData.device_identity.tenant_guid == tenant_guid;
                    }
                    catch (JsonException)
                    {
                        return false;
                    }
                }).Key;

                if (string.IsNullOrEmpty(clientId))
                {
                    Logging.Handler.Debug("SignalR CommandHub", "Get_Device_ClientID", "Client ID not found.");
                }

                return clientId;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "Get_Device_ClientID", ex.ToString());
                return null;
            }
        }

        public static async Task<string> Get_Admin_ClientId_By_ResponseId(string responseId)
        {
            try
            {
                // Optimierte Suche durch Einschränkung der Logausgabe 
                // Nur bei niedrigerem Log-Level alle Clients auflisten
                if (Logging.Handler.IsDebugVerboseEnabled())
                {
                    foreach (var client in CommandHubSingleton.Instance._adminCommands)
                    {
                        Logging.Handler.Debug("SignalR CommandHub", "Get_Admin_ClientId_By_ResponseId", $"Admin command: {client.Key}, {client.Value}");
                    }
                }

                if (CommandHubSingleton.Instance._adminCommands.TryGetValue(responseId, out string admin_identity_info_json))
                {
                    return admin_identity_info_json;
                }

                return null; // If the responseId is not found
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "Get_Admin_ClientId_By_ResponseId", ex.ToString());
                return null;
            }
        }

        public async Task SendMessageToClient(string client_id, string command_json)
        {
            try
            {
                Logging.Handler.Debug("SignalR CommandHub", "SendMessageToClient", $"Sending command to client {client_id}: {command_json}");

                // Send the command to the client mit Retry-Mechanismus
                await TrySendToClientWithRetry(client_id, "ReceiveMessage", command_json);

                Logging.Handler.Debug("SignalR CommandHub", "SendMessageToClient", $"Command sent to client {client_id}");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "SendMessageToClient", ex.ToString());
            }
        }

        public async Task SendMessageToClientAndWaitForResponse(string admin_identity_info_json, string client_id, string command_json)
        {
            try
            {
                Logging.Handler.Debug("SignalR CommandHub", "SendMessageToClientAndWaitForResponse", $"Sending command to client {client_id}");

                // Generate a unique responseId for the command
                var responseId = Guid.NewGuid().ToString();

                // Save responseId & admin_identity_info_json
                CommandHubSingleton.Instance.AddAdminCommand(responseId, admin_identity_info_json);

                // Add the responseId to the command JSON
                command_json = AddResponseIdToJson(command_json, responseId);

                // Send the command to the client with Retry-Logik bei Fehlern
                int attempts = 0;
                bool success = false;
                
                while (attempts < MAX_CONNECTION_ATTEMPTS && !success)
                {
                    try
                    {
                        attempts++;
                        await CommandHubSingleton.Instance.HubContext.Clients.Client(client_id).SendAsync("SendMessageToClientAndWaitForResponse", command_json);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Warning("SignalR CommandHub", "SendMessageToClientAndWaitForResponse", 
                            $"Attempt {attempts}/{MAX_CONNECTION_ATTEMPTS} failed: {ex.Message}");
                        
                        if (attempts < MAX_CONNECTION_ATTEMPTS)
                            await Task.Delay(CONNECTION_ATTEMPT_DELAY_MS);
                        else
                            throw; // Re-throw wenn alle Versuche fehlgeschlagen sind
                    }
                }

                Logging.Handler.Debug("SignalR CommandHub", "SendMessageToClientAndWaitForResponse", $"Command sent to client {client_id}");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "SendMessageToClientAndWaitForResponse", ex.ToString());
            }
        }

        // Receive response from client and send it back to the admin client
        public async Task ReceiveClientResponse(string responseId, string response)
        {
            try
            {
                Logging.Handler.Debug("SignalR CommandHub", "ReceiveClientResponse", $"Received response from client. ResponseId: {responseId}");

                if (String.IsNullOrEmpty(responseId) || String.IsNullOrEmpty(response))
                {
                    Logging.Handler.Debug("SignalR CommandHub", "ReceiveClientResponse", "ResponseId or response is empty.");
                    return;
                }

                // Get the admin client ID from the dictionary
                string admin_identity_info_json = await Get_Admin_ClientId_By_ResponseId(responseId);

                // Nur bei detailliertem Debug-Level alle Admin-Clients ausgeben
                if (Logging.Handler.IsDebugVerboseEnabled())
                {
                    foreach (var client in CommandHubSingleton.Instance._adminCommands)
                    {
                        Logging.Handler.Debug("SignalR CommandHub", "ReceiveClientResponse", $"Admin command: {client.Key}, {client.Value}");
                    }
                }

                if (string.IsNullOrEmpty(admin_identity_info_json))
                {
                    Logging.Handler.Debug("SignalR CommandHub", "ReceiveClientResponse", "Admin identity info not found.");
                    return;
                }

                string admin_client_id = String.Empty;
                string admin_token = String.Empty;
                string device_id = String.Empty;
                int type = 0;
                string command = String.Empty;
                int file_browser_command = 0;
                string powershell_code = String.Empty;

                // Deserialisierung des gesamten JSON-Strings
                using (JsonDocument document = JsonDocument.Parse(admin_identity_info_json))
                {
                    try
                    {
                        // Get the admin client ID from the JSON
                        JsonElement admin_client_id_element = document.RootElement.GetProperty("admin_client_id");
                        admin_client_id = admin_client_id_element.ToString();

                        // Get the admin username
                        JsonElement admin_token_element = document.RootElement.GetProperty("admin_token");
                        admin_token = admin_token_element.ToString();

                        // Get the device ID from the JSON
                        JsonElement device_id_element = document.RootElement.GetProperty("device_id");
                        device_id = device_id_element.ToString();

                        // Get the command type from the JSON
                        JsonElement type_element = document.RootElement.GetProperty("type");
                        type = type_element.GetInt32();

                        // Get the command from the JSON
                        JsonElement command_element = document.RootElement.GetProperty("command");
                        command = command_element.ToString();

                        // Get the powershell code
                        JsonElement powershell_code_element = document.RootElement.GetProperty("powershell_code");
                        powershell_code = powershell_code_element.ToString();

                        // Get the file browser command from the JSON
                        JsonElement file_browser_command_element = document.RootElement.GetProperty("file_browser_command");
                        file_browser_command = file_browser_command_element.GetInt32();
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("SignalR CommandHub", "ReceiveClientResponse", $"Error parsing admin info JSON: {ex.Message}");
                        return;
                    }
                }

                Logging.Handler.Debug("SignalR CommandHub", "ReceiveClientResponse", $"Admin client ID: {admin_client_id} type: {type}");

                // insert result into history table
                if (type == 0) // remote shell
                {
                    // Verbesserte Datenbankverbindung mit using-Statement für automatisches Schließen
                    using (MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String))
                    {
                        try
                        {
                            await conn.OpenAsync();

                            // Parameter definieren und mit AddWithValue hinzufügen
                            string execute_query = "INSERT INTO `device_information_remote_shell_history` (`device_id`, `date`, `author`, `command`, `result`) VALUES (@device_id, @date, @author, @command, @result);";

                            using (MySqlCommand cmd = new MySqlCommand(execute_query, conn))
                            {
                                cmd.Parameters.AddWithValue("@device_id", device_id);
                                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.Parameters.AddWithValue("@author", await MySQL.Handler.Get_Admin_Username_By_Remote_Session_Token(admin_token));
                                cmd.Parameters.AddWithValue("@command", powershell_code);
                                cmd.Parameters.AddWithValue("@result", response);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Error("SignalR CommandHub", "Database operation error", ex.ToString());
                        }
                        // Kein finally-Block notwendig, da using-Statement
                    }
                }

                // Check if the admin client ID is empty or null and return if it is
                if (string.IsNullOrEmpty(admin_client_id))
                {
                    Logging.Handler.Debug("SignalR CommandHub", "ReceiveClientResponse", "Admin client ID not found.");
                    return;
                }

                // Verbesserte Antwortlogik mit Retry-Mechanismus
                await TrySendToClientWithRetry(admin_client_id, GetResponseMethodName(type, file_browser_command, command), response);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "ReceiveClientResponse", ex.ToString());
            }
            finally
            {
                // Remove the responseId from the dictionary
                CommandHubSingleton.Instance.RemoveAdminCommand(responseId);
            }
        }

        // Helper-Methode zur Bestimmung des korrekten Methodennamens basierend auf Type und Command
        private string GetResponseMethodName(int type, int file_browser_command, string command = null)
        {
            if (type == 0) // remote shell
                return "ReceiveClientResponseRemoteShell";
            else if (type == 1) // file browser
            {
                switch(file_browser_command)
                {
                    case 0: return "ReceiveClientResponseRemoteFileBrowserDrives";
                    case 1: return "ReceiveClientResponseRemoteFileBrowserIndex";
                    case 2: return "ReceiveClientResponseRemoteFileBrowserCreateDirectory";
                    case 3: return "ReceiveClientResponseRemoteFileBrowserDeleteDirectory";
                    case 4: return "ReceiveClientResponseRemoteFileBrowserMoveDirectory";
                    case 5: return "ReceiveClientResponseRemoteFileBrowserRenameDirectory";
                    case 6: return "ReceiveClientResponseRemoteFileBrowserCreateFile";
                    case 7: return "ReceiveClientResponseRemoteFileBrowserDeleteFile";
                    case 8: return "ReceiveClientResponseRemoteFileBrowserMoveFile";
                    case 9: return "ReceiveClientResponseRemoteFileBrowserRenameFile";
                    case 10: return "ReceiveClientResponseRemoteFileBrowserUploadFile";
                    case 11: return "ReceiveClientResponseRemoteFileBrowserDownloadFile";
                    default: return "ReceiveClientResponse";
                }
            }
            else if (type == 2) // Service Action
                return "ReceiveClientResponseServiceAction";
            else if (type == 3) // Task Manager Action
                return "ReceiveClientResponseTaskManagerAction";
            else if (type == 4) // Remote Control
            {
                if (command == null) return "ReceiveClientResponse";
                
                switch(command)
                {
                    case "3": return "ReceiveClientResponseRemoteControlScreenIndexes";
                    case "4": return "ReceiveClientResponseRemoteControlUsers";
                    case "6": return "ReceiveClientResponseRemoteControlClipboard";
                    default: return "ReceiveClientResponse";
                }
            }
            
            return "ReceiveClientResponse"; // Fallback
        }

        // Method to receive commands from the webconsole
        public async Task MessageReceivedFromWebconsole(string message)
        {
            try
            {
                Logging.Handler.Debug("SignalR CommandHub", "MessageReceivedFromWebconsole", $"Received message from client: {message}");

                // Decode the received JSON
                string adminIdentityJson = Uri.UnescapeDataString(message);

                // Deserialize the JSON
                Root_Entity rootData = JsonSerializer.Deserialize<Root_Entity>(adminIdentityJson);
                
                if (rootData == null || rootData.admin_identity == null || 
                    rootData.target_device == null || rootData.command == null)
                {
                    Logging.Handler.Error("SignalR CommandHub", "MessageReceivedFromWebconsole", "Invalid message format");
                    return;
                }
                
                Admin_Identity admin_identity = rootData.admin_identity;
                Target_Device target_device = rootData.target_device;
                Command command = rootData.command;

                string commandJson = JsonSerializer.Serialize(command);

                // Get client id
                string client_id = await Get_Device_ClientId(target_device.device_name, target_device.location_guid, target_device.tenant_guid);

                // Do connection checks
                if (String.IsNullOrEmpty(client_id))
                {
                    string responseMessage = "Remote device is not connected with the NetLock RMM backend. Make sure your target device is connected.";
                    
                    if (command.type == 0) // if remote shell
                        await Clients.Caller.SendAsync("ReceiveClientResponseRemoteShell", responseMessage);
                    else if (command.type == 4) // if remote control
                        await Clients.Caller.SendAsync("ReceiveClientResponseRemoteControl", responseMessage);
                    else if (command.type == 5) // check connection
                        await Clients.Caller.SendAsync("ReceiveClientResponseCheckConnection", responseMessage);
                    else
                        await Clients.Caller.SendAsync("ReceiveClientResponse", responseMessage);
                    
                    return;
                }
                else if (command.type == 5) // check connection with positive response
                {
                    await Clients.Caller.SendAsync("ReceiveClientResponseCheckConnection", "Remote device is connected with the NetLock RMM backend.");
                    return; // No need to forward this check to the client
                }

                // Get admins client id
                var admin_client_id = Context.ConnectionId;

                //  Create the JSON object
                var jsonObject = new
                {
                    admin_client_id = admin_client_id, // admin client id
                    admin_token = admin_identity.token, // admin_token
                    device_id = target_device.device_id, // device_id
                    command = command.command, // command
                    powershell_code = command.powershell_code, // powershell_code
                    type = command.type, // represents the command type. Needed for the response to know how to handle the response
                    file_browser_command = command.file_browser_command, // represents the file browser command type. Needed for the response to know how to handle the response
                };

                // Convert the object into a JSON string
                string admin_identity_info_json = JsonSerializer.Serialize(jsonObject);

                // Send the command to the client and wait for the response
                if (command.wait_response)
                {
                    await SendMessageToClientAndWaitForResponse(admin_identity_info_json, client_id, commandJson);
                    Logging.Handler.Debug("SignalR CommandHub", "MessageReceivedFromWebconsole", $"Triggered command with waiting for response.");
                }
                else // Send the command to the client without waiting for the response
                {
                    await SendMessageToClient(client_id, commandJson);
                    Logging.Handler.Debug("SignalR CommandHub", "MessageReceivedFromWebconsole", $"Triggered command without waiting for response.");
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "MessageReceivedFromWebconsole", ex.ToString());
            }
        }

        private string AddResponseIdToJson(string json, string responseId)
        {
            try
            {
                // Parse the existing JSON string
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    // Create a dictionary to hold the properties
                    Dictionary<string, JsonElement> properties = new Dictionary<string, JsonElement>();

                    // Iterate over the properties of the existing JSON and add them to the new JSON object
                    foreach (var property in document.RootElement.EnumerateObject())
                    {
                        properties.Add(property.Name, property.Value.Clone());
                    }

                    // Add the responseId to the new JSON object
                    properties.Add("response_id", JsonDocument.Parse($"\"{responseId}\"").RootElement);

                    // Serialize the new JSON object back to string
                    return JsonSerializer.Serialize(properties);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "AddResponseIdToJson", $"Error adding responseId to JSON: {ex.ToString()}");
                throw; // Rethrow the exception to handle it appropriately in the calling method
            }
        }

        // Helper-Methode für robustere SignalR-Kommunikation
        private async Task<bool> TrySendToClientWithRetry(string clientId, string method, object arg)
        {
            int attempts = 0;
            bool success = false;
            
            while (attempts < MAX_CONNECTION_ATTEMPTS && !success)
            {
                try
                {
                    attempts++;
                    await CommandHubSingleton.Instance.HubContext.Clients.Client(clientId).SendAsync(method, arg);
                    success = true;
                }
                catch (Exception ex)
                {
                    Logging.Handler.Warning("SignalR CommandHub", "TrySendToClientWithRetry", 
                        $"Attempt {attempts}/{MAX_CONNECTION_ATTEMPTS} failed: {ex.Message}");
                    
                    if (attempts < MAX_CONNECTION_ATTEMPTS)
                        await Task.Delay(CONNECTION_ATTEMPT_DELAY_MS);
                }
            }
            
            return success;
        }
    }
}

