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
        // Connection values are loaded from the appsettings.json file and preset to sensible values.
        private static readonly int MAX_CONNECTION_ATTEMPTS = Configuration.SignalR.MaxConnectionAttempts;
        private static readonly int CONNECTION_ATTEMPT_DELAY_MS = Configuration.SignalR.ConnectionAttemptDelayMs;

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
            //public string device_id { get; set; }
            //public string device_name { get; set; }
            //public string location_guid { get; set; } 
            //public string tenant_guid { get; set; }
            public string access_key { get; set; }
        }

        public class Command
        {
            public int type { get; set; }
            public bool wait_response { get; set; }
            public string powershell_code { get; set; } 
            public int file_browser_command { get; set; } 
            public string file_browser_path { get; set; }
            public string file_browser_path_move { get; set; }
            public List<string> file_browser_paths { get; set; }
            public string file_browser_file_content { get; set; }
            public string file_browser_file_guid { get; set; }
            public string remote_control_username { get; set; }
            public string remote_control_screen_index { get; set; }
            public string remote_control_mouse_action { get; set; }
            public string remote_control_mouse_xyz { get; set; }
            public string remote_control_input_intent { get; set; }
            public string remote_control_keyboard_text { get; set; }
            public string remote_control_key_code { get; set; }
            public string remote_control_modifiers { get; set; }
            public string remote_control_keyboard_content { get; set; }
            public string remote_control_elevation_username { get; set; }
            public string remote_control_elevation_password { get; set; }
            public int remote_control_render_mode { get; set; }
            public string command { get; set; } // used for service, task manager, screen capture
            public string remote_shell_language { get; set; } // "", "powershell", "cmd", "bash", "zsh", "python3"
        }
         
        public class Root_Entity
        {
            public Device_Identity? device_identity { get; set; }
            public Admin_Identity? admin_identity { get; set; }
            public Target_Device? target_device { get; set; }
            public Command? command { get; set; }
        }

        // Maps a remote command type to the account permission key that the web console UI uses to
        // expose it. Types not present here have no matching permission and are rejected. Check
        // connection (type 5) is handled separately and intentionally not listed.
        private static readonly Dictionary<int, string> CommandTypePermissionMap = new Dictionary<int, string>
        {
            { 0, "devices_remote_shell" },              // Remote Shell
            { 1, "devices_remote_file_browser" },       // File Browser
            { 2, "devices_remote_control" },            // Service Action
            { 3, "devices_task_manager" },              // Task Manager Action
            { 4, "devices_remote_control" },            // Remote Screen Control
            { 6, "devices_remote_control" },            // Tray Icon - Show chat window
            { 7, "devices_remote_control" },            // Ask for remote screen control access
            { 8, "devices_remote_control" },            // Remote control - elevation request
            { 9, "devices_shutdown" },                  // Power actions (shutdown/reboot)
            { 10, "devices_remote_eventlog_viewer" },   // Event Log - Simple Commands
            { 11, "devices_remote_eventlog_viewer" },   // Event Log - Read with Parameters
            { 12, "devices_remote_eventlog_viewer" },   // Event Log - Stats
            { 13, "devices_remote_eventlog_viewer" },   // Event Log - Clear
            { 15, "devices_remote_control" },           // Virtual Display Management
            { 17, "devices_remote_shell" },             // Real-Time Shell
            { 18, "devices_remote_registry_editor" },   // Registry Editor
            { 19, "devices_remote_shell" },             // Remote Shell - Get Users
            { 20, "devices_wake_on_lan" },              // Wake On LAN
            { 21, "devices_software" },                 // Uninstall Application
            { 22, "devices_force_sync" },               // Force Sync
            { 23, "devices_snmp_tools" },               // SNMP Tools
            { 24, "devices_remote_control" },           // Read Service Details
            { 25, "devices_remote_control" },           // Apply Service Edit
            { 26, "devices_remote_control" },           // Refresh Services List
            { 27, "devices_remote_control" },           // Realtime Metrics
            { 28, "devices_software" },                 // Software Deployment - Trigger Pull
            { 29, "devices_uninstall_agent" },          // Uninstall Agent
            { 30, "sensors_dynamic_discovery" },        // Sensor Discovery (dynamic option picker)
            { 31, "devices_updates" },                  // Patch Now (on-demand patch installation)
            { 61, "devices_remote_control" },           // Tray Icon
            { 62, "devices_remote_control" },           // Tray Icon - Play sound
            { 63, "devices_remote_control" },           // Tray Icon
            { 64, "devices_remote_control" },           // Tray Icon
            { 65, "devices_remote_control" },           // Tray Icon - Show support overlay
        };

        // Maps a remote command type to the AgentSettings (policy) JSON property that gates it.
        // The device policy must have the mapped property set to true for the command to be forwarded.
        // Types not present here are not policy-gated server-side (always allowed at this stage).
        // Screen-control types (4,6,7,8,61,63,64,65) are intentionally NOT listed: they remain gated
        // on the agent. Type 27 (no policy field) and always-allowed types are also not listed.
        private static readonly Dictionary<int, string> CommandTypeFeatureMap = new Dictionary<int, string>
        {
            { 0, "RemoteShellEnabled" },                // Remote Shell
            { 17, "RemoteShellEnabled" },               // Real-Time Shell
            { 19, "RemoteShellEnabled" },               // Remote Shell - Get Users
            { 1, "RemoteFileBrowserEnabled" },          // File Browser
            { 2, "RemoteServiceManagerEnabled" },       // Service Action
            { 24, "RemoteServiceManagerEnabled" },      // Read Service Details
            { 25, "RemoteServiceManagerEnabled" },      // Apply Service Edit
            { 26, "RemoteServiceManagerEnabled" },      // Refresh Services List
            { 3, "RemoteTaskManagerEnabled" },          // Task Manager Action
            { 10, "RemoteEventLogEnabled" },            // Event Log - Simple Commands
            { 11, "RemoteEventLogEnabled" },            // Event Log - Read with Parameters
            { 12, "RemoteEventLogEnabled" },            // Event Log - Stats
            { 13, "RemoteEventLogEnabled" },            // Event Log - Clear
            { 18, "RemoteRegistryEditorEnabled" },      // Registry Editor
            { 23, "SnmpToolsEnabled" },                 // SNMP Tools
        };

        // Per-device agent_settings JSON cache keyed by access_key, used to gate command types
        // against the device policy without resolving the policy on every command. Entries expire
        // after AGENT_SETTINGS_CACHE_TTL_SECONDS.
        private static readonly ConcurrentDictionary<string, (string agentSettingsJson, DateTime expiresAt)> _agentSettingsCache = new ConcurrentDictionary<string, (string, DateTime)>();
        private const int AGENT_SETTINGS_CACHE_TTL_SECONDS = 30;

        private bool IsAdminConnection()
        {
            return Context.Items.TryGetValue("role", out var role) && (role as string) == "admin";
        }

        private bool IsDeviceConnection()
        {
            return Context.Items.TryGetValue("role", out var role) && (role as string) == "device";
        }

        // Confirms the operator's permissions grant the given command type. Unmapped types are rejected.
        private static bool Operator_Has_Permission_For_Type(MySQL.Handler.Operator_Info op, int type)
        {
            if (op == null)
                return false;

            if (!CommandTypePermissionMap.TryGetValue(type, out string permission_key) || string.IsNullOrEmpty(permission_key))
                return false;

            if (string.IsNullOrEmpty(op.permissions_json) || op.permissions_json == "[]")
                return false;

            try
            {
                using JsonDocument document = JsonDocument.Parse(op.permissions_json);

                if (document.RootElement.TryGetProperty(permission_key, out JsonElement element) &&
                    element.ValueKind == JsonValueKind.True)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "Operator_Has_Permission_For_Type", ex.ToString());
                return false;
            }
        }

        // Confirms the operator may act on the device's tenant. Full-access operators are always allowed.
        private static bool Operator_Has_Tenant_Access(MySQL.Handler.Operator_Info op, string tenant_id)
        {
            if (op == null)
                return false;

            if (op.full_access)
                return true;

            if (string.IsNullOrEmpty(tenant_id))
                return false;

            return op.tenant_ids.Contains(tenant_id);
        }

        // Resolves the device policy's agent_settings JSON for the given access_key, using a short-lived
        // cache. On a cache miss the policy name is resolved via the automations priority chain (external
        // IP intentionally skipped) and the matching policy's agent_settings column is read. The result
        // (including an empty string) is cached for AGENT_SETTINGS_CACHE_TTL_SECONDS.
        private static async Task<string> Get_Agent_Settings_For_Device(string access_key)
        {
            if (string.IsNullOrEmpty(access_key))
                return string.Empty;

            if (_agentSettingsCache.TryGetValue(access_key, out var cached) && cached.expiresAt > DateTime.UtcNow)
                return cached.agentSettingsJson;

            string agent_settings_json = string.Empty;

            try
            {
                using (var conn = new MySqlConnection(Configuration.MySQL.Connection_String))
                {
                    await conn.OpenAsync();

                    // Read the persisted internal IP and domain for this device (used by the resolver).
                    string internal_ip = string.Empty;
                    string domain = string.Empty;

                    using (var cmd = new MySqlCommand(
                        "SELECT ip_address_internal, domain FROM devices WHERE access_key = @access_key LIMIT 1;", conn))
                    {
                        cmd.Parameters.AddWithValue("@access_key", access_key);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                internal_ip = reader["ip_address_internal"] != DBNull.Value ? reader["ip_address_internal"].ToString() : string.Empty;
                                domain = reader["domain"] != DBNull.Value ? reader["domain"].ToString() : string.Empty;
                            }
                        }
                    }

                    // Resolve the policy name (external IP skipped) and read its agent_settings.
                    string policy_name = await Agent.Windows.Policy_Handler.Resolve_Policy_Name_For_Device(conn, access_key, internal_ip, domain, "");

                    if (!string.IsNullOrEmpty(policy_name))
                        agent_settings_json = await Agent.Windows.Policy_Handler.Get_Agent_Settings_By_Policy_Name(conn, policy_name);
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "Get_Agent_Settings_For_Device", ex.ToString());
            }

            _agentSettingsCache[access_key] = (agent_settings_json ?? string.Empty, DateTime.UtcNow.AddSeconds(AGENT_SETTINGS_CACHE_TTL_SECONDS));

            return agent_settings_json ?? string.Empty;
        }

        // Confirms the device policy allows the given command type. Types not in CommandTypeFeatureMap
        // are always allowed. If the policy / agent_settings cannot be resolved, the command is denied
        // (fail-closed). The mapped property must exist and be a JSON boolean true.
        private static async Task<bool> Device_Policy_Allows_Command_Type(string access_key, int type)
        {
            if (!CommandTypeFeatureMap.TryGetValue(type, out string feature_property) || string.IsNullOrEmpty(feature_property))
                return true;

            string agent_settings_json = await Get_Agent_Settings_For_Device(access_key);

            if (string.IsNullOrEmpty(agent_settings_json))
            {
                Logging.Handler.Warning("SignalR CommandHub", "Device_Policy_Allows_Command_Type",
                    $"No agent_settings resolved for access_key {access_key}. Command type {type} denied (fail-closed).");
                return false;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(agent_settings_json);

                if (document.RootElement.TryGetProperty(feature_property, out JsonElement element) &&
                    element.ValueKind == JsonValueKind.True)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Warning("SignalR CommandHub", "Device_Policy_Allows_Command_Type",
                    $"Failed to parse agent_settings for access_key {access_key}. Command type {type} denied (fail-closed). {ex}");
                return false;
            }
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

                    // Try to deserialize as Root_Entity first (with wrapper)
                    Device_Identity deviceIdentity = null;
                    try
                    {
                        var rootData = JsonSerializer.Deserialize<Root_Entity>(decodedIdentityJson);
                        if (rootData?.device_identity != null)
                        {
                            deviceIdentity = rootData.device_identity;
                        }
                        else
                        {
                            // Try direct deserialization if root entity failed
                            deviceIdentity = JsonSerializer.Deserialize<Device_Identity>(decodedIdentityJson);
                        }
                    }
                    catch (JsonException)
                    {
                        // Try direct deserialization if root entity parsing failed
                        deviceIdentity = JsonSerializer.Deserialize<Device_Identity>(decodedIdentityJson);
                    }
                    
                    if (deviceIdentity == null || string.IsNullOrEmpty(deviceIdentity.access_key))
                    {
                        Logging.Handler.Error("SignalR CommandHub", "OnConnectedAsync", 
                            $"Failed to deserialize device identity or access_key is empty. JSON: {decodedIdentityJson}");
                        Context.Abort();
                        await Task.CompletedTask;
                        return;
                    }
                    
                    // Verify that the device exists in the database and is authorized to connect
                    bool isDeviceAuthorized = await VerifyDeviceAccessKey(deviceIdentity.access_key);
                    if (!isDeviceAuthorized)
                    {
                        Logging.Handler.Warning("SignalR CommandHub", "OnConnectedAsync", 
                            $"Device with access_key {deviceIdentity.access_key} is not authorized. Connection rejected.");
                        Context.Abort();
                        await Task.CompletedTask;
                        return;
                    }

                    // Improved connection logic: Check for existing connections
                    // Device identifies itself only via access_key
                    string deviceClientId = await Get_Device_ClientId(deviceIdentity.access_key);

                    // If an old connection exists, remove it.
                    if (!String.IsNullOrEmpty(deviceClientId))
                    {
                        Logging.Handler.Debug("SignalR CommandHub", "OnConnectedAsync", $"Device with access_key already connected with ID {deviceClientId}. Replacing connection.");

                        // Log connection changes with more information
                        Logging.Handler.Debug("SignalR CommandHub", "OnConnectedAsync", 
                            $"Connection replacement: Old ID={deviceClientId}, New ID={clientId}");
                        
                        // Remove old connection
                        CommandHubSingleton.Instance.RemoveClientConnection(deviceClientId);
                    }

                    // Tag this connection as a device so hub methods can distinguish it from an admin
                    Context.Items["role"] = "device";
                }
                else if (!string.IsNullOrEmpty(adminIdentityEncoded))
                {
                    decodedIdentityJson = Uri.UnescapeDataString(adminIdentityEncoded);
                    Logging.Handler.Debug("SignalR CommandHub", "OnConnectedAsync", "Admin identity: " + decodedIdentityJson);

                    // Verify admin token
                    try
                    {
                        var adminRoot = JsonSerializer.Deserialize<Root_Entity>(decodedIdentityJson);
                        if (adminRoot?.admin_identity == null || string.IsNullOrEmpty(adminRoot.admin_identity.token))
                        {
                            Logging.Handler.Warning("SignalR CommandHub", "OnConnectedAsync", 
                                "Admin identity token is missing. Connection rejected.");
                            Context.Abort();
                            await Task.CompletedTask;
                            return;
                        }

                        bool isTokenValid = await Webconsole.Handler.Verify_Remote_Session_Token(adminRoot.admin_identity.token);
                        if (!isTokenValid)
                        {
                            Logging.Handler.Warning("SignalR CommandHub", "OnConnectedAsync",
                                "Admin token is invalid. Connection rejected.");
                            Context.Abort();
                            await Task.CompletedTask;
                            return;
                        }

                        // Tag this connection as an admin and remember the verified token so hub methods
                        // can scope it to the operator behind it.
                        Context.Items["role"] = "admin";
                        Context.Items["admin_token"] = adminRoot.admin_identity.token;
                    }
                    catch (JsonException ex)
                    {
                        Logging.Handler.Error("SignalR CommandHub", "OnConnectedAsync", 
                            $"Failed to parse admin identity JSON: {ex.Message}");
                        Context.Abort();
                        await Task.CompletedTask;
                        return;
                    }
                }

                // Save clientId and any other relevant data in the Singleton's data structure
                CommandHubSingleton.Instance.AddClientConnection(clientId, decodedIdentityJson);

                // Check uptime monitoring
                await Uptime_Monitoring.Handler.Do(decodedIdentityJson, true);

                // Send connection established message back to client
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
                Console.WriteLine($"[DISCONNECT] Client {clientId} disconnected. Identity JSON: {identityJson}");

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
                
                // Cleanup Relay-Sessions falls Device disconnected
                if (!string.IsNullOrEmpty(identityJson))
                {
                    Console.WriteLine($"[RELAY] Cleaning up relay sessions for disconnected client {clientId}");
                    try
                    {
                        Console.WriteLine($"[RELAY] Attempting to deserialize identity JSON...");
                        
                        // Parse als Root_Entity da das JSON ein Root-Objekt mit device_identity hat
                        var rootData = JsonSerializer.Deserialize<Root_Entity>(identityJson);
                        Console.WriteLine($"[RELAY] Deserialization successful: {rootData != null}");
                        
                        if (rootData?.device_identity != null)
                        {
                            var deviceIdentity = rootData.device_identity;
                            Console.WriteLine($"[RELAY] Device access_key: {deviceIdentity.access_key}");
                            
                            if (!string.IsNullOrEmpty(deviceIdentity.access_key))
                            {
                                var relayServer = Relay.RelayServer.Instance;
                                Console.WriteLine($"[RELAY] Getting active sessions...");
                                
                                var allSessions = relayServer.GetActiveSessions();
                                Console.WriteLine($"[RELAY] Total active sessions: {allSessions.Count}");
                                
                                var sessionsToCleanup = allSessions
                                    .Where(s => s.TargetDeviceId == deviceIdentity.access_key)
                                    .ToList();
                                
                                Console.WriteLine($"[RELAY] Sessions to cleanup for device {deviceIdentity.access_key}: {sessionsToCleanup.Count}");
                                
                                if (sessionsToCleanup.Count > 0)
                                {
                                    Console.WriteLine($"[RELAY] Device {deviceIdentity.access_key} disconnected. Cleaning up {sessionsToCleanup.Count} relay sessions.");
                                    
                                    foreach (var session in sessionsToCleanup)
                                    {
                                        Console.WriteLine($"[RELAY] Cleaning up session {session.SessionId}...");
                                        // Cleanup the target tunnel for this session
                                        relayServer.CleanupTargetTunnelForSession(session.SessionId);
                                        Console.WriteLine($"[RELAY] Cleaned up target tunnel for session {session.SessionId}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"[RELAY] No relay sessions found for device {deviceIdentity.access_key}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[RELAY] Device has no access_key, skipping relay cleanup");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[RELAY] Failed to deserialize device identity or device_identity is null");
                        }
                    }
                    catch (Exception relayEx)
                    {
                        Console.WriteLine($"[RELAY] Error cleaning up relay sessions: {relayEx.Message}");
                        Console.WriteLine($"[RELAY] Stack trace: {relayEx.StackTrace}");
                        Logging.Handler.Error("SignalR CommandHub", "OnDisconnectedAsync_RelayCleanup", relayEx.ToString());
                    }
                }
                else
                {
                    Console.WriteLine($"[RELAY] No identity JSON for client {clientId}, skipping relay cleanup");
                }

                // Only output all connected clients at detailed debug level
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

        /// <summary>
        /// Verifies that the device access_key exists and is authorized in the database
        /// </summary>  
        private async Task<bool> VerifyDeviceAccessKey(string accessKey)
        {
            try
            {
                using (var conn = new MySqlConnection(Configuration.MySQL.Connection_String))
                {
                    await conn.OpenAsync();
                    
                    using (var cmd = new MySqlCommand(
                        "SELECT authorized FROM devices WHERE access_key = @access_key LIMIT 1;", conn))
                    {
                        cmd.Parameters.AddWithValue("@access_key", accessKey);
                        
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                // Device exists, check if authorized
                                var authorized = reader["authorized"]?.ToString();
                                return authorized == "1";
                            }
                        }
                    }
                }
                
                // Device not found
                return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "VerifyDeviceAccessKey", ex.ToString());
                return false;
            }
        }

        // Get device client id by access key (used when admin sends command via webconsole)
        public async Task<string> Get_Device_ClientId(string access_key)
        {
            try
            {
                Logging.Handler.Debug("SignalR CommandHub", "Get_Device_ClientID", $"Access Key: {access_key}");

                // Optimized search by limiting log output 
                // Only list all clients if the log level is lower
                if (Logging.Handler.IsDebugVerboseEnabled())
                {
                    // List amount of connected clients
                    Logging.Handler.Debug("SignalR CommandHub", "Get_Device_ClientID", $"Total connected clients: {CommandHubSingleton.Instance._clientConnections.Count}");
                    
                    // List all connected clients for debugging
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
                               rootData.device_identity.access_key == access_key;
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
                // Optimized search by limiting log output 
                // Only list all clients if the log level is lower
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

        private async Task SendMessageToClient(string client_id, string command_json)
        {
            try
            {
                Logging.Handler.Debug("SignalR CommandHub", "SendMessageToClient", $"Sending command to client {client_id}: {command_json}");

                // Send the command to the client mit Retry-Mechanismus
                //await TrySendToClientWithRetry(client_id, "SendMessageToClient", command_json);
                await CommandHubSingleton.Instance.HubContext.Clients.Client(client_id).SendAsync("SendMessageToClient", command_json);
                
                Logging.Handler.Debug("SignalR CommandHub", "SendMessageToClient", $"Command sent to client {client_id}");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "SendMessageToClient", ex.ToString());
            }
        }

        private async Task SendMessageToClientAndWaitForResponse(string admin_identity_info_json, string client_id, string command_json)
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
        public async Task ReceiveClientResponse(string responseId, string response, bool persistent)
        {
            try
            {
                // This callback is only ever raised by a connected device.
                if (!IsDeviceConnection())
                    return;

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
                string remote_control_username = String.Empty;

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

                        // Get the remote control username (run as user) from the JSON
                        if (document.RootElement.TryGetProperty("remote_control_username", out JsonElement remote_control_username_element))
                            remote_control_username = remote_control_username_element.GetString() ?? "";
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("SignalR CommandHub", "ReceiveClientResponse", $"Error parsing admin info JSON: {ex.Message}");
                        return;
                    }
                }

                Logging.Handler.Debug("SignalR CommandHub", "ReceiveClientResponse", $"Admin client ID: {admin_client_id} type: {type}");

                // insert result into history table and add device_name for remote shell commands response
                if (type == 0) // remote shell
                {
                    // Verbesserte Datenbankverbindung mit using-Statement für automatisches Schließen
                    using (MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String))
                    {
                        try
                        {
                            await conn.OpenAsync();

                            // Parameter definieren und mit AddWithValue hinzufügen
                            string execute_query = "INSERT INTO `device_information_remote_shell_history` (`device_id`, `date`, `author`, `run_as_user`, `command`, `result`) VALUES (@device_id, @date, @author, @run_as_user, @command, @result);";

                            using (MySqlCommand cmd = new MySqlCommand(execute_query, conn))
                            {
                                cmd.Parameters.AddWithValue("@device_id", device_id);
                                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                cmd.Parameters.AddWithValue("@author", await MySQL.Handler.Get_Admin_Username_By_Remote_Session_Token(admin_token));
                                cmd.Parameters.AddWithValue("@run_as_user", remote_control_username);
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
                                
                    // Add device_id to the response for remote shell commands to identify the device in the webconsole response for bulk executin view
                    response = device_id + ">>nlocksep<<" + response;
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
                if (!persistent) 
                    CommandHubSingleton.Instance.RemoveAdminCommand(responseId);
            }
        }

        // Helper method to determine the correct method name based on Type and Command
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
                    case 12: return "ReceiveClientResponseRemoteFileBrowserReadFile";
                    case 13: return "ReceiveClientResponseRemoteFileBrowserViewImage";
                    case 14: return "ReceiveClientResponseRemoteFileBrowserViewPdf";
                    case 15: return "ReceiveClientResponseRemoteFileBrowserViewZip";
                    case 16: return "ReceiveClientResponseRemoteFileBrowserExtractZip";
                    case 17: return "ReceiveClientResponseRemoteFileBrowserZipItems";
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
                    case "8": return "ReceiveClientResponseRemoteControlElevation";
                    default: return "ReceiveClientResponse";
                }
            }
            else if (type == 6) // Tray Icon - Chat Message
                return "ReceiveClientResponseTrayIconChatMessage";
            else if (type == 7) // Remote Control Access Request
                return "ReceiveClientResponseRemoteControlAccessRequest";
            else if (type == 9) // Power Management Action
                return "ReceiveClientResponsePowerManagementAction";
            else if (type == 10 || type == 11 || type == 12 || type == 13) // Remote Eventlog Viewer - Get Eventlogs
                return "ReceiveClientResponseRemoteEventlogViewer";
            else if (type == 15) // Virtual Display Driver Management
                return "ReceiveClientResponseVirtualDisplay";
            else if (type == 17) // Real-Time Shell (output streams via dedicated ReceiveRealTimeShellOutput method)
                return "ReceiveRealTimeShellOutput";
            else if (type == 19) // Remote Shell - Get Users
                return "ReceiveClientResponseRemoteShellUsers";
            else if (type == 18) // Remote Registry Editor
                return "ReceiveClientResponseRemoteRegistryEditor";
            else if (type == 20) // Wake On LAN
                return "ReceiveClientResponseWakeOnLan";
            else if (type == 21) // Uninstall Application
                return "ReceiveClientResponseUninstallApplication";
            else if (type == 23) // SNMP Tools
                return "ReceiveClientResponseSNMPTools";
            else if (type == 24) // Read Service Details
                return "ReceiveClientResponseServiceReadDetails";
            else if (type == 25) // Apply Service Edit
                return "ReceiveClientResponseServiceEditApply";
            else if (type == 26) // Refresh Services List
                return "ReceiveClientResponseRefreshServicesList";
            else if (type == 27) // Realtime Metrics (CPU / RAM live poll)
                return "ReceiveClientResponseRealtimeMetrics";
            else if (type == 29) // Uninstall Agent
                return "ReceiveClientResponseUninstallAgent";
            else if (type == 30) // Sensor Discovery
                return "ReceiveClientResponseSensorDiscovery";

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

                Console.WriteLine($"[REALTIME-SHELL-DEBUG] MessageReceivedFromWebconsole: type={command.type}, wait_response={command.wait_response}, command_length={command.command?.Length}");

                // Only admin connections may dispatch commands to devices.
                if (!IsAdminConnection())
                {
                    Logging.Handler.Warning("SignalR CommandHub", "MessageReceivedFromWebconsole", "Caller is not an admin connection. Rejected.");
                    return;
                }

                // The token in the payload must be valid and match the one this connection authenticated with.
                if (admin_identity == null || string.IsNullOrEmpty(admin_identity.token) ||
                    !await Webconsole.Handler.Verify_Remote_Session_Token(admin_identity.token) ||
                    !(Context.Items.TryGetValue("admin_token", out var connToken) && (connToken as string) == admin_identity.token))
                {
                    Logging.Handler.Warning("SignalR CommandHub", "MessageReceivedFromWebconsole", "Admin token missing, invalid, or does not match the connection. Rejected.");
                    return;
                }

                // Resolve the operator so we can scope it to its tenants and permissions.
                MySQL.Handler.Operator_Info op = await MySQL.Handler.Resolve_Operator(admin_identity.token);
                if (op == null)
                {
                    Logging.Handler.Warning("SignalR CommandHub", "MessageReceivedFromWebconsole", "Operator could not be resolved. Rejected.");
                    return;
                }

                string commandJson = JsonSerializer.Serialize(command);

                // Get signalR client id of the target device
                string client_id = await Get_Device_ClientId(target_device.access_key);

                // Get device_id from database based on access_key
                string device_id = await MySQL.Handler.Get_Device_Id_By_Access_Key(target_device.access_key);

                // Scope the operator to the target device's tenant.
                string device_tenant_id = await MySQL.Handler.Get_TenantID_From_DeviceID(device_id);
                if (!Operator_Has_Tenant_Access(op, device_tenant_id))
                {
                    Logging.Handler.Warning("SignalR CommandHub", "MessageReceivedFromWebconsole", $"Operator {op.username} has no access to tenant {device_tenant_id}. Rejected.");
                    return;
                }

                // Check connection is allowed for any tenant-scoped operator and carries no feature permission.
                if (command.type != 5 && !Operator_Has_Permission_For_Type(op, command.type))
                {
                    Logging.Handler.Warning("SignalR CommandHub", "MessageReceivedFromWebconsole", $"Operator {op.username} lacks permission for command type {command.type}. Rejected.");
                    return;
                }

                // Enforce the device policy gating for non-screen-control feature command types.
                if (command.type != 5 && !await Device_Policy_Allows_Command_Type(target_device.access_key, command.type))
                {
                    Logging.Handler.Warning("SignalR CommandHub", "MessageReceivedFromWebconsole", $"Command type {command.type} is disabled by device policy for access_key {target_device.access_key}. Rejected.");
                    return;
                }

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
                    else if (command.type == 17) // real-time shell
                        await Clients.Caller.SendAsync("ReceiveRealTimeShellOutput", "", responseMessage);
                    else if (command.type == 18) // registry editor
                        await Clients.Caller.SendAsync("ReceiveClientResponseRemoteRegistryEditor", responseMessage);
                    else if (command.type == 19) // remote shell get users
                        await Clients.Caller.SendAsync("ReceiveClientResponseRemoteShellUsers", responseMessage);
                    else if (command.type == 24) // read service details
                        await Clients.Caller.SendAsync("ReceiveClientResponseServiceReadDetails", responseMessage);
                    else if (command.type == 25) // apply service edit
                        await Clients.Caller.SendAsync("ReceiveClientResponseServiceEditApply", responseMessage);
                    else if (command.type == 26) // refresh services list
                        await Clients.Caller.SendAsync("ReceiveClientResponseRefreshServicesList", responseMessage);
                    else if (command.type == 30) // sensor discovery
                        await Clients.Caller.SendAsync("ReceiveClientResponseSensorDiscovery", responseMessage);
                    else
                        await Clients.Caller.SendAsync("ReceiveClientResponse", responseMessage);
                    
                    return; // No need to forward this check to the client
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
                    device_id = device_id, // device_id from database
                    command = command.command, // command
                    powershell_code = command.powershell_code, // powershell_code
                    type = command.type, // represents the command type. Needed for the response to know how to handle the response
                    file_browser_command = command.file_browser_command, // represents the file browser command type. Needed for the response to know how to handle the response
                    remote_control_username = command.remote_control_username ?? "", // for run as user feature
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
        
        // Receive real-time shell output from agent and forward to admin client
        public async Task ReceiveRealTimeShellOutput(string responseId, string sessionId, string data)
        {
            try
            {
                // This callback is only ever raised by a connected device.
                if (!IsDeviceConnection())
                    return;

                Console.WriteLine($"[REALTIME-SHELL-DEBUG] ReceiveRealTimeShellOutput called: ResponseId={responseId}, SessionId={sessionId}, DataLength={data?.Length}");
                Logging.Handler.Debug("SignalR CommandHub", "ReceiveRealTimeShellOutput", $"ResponseId: {responseId}, SessionId: {sessionId}, DataLength: {data?.Length}");

                // Look up the admin client info from the responseId
                string admin_identity_info_json = await Get_Admin_ClientId_By_ResponseId(responseId);

                if (string.IsNullOrEmpty(admin_identity_info_json))
                {
                    Logging.Handler.Debug("SignalR CommandHub", "ReceiveRealTimeShellOutput", "Admin identity info not found for responseId: " + responseId);
                    return;
                }

                string admin_client_id = String.Empty;

                using (JsonDocument document = JsonDocument.Parse(admin_identity_info_json))
                {
                    JsonElement admin_client_id_element = document.RootElement.GetProperty("admin_client_id");
                    admin_client_id = admin_client_id_element.ToString();
                }

                if (string.IsNullOrEmpty(admin_client_id))
                {
                    Logging.Handler.Debug("SignalR CommandHub", "ReceiveRealTimeShellOutput", "Admin client ID not found.");
                    return;
                }

                // Forward the output to the admin client
                await CommandHubSingleton.Instance.HubContext.Clients.Client(admin_client_id).SendAsync("ReceiveRealTimeShellOutput", sessionId, data);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "ReceiveRealTimeShellOutput", ex.ToString());
            }
        }

        /// <summary>
        /// Convenience wrapper used by the Deploy_Software_Dialog ("Run Now"): resolves the
        /// client_id by access_key then forwards the command via SendMessageToClient. Keeps
        /// the web-console call site a single hub invocation without needing a prior hop.
        /// </summary>
        public async Task SendMessageToClientByAccessKey(string access_key, string command_json)
        {
            try
            {
                // Only admin connections may dispatch commands to devices.
                if (!IsAdminConnection())
                {
                    Logging.Handler.Warning("SignalR CommandHub", "SendMessageToClientByAccessKey", "Caller is not an admin connection. Rejected.");
                    return;
                }

                // Use the token this connection authenticated with to resolve the operator.
                if (!(Context.Items.TryGetValue("admin_token", out var connToken) && connToken is string admin_token) ||
                    string.IsNullOrEmpty(admin_token) || !await Webconsole.Handler.Verify_Remote_Session_Token(admin_token))
                {
                    Logging.Handler.Warning("SignalR CommandHub", "SendMessageToClientByAccessKey", "Connection has no valid admin token. Rejected.");
                    return;
                }

                MySQL.Handler.Operator_Info op = await MySQL.Handler.Resolve_Operator(admin_token);
                if (op == null)
                {
                    Logging.Handler.Warning("SignalR CommandHub", "SendMessageToClientByAccessKey", "Operator could not be resolved. Rejected.");
                    return;
                }

                // Determine the command type from the payload.
                int command_type;
                try
                {
                    using JsonDocument document = JsonDocument.Parse(command_json);
                    command_type = document.RootElement.GetProperty("type").GetInt32();
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("SignalR CommandHub", "SendMessageToClientByAccessKey", "Could not parse command type: " + ex.ToString());
                    return;
                }

                // Scope the operator to the target device's tenant.
                string device_id = await MySQL.Handler.Get_Device_Id_By_Access_Key(access_key);
                string device_tenant_id = await MySQL.Handler.Get_TenantID_From_DeviceID(device_id);
                if (!Operator_Has_Tenant_Access(op, device_tenant_id))
                {
                    Logging.Handler.Warning("SignalR CommandHub", "SendMessageToClientByAccessKey", $"Operator {op.username} has no access to tenant {device_tenant_id}. Rejected.");
                    return;
                }

                if (command_type != 5 && !Operator_Has_Permission_For_Type(op, command_type))
                {
                    Logging.Handler.Warning("SignalR CommandHub", "SendMessageToClientByAccessKey", $"Operator {op.username} lacks permission for command type {command_type}. Rejected.");
                    return;
                }

                string client_id = await Get_Device_ClientId(access_key);
                if (string.IsNullOrEmpty(client_id))
                {
                    Logging.Handler.Debug("SignalR CommandHub", "SendMessageToClientByAccessKey",
                        $"No connected client for access_key {access_key} — command dropped.");
                    return;
                }
                await CommandHubSingleton.Instance.HubContext.Clients.Client(client_id).SendAsync("SendMessageToClient", command_json);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "SendMessageToClientByAccessKey", ex.ToString());
            }
        }

        // ----- Software Deployment live-progress groups -----
        // The Deployment_Detail page joins "software_deployment_<id>" so the agent's
        // ReceiveDeploymentProgress events can be fanned out to interested admins only.
        public async Task Join_Software_Deployment_Group(int job_id)
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"software_deployment_{job_id}");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "Join_Software_Deployment_Group", ex.ToString());
            }
        }

        public async Task Leave_Software_Deployment_Group(int job_id)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"software_deployment_{job_id}");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR CommandHub", "Leave_Software_Deployment_Group", ex.ToString());
            }
        }

        // Helper method for more robust SignalR communication
        private async Task<bool> TrySendToClientWithRetry(string clientId, string method, string arg)
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
