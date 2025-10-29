using Global.Helper;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Sockets;
using System.Text.Json;
using System.Text;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net;
using System.Security.AccessControl;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Globalization;
using Windows.Helper.ScreenControl;
using System.Runtime.InteropServices;
using Global.Configuration;
using Global.Encryption;

namespace NetLock_RMM_Agent_Remote
{
    public class Remote_Worker : BackgroundService
    {
        // Server config
        public static string access_key = string.Empty;
        public static bool authorized = false;

        // Server communication
        public static string remote_server = string.Empty;
        public static string file_server = string.Empty;
        
        public static bool remote_server_status = false;
        public static bool file_server_status = false;
        
        // Local Server
        private const int Port = 7337;
        private const string ServerIp = "127.0.0.1"; // Localhost
        private TcpClient local_server_client;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Timer local_server_clientCheckTimer;

        // Remote Server Client
        public static string remote_server_url_command = String.Empty;
        private HubConnection remote_server_client;
        private Timer remote_server_clientCheckTimer;
        bool remote_server_client_setup = false;

        // User process monitoring
        private Timer user_process_monitoringCheckTimer;

        // Tray icon process monitoring
        //private Timer tray_icon_process_monitoringCheckTimer; currently disabled to prevent ghosting issues
        
        // Get server config timer
        private Timer serverConfigCheckTimer;
        
        // Device Identity
        public string device_identity_json = String.Empty;
        
        // Remote screen control
        private bool _agentSettingsRemoteServiceEnabled = false;
        private bool _agentSettingsRemoteShellEnabled = false;
        private bool _agentSettingsRemoteFileBrowserEnabled = false;
        private bool _agentSettingsRemoteTaskManagerEnabled = false;
        private bool _agentSettingsRemoteServiceManagerEnabled = false;
        private bool _agentSettingsRemoteScreenControlEnabled = false;
        private bool _agentSettingsRemoteScreenControlUnattendedAccess = false;
        private bool _remoteScreenControlAccessGranted = false;
        private List<string> _remoteScreenControlGrantedUsers = new List<string>();

        public class Device_Identity
        {
            public string agent_version { get; set; }
            public string package_guid { get; set; }
            public string device_name { get; set; }
            public string location_guid { get; set; }
            public string tenant_guid { get; set; }
            public string access_key { get; set; }
            public string hwid { get; set; }
            public string platform { get; set; }
            public string ip_address_internal { get; set; }
            public string operating_system { get; set; }
            public string domain { get; set; }
            public string antivirus_solution { get; set; }
            public string firewall_status { get; set; }
            public string architecture { get; set; }
            public string last_boot { get; set; }
            public string timezone { get; set; }
            public string cpu { get; set; }
            public string cpu_usage { get; set; }
            public string mainboard { get; set; }
            public string gpu { get; set; }
            public string ram { get; set; }
            public string ram_usage { get; set; }
            public string tpm { get; set; }
            public string environment_variables { get; set; }
            public string last_active_user { get; set; }
        }

        public class Command_Entity
        {
            public int type { get; set; }
            public bool wait_response { get; set; }
            public string powershell_code { get; set; }
            public string response_id { get; set; }
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
            public string command { get; set; } // used for service, task manager, screen capture. A command can either be a quick command like "list" or a json string with parameters, a number or json string
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool firstRun = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (firstRun)
                {
                    firstRun = false;
if (Agent.debug_mode)
    Logging.Debug("Service.ExecuteAsync", "Service is starting...", "Information");

                    
                    try
                    {
if (Agent.debug_mode)
    Logging.Debug("Service.OnStart", "Service started", "Information");

                        
                        await LoadServerConfig();

                        // Start the timer to check the local server connection status every 15 seconds
                        local_server_clientCheckTimer =
                            new Timer(async (e) => await Local_Server_Check_Connection_Status(), null, TimeSpan.Zero,
                                TimeSpan.FromSeconds(15));
                        
                        // Start the timer to check the remote server connection status every 15 seconds
                        remote_server_clientCheckTimer =
                            new Timer(async (e) => await Remote_Server_Check_Connection_Status(), null, TimeSpan.Zero,
                                TimeSpan.FromSeconds(15));
                        
                        // Start the timer to check the user process status every 1 minute
                        user_process_monitoringCheckTimer = new Timer(async (e) => await CheckUserProcessStatus(), null,
                            TimeSpan.Zero, TimeSpan.FromMinutes(1));
                        
                        // Start the timer to check the tray icon process status every 1 minute
                        //tray_icon_process_monitoringCheckTimer = new Timer(async (e) => await CheckTrayIconProcessStatus(), null,
                          //  TimeSpan.Zero, TimeSpan.FromMinutes(1));
                        
                        // Start the timer to reload the server config every 1 minute
                        serverConfigCheckTimer = new Timer(async (e) => await LoadServerConfig(),
                            null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
                        
                        // Starting the local server
                        _ = Task.Run(async () => await Local_Server_Start());

                        // Establishing a connection to the local server
                        _ = Task.Run(async () => await Local_Server_Connect()); // Läuft im Hintergrund
                        
                        // Check user process status as early as possible without blocking. This helps with device reboot scenarios
                        _ = Task.Run(async () => await CheckUserProcessStatus());
                    }
                    catch (Exception ex)
                    {
if (Agent.debug_mode)
    Logging.Error("Service.OnStart", "Error during service startup.", ex.ToString());

                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        #region Comm Agent Local Server 
        private async Task Local_Server_Connect()
        {
            try
            {
                local_server_client = new TcpClient();
                await local_server_client.ConnectAsync(ServerIp, Port);

                _stream = local_server_client.GetStream();
                _ = Local_Server_Handle_Server_Messages(_cancellationTokenSource.Token);
if (Agent.debug_mode)
    Logging.Debug("Service.Local_Server_Connect", "Connected to the local server.", "");


                // Previously used for initial device identity request. Removed that, but logic stays in place for future use cases.
            }
            catch (Exception ex)
            {
if (Agent.debug_mode)
    Logging.Error("Service.Local_Server_Connect", "Failed to connect to the local server.", ex.ToString());

            }
        }

        private async Task Local_Server_Handle_Server_Messages(CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[2096];

                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) // Server disconnected
                        break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
if (Agent.debug_mode)
    Logging.Debug("Service.Local_Server_Handle_Server_Messages", "Received message", message);


                    // Split the message per $
                    string[] messageParts = message.Split('$');

                    // device_identity
                    if (messageParts[0].ToString() == "device_identity")
                    {
                        Logging.Debug("Service.Local_Server_Handle_Server_Messages", "Device identity received",
                            messageParts[1]);
                        
                        // Preset logic in place for future use cases
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Local_Server_Handle_Server_Messages", "Failed to handle server messages.",
                    ex.ToString());
            }
        }

        private async Task Local_Server_Send_Message(string message)
        {
            try
            {
                if (_stream != null && local_server_client.Connected)
                {
if (Agent.debug_mode)
    Logging.Debug("Service.Local_Server_Send_Message", "Sent message", message);


                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    await _stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Local_Server_Send_Message", "Failed to send message to the local server.",
                    ex.ToString());
            }
        }

        private async Task Local_Server_Check_Connection_Status()
        {
            try
            {
                if (local_server_client == null)
                {
if (Agent.debug_mode)
    Logging.Error("Service.Check_Connection_Status", "local_server_client is null.", "");

                    return;
                }

                // Check if the local_server_client is connected and if, execute regular tasks
                if (local_server_client.Connected)
                {
if (Agent.debug_mode)
    Logging.Debug("Service.Check_Connection_Status", "Local server connection is active.", "");


                    // Previously used for initial device identity request. Removed that, but logic stays in place for future use cases.
                }
                else
                {
                    Logging.Debug("Service.Check_Connection_Status",
                        "Local server connection lost, attempting to reconnect.", "");
                    await Local_Server_Connect();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Check_Connection_Status",
                    "Failed to check remote_server_client or local_server_client status.", ex.ToString());
            }
        }
        
        #endregion

        #region SignalR Remote Server
        
        private readonly SemaphoreSlim _signalRConnectionLock = new SemaphoreSlim(1, 1);
        private bool _signalRConnecting = false;

        private async Task Remote_Server_Check_Connection_Status()
        {
            try
            {
                if (!_agentSettingsRemoteServiceEnabled)
                {
                    // Close the connection if open and return
                    if (remote_server_client != null &&
                        (remote_server_client.State == HubConnectionState.Connected ||
                         remote_server_client.State == HubConnectionState.Connecting ||
                         remote_server_client.State == HubConnectionState.Reconnecting))
                    {
                        Logging.Debug("Service.Check_Connection_Status",
                            "Remote service disabled in agent settings, closing connection.", "");

                        await remote_server_client.StopAsync();
                        await remote_server_client.DisposeAsync();
                        remote_server_client = null;
                        remote_server_client_setup = false;
                    }
                }
                
                if (!string.IsNullOrEmpty(device_identity_json))
                {
                    // Prevents multiple simultaneous connection attempts (in place to test remote screen control keyboard ghosting) https://github.com/0x101-Cyber-Security/NetLock-RMM/issues/89
                    await _signalRConnectionLock.WaitAsync();
                    try
                    {
                        if (_signalRConnecting)
                            return;

                        if (!remote_server_client_setup || remote_server_client == null ||
                            remote_server_client.State == HubConnectionState.Disconnected)
                        {
                            _signalRConnecting = true;
                            await Setup_SignalR();
                        }
                        else if (remote_server_client.State == HubConnectionState.Connected)
                        {
                            Logging.Debug("Service.Check_Connection_Status",
                                "Remote server connection is already active.", "");
                        }
                    }
                    finally
                    {
                        _signalRConnecting = false;
                        _signalRConnectionLock.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Check_Connection_Status", "Failed to check remote_server_client status.",
                    ex.ToString());
            }
        }

        public async Task Setup_SignalR()
        {
            try
            {
                // Check if the device_identity is empty, if so, return
                if (String.IsNullOrEmpty(device_identity_json))
                {
if (Agent.debug_mode)
    Logging.Error("Service.Setup_SignalR", "Device identity is empty.", "");

                    return;
                }
                else
                    Logging.Debug("Service.Setup_SignalR", "Device identity is not empty. Preparing remote connection.",
                        "");
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Device identity JSON", device_identity_json);


                // Deserialise device identity
                var jsonDocument = JsonDocument.Parse(device_identity_json);
                var deviceIdentityElement = jsonDocument.RootElement.GetProperty("device_identity");

                Device_Identity device_identity_object =
                    JsonSerializer.Deserialize<Device_Identity>(deviceIdentityElement.ToString());

                if (remote_server_client != null)
                {
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Disposing existing remote server client.", "");

                    await remote_server_client.StopAsync();
                    await remote_server_client.DisposeAsync();
                    remote_server_client = null;
                }

                remote_server_client = new HubConnectionBuilder()
                    .WithUrl(Global.Configuration.Agent.http_https + remote_server, options =>
                    {
                        options.Headers.Add("Device-Identity", Uri.EscapeDataString(device_identity_json));
                        options.UseStatefulReconnect = true;
                        options.WebSocketConfiguration = socket =>
                        {
                            socket.KeepAliveInterval = TimeSpan.FromSeconds(30);
                        };
                    }).ConfigureLogging(logging =>
                    {
                        if (OperatingSystem.IsWindows())
if (Agent.debug_mode)
    logging.AddEventLog();

if (Agent.debug_mode)

    logging.AddConsole();

if (Agent.debug_mode)

    logging.SetMinimumLevel(LogLevel.Warning);

                    })
                    .WithAutomaticReconnect()
                    .Build();

                // Handle ConnectionEstablished event from server
                remote_server_client.On<string>("ConnectionEstablished", (message) =>
                {
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "ConnectionEstablished with message", message);

                    // Connection established, no further action needed
                });

                // Handle ConnectionEstablished event from server - without parameter
                remote_server_client.On("ConnectionEstablished", () =>
                {
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "ConnectionEstablished without message", "");

                    // Connection established, no further action needed
                });
                
                remote_server_client.On<string>("SendMessageToClient", async (command) =>
                {
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "SendMessageToClient", command);


                    // Deserialisation of the entire JSON string
                    Command_Entity command_object = JsonSerializer.Deserialize<Command_Entity>(command);

                    try
                    {
                        // Insert the logic here to execute the command
                        if (command_object.type == 61 && _agentSettingsRemoteScreenControlEnabled) // Tray Icon - Hide chat window
                        {
                            if (Agent.debug_mode)
                                Logging.Debug("Service.Setup_SignalR", "Tray icon command", command_object.command);
                            
                            //  Create the JSON object
                            var jsonObject = new
                            {
                                response_id = command_object.response_id,
                                type = "hide_chat_window",
                            };

                            // Convert the object into a JSON string
                            string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });

                            if (Agent.debug_mode)

                                Logging.Debug("Service.Setup_SignalR", "Remote Control json", json);


                            // Send through local server to tray icon user process
                            await SendToClient(command_object.remote_control_username + "tray", json);
                        }
                        else if (command_object.type == 62) // Tray Icon - Play sound
                        {
                            if (Agent.debug_mode)
                                Logging.Debug("Service.Setup_SignalR", "Tray icon command", command_object.command);
                            
                            //  Create the JSON object
                            var jsonObject = new
                            {
                                response_id = command_object.response_id,
                                type = "play_sound",
                            };

                            // Convert the object into a JSON string
                            string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                            
                            if (Agent.debug_mode)
                                Logging.Debug("Service.Setup_SignalR", "Remote Control json", json);

                            // Send through local server to tray icon user process
                            await SendToClient(command_object.remote_control_username + "tray", json);
                        }
                        else if (command_object.type == 63 && _agentSettingsRemoteScreenControlEnabled) // Tray Icon - Exit chat window
                        {
                            if (Agent.debug_mode)
                                Logging.Debug("Service.Setup_SignalR", "Tray icon command", command_object.command);
                            
                            //  Create the JSON object
                            var jsonObject = new
                            {
                                response_id = command_object.response_id,
                                type = "exit_chat_window",
                            };

                            // Convert the object into a JSON string
                            string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                            
                            if (Agent.debug_mode)
                                Logging.Debug("Service.Setup_SignalR", "Remote Control json", json);


                            // Send through local server to tray icon user process
                            await SendToClient(command_object.remote_control_username + "tray", json);
                        }
                        else if (command_object.type == 64 && _agentSettingsRemoteScreenControlEnabled) // Tray Icon - Send message
                        {
                            if (Agent.debug_mode)
                                Logging.Debug("Service.Setup_SignalR", "Tray icon command", command_object.command);
 
                            
                            //  Create the JSON object
                            var jsonObject = new
                            {
                                response_id = command_object.response_id,
                                type = "new_chat_message",
                                command = command_object.command,
                            };

                            // Convert the object into a JSON string
                            string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                            
                            if (Agent.debug_mode)
                                Logging.Debug("Service.Setup_SignalR", "Remote Control json", json);


                            // Send through local server to tray icon user process
                            await SendToClient(command_object.remote_control_username + "tray", json);
                        }
                        else if (command_object.type == 8 && _agentSettingsRemoteScreenControlEnabled) // End remote screen control access
                        {

                            if (Agent.debug_mode)
                                Logging.Debug("Service.Setup_SignalR", "Tray icon command", command_object.command);
 
                            
                            //  Create the JSON object
                            var jsonObject = new
                            {
                                response_id = command_object.response_id,
                                type = "end_remote_session",
                                command = command_object.command,
                            };

                            // Convert the object into a JSON string
                            string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Remote Control json", json);


                            // Send through local server to tray icon user process
                            await SendToClient(command_object.remote_control_username + "tray", json);
                            
                            // Remove user from allowed remote screen control users
                            _remoteScreenControlGrantedUsers.Remove(command_object.remote_control_username);
                            
                            Logging.Debug("Service.Setup_SignalR", "Remote Control access ended for user",
                                command_object.remote_control_username);
                        }
                    }
                    catch (Exception ex)
                    {
if (Agent.debug_mode)
    Logging.Error("Service.Setup_SignalR", "Failed to deserialize command object.", ex.ToString());

                    }

                    // Example: If the command is "sync", send a message to the local server to force a sync with the remote server
                    if (command == "sync")
                        await Local_Server_Send_Message("sync");
                });

                // Receive a message from the remote server, process the command and send a response back to the remote server
                remote_server_client.On<string>("SendMessageToClientAndWaitForResponse", async (command) =>
                {
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "SendMessageToClientAndWaitForResponse", command);

                    
                    // Deserialisation of the entire JSON string
                    Command_Entity command_object = JsonSerializer.Deserialize<Command_Entity>(command);
                    // Example: If the type is 0, execute the powershell code and send the response back to the remote server if wait_response = true

                    string result = string.Empty;

                    try
                    {
                        if (command_object.type == 0 && _agentSettingsRemoteShellEnabled) // Remote Shell
                        {
                            if (OperatingSystem.IsWindows())
                                result = Windows.Helper.PowerShell.Execute_Script(command_object.type.ToString(),
                                    command_object.powershell_code);
                            else if (OperatingSystem.IsLinux())
                                result = Linux.Helper.Bash.Execute_Script("Remote Shell", true,
                                    command_object.powershell_code);
                            else if (OperatingSystem.IsMacOS())
                                result = MacOS.Helper.Zsh.Execute_Script("Remote Shell", true,
                                    command_object.powershell_code);
if (Agent.debug_mode)
    Logging.Debug("Client", "PowerShell executed", result);

                        }
                        else if (command_object.type == 1 && _agentSettingsRemoteFileBrowserEnabled) // File Browser
                        {
                            // if linux or macos convert the path to linux/macos path
                            if (!String.IsNullOrEmpty(command_object.file_browser_path) && OperatingSystem.IsLinux() ||
                                OperatingSystem.IsMacOS())
                                command_object.file_browser_path = command_object.file_browser_path.Replace("\\", "/");

                            // 0 = get drives, 1 = index, 2 = create dir, 3 = delete dir, 4 = move dir, 5 = rename dir, 6 = create file, 7 = delete file, 8 = move file, 9 = rename file, 10 = download file, 11 = upload file

                            // File Browser Command

                            if (command_object.file_browser_command == 0) // Get drives
                            {
                                result = IO.Get_Drives();
                            }

                            if (command_object.file_browser_command == 1) // index
                            {
                                // Get all directories and files in the specified path, create a json including date, size and file type
                                var directoryDetails = await IO.Get_Directory_Index(command_object.file_browser_path);
                                result = JsonSerializer.Serialize(directoryDetails,
                                    new JsonSerializerOptions { WriteIndented = true });
                            }
                            else if (command_object.file_browser_command == 2) // create dir
                            {
                                result = IO.Create_Directory(command_object.file_browser_path);
                            }
                            else if (command_object.file_browser_command == 3) // delete dir
                            {
                                result = IO.Delete_Directory(command_object.file_browser_path).ToString();
                            }
                            else if (command_object.file_browser_command == 4) // move dir
                            {
                                result = IO.Move_Directory(command_object.file_browser_path,
                                    command_object.file_browser_path_move);
                            }
                            else if (command_object.file_browser_command == 5) // rename dir
                            {
                                result = IO.Rename_Directory(command_object.file_browser_path,
                                    command_object.file_browser_path_move);
                            }
                            else if (command_object.file_browser_command == 6) // create file
                            {
                                result = await IO.Create_File(command_object.file_browser_path,
                                    command_object.file_browser_file_content);
                            }
                            else if (command_object.file_browser_command == 7) // delete file
                            {
                                result = IO.Delete_File(command_object.file_browser_path).ToString();
                            }
                            else if (command_object.file_browser_command == 8) // move file
                            {
                                result = IO.Move_File(command_object.file_browser_path,
                                    command_object.file_browser_path_move);
                            }
                            else if (command_object.file_browser_command == 9) // rename file
                            {
                                result = IO.Rename_File(command_object.file_browser_path,
                                    command_object.file_browser_path_move);
                            }
                            else if (command_object.file_browser_command == 10) // download file from file server
                            {
                                // download url with tenant guid, location guid & device name
                                string download_url = file_server + "/admin/files/download/device" + "?guid=" +
                                                      command_object.file_browser_file_guid + "&tenant_guid=" +
                                                      device_identity_object.tenant_guid + "&location_guid=" +
                                                      device_identity_object.location_guid + "&device_name=" +
                                                      device_identity_object.device_name + "&access_key=" +
                                                      device_identity_object.access_key + "&hwid=" +
                                                      device_identity_object.hwid;
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Download URL", download_url);


                                result = await Http.DownloadFileAsync(Global.Configuration.Agent.ssl, download_url,
                                    command_object.file_browser_path, device_identity_object.package_guid);
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "File downloaded", result);

                            }
                            else if (command_object.file_browser_command == 11) // upload file
                            {
                                string file_name = Path.GetFileName(command_object.file_browser_path);

                                // upload url with tenant guid, location guid & device name
                                string upload_url = file_server + "/admin/files/upload/device" + "?tenant_guid=" +
                                                    device_identity_object.tenant_guid + "&location_guid=" +
                                                    device_identity_object.location_guid + "&device_name=" +
                                                    device_identity_object.device_name + "&access_key=" +
                                                    device_identity_object.access_key + "&hwid=" +
                                                    device_identity_object.hwid;
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Upload URL", upload_url);


                                // Upload the file to the server
                                result = await Http.UploadFileAsync(Global.Configuration.Agent.ssl, upload_url, command_object.file_browser_path,
                                    device_identity_object.package_guid);
                            }
                        }
                        else if (command_object.type == 2 && _agentSettingsRemoteServiceManagerEnabled) // Service
                        {
                            // Deserialise the command_object.command json, using json document (action, name)
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Service command", command_object.command);


                            string action = String.Empty;
                            string name = String.Empty;

                            using (JsonDocument doc = JsonDocument.Parse(command_object.command))
                            {
                                JsonElement root = doc.RootElement;

                                // Access to the "action" field
                                action = root.GetProperty("action").GetString();

                                // Access to the "name" field
                                name = root.GetProperty("name").GetString();
                            }

                            // Execute
                            result = await Service.Action(action, name);
                            
                            if (Agent.debug_mode)
                                Logging.Debug("Service.Setup_SignalR", "Service Action", result);
                        }
                        else if (command_object.type == 3 && _agentSettingsRemoteTaskManagerEnabled) // Task Manager Action
                        {
                            // Terminate process by pid
                            result = await Task_Manager.Terminate_Process_Tree(Convert.ToInt32(command_object.command));
                            
                            if (Agent.debug_mode)
                                Logging.Debug("Service.Setup_SignalR", "Terminate Process", result);

                        }
                        else if (command_object.type == 4 && _agentSettingsRemoteScreenControlEnabled) // Remote Screen Control
                        {
                            // Check if the command requests connected users
                            if (command_object.command == "4")
                            {
                                // Get connected users from _clients, excluding those with "tray" suffix
                                List<string> connected_users = _clients.Keys
                                    .Where(u => !u.EndsWith("tray", StringComparison.OrdinalIgnoreCase))
                                    .ToList();

                                // Move the device name user (if existing) to the first position
                                if (connected_users.Contains(device_identity_object.device_name))
                                {
                                    connected_users.Remove(device_identity_object.device_name);
                                    connected_users.Insert(0, device_identity_object.device_name);
                                }
                                
                                // Convert the list to a comma separated string 
                                result = string.Join(",", connected_users);
                            }
                            else // Forward the command to the users process
                            {
                                if (!_remoteScreenControlAccessGranted || !_agentSettingsRemoteScreenControlEnabled)
                                {
                                    result = "Remote screen control access denied.";
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Remote Control access denied", result);

                                    
                                    // Return because no further action is required
                                    return;
                                }
                                
                                // Check if command_object.remote_control_username is allowed to use remote screen control (_remoteScreenControlGrantedUsers)
                                if (!_remoteScreenControlGrantedUsers.Contains(command_object.remote_control_username))
                                {
                                    result = "Remote screen control access denied for user: " +  command_object.remote_control_username;
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Remote Control access denied", result);

                                    
                                    // Return because no further action is required
                                    return;
                                }
                                
                                try
                                {
                                    Logging.Debug("Service.Setup_SignalR", "Remote Control command",
                                        command_object.command);

                                    // Check if ctrlaltdel and send through SAS instead
                                    if (command_object.remote_control_keyboard_input == "ctrlaltdel")
                                    {
                                        Logging.Debug("Service.Setup_SignalR", "Sending SAS for ctrlaltdel",
                                            command_object.remote_control_username);

                                        Sas_Diagnostics.LogContext();

                                        User32.SendSAS(false); // Send the SAS twice to ensure it is processed correctly
                                        //User32.SendSAS(true); // Send the SAS twice to ensure it is processed correctly
                                    }

                                    //  Create the JSON object
                                    var jsonObject = new
                                    {
                                        response_id = command_object.response_id,
                                        type = command_object.command,
                                        remote_control_screen_index = command_object.remote_control_screen_index,
                                        remote_control_mouse_action = command_object.remote_control_mouse_action,
                                        remote_control_mouse_xyz = command_object.remote_control_mouse_xyz,
                                        remote_control_keyboard_input = command_object.remote_control_keyboard_input,
                                        remote_control_keyboard_content = command_object.remote_control_keyboard_content,
                                    };

                                    // Convert the object into a JSON string
                                    string json = JsonSerializer.Serialize(jsonObject,
                                        new JsonSerializerOptions { WriteIndented = true });
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Remote Control json", json);


                                    // Send through local SignalR Hub to User
                                    await SendToClient(command_object.remote_control_username, json);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Error("Service.Setup_SignalR", "Failed to execute remote control command.",
                                        ex.ToString());
                                }

                                // Return because no further action is required
                                return;
                            }
                        }
                        else if (command_object.type == 6 && _agentSettingsRemoteScreenControlEnabled) // Tray Icon - Show chat window
                        {
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Tray icon command", command_object.command);
 
                            
                            //  Create the JSON object
                            var jsonObject = new
                            {
                                response_id = command_object.response_id,
                                type = "show_chat_window",
                                command = command_object.command,
                            };

                            // Convert the object into a JSON string
                            string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Remote Control json", json);


                            // Send through local server to tray icon user process
                            await SendToClient(command_object.remote_control_username + "tray", json);
                            
                            // Return because no further action is required
                            return;
                        }
                        else if (command_object.type == 7) // Ask for remote screen control access
                        {
                            if (!_agentSettingsRemoteScreenControlEnabled)
                            {
                                _remoteScreenControlAccessGranted = false;
                                _remoteScreenControlGrantedUsers.Clear();
                                result = "Remote screen control access denied by policy settings.";
                            }
                            else
                            {
                                _remoteScreenControlAccessGranted = true;

                                if (_agentSettingsRemoteScreenControlUnattendedAccess)
                                {
                                    _remoteScreenControlGrantedUsers.Add(command_object.remote_control_username);
                                    result = "accepted";
                                }
                                else
                                {

                                    var jsonObject = new
                                    {
                                        response_id = command_object.response_id,
                                        type = "access_request",
                                        command = command_object.command
                                    };

                                    string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
if (Agent.debug_mode)
    Logging.Debug("Service.Setup_SignalR", "Remote Control access request json", json);


                                    await SendToClient(command_object.remote_control_username + "tray", json);

                                    result = "Remote screen control access request sent to the user.";
                                    
                                    // Return here to prevent losing the response_id context
                                    return;
                                }
                            }
                        }
                        else if (command_object.type == 9) // Power actions
                        {
                            if (command_object.command == "shutdown")
                                result = "shutdowndone";
                            else if (command_object.command == "reboot")
                                result = "rebootdone";
                            
                            // Send immediate response back to server
                            await remote_server_client.InvokeAsync("ReceiveClientResponse", command_object.response_id,
                                result, false);
                            
                            if (command_object.command == "shutdown")
                            {
                                if (OperatingSystem.IsWindows())
                                    result = Windows.Helper.PowerShell.Execute_Command("shutdown", "Stop-Computer -ComputerName localhost -Force", 60000);
                                else if (OperatingSystem.IsLinux())
                                    result = Linux.Helper.Bash.Execute_Script("shutdown", false, "sudo shutdown now");
                                else if (OperatingSystem.IsMacOS())
                                    result = MacOS.Helper.Zsh.Execute_Script("shutdown", false, "sudo shutdown now");
                            }
                            else if (command_object.command == "reboot")
                            {
                                if (OperatingSystem.IsWindows())
                                    result = Windows.Helper.PowerShell.Execute_Command("restart",
                                        "Restart-Computer -ComputerName localhost -Force", 60000);
                                else if (OperatingSystem.IsLinux())
                                    result = Linux.Helper.Bash.Execute_Script("restart", false, "sudo reboot");
                                else if (OperatingSystem.IsMacOS())
                                    result = MacOS.Helper.Zsh.Execute_Script("restart", false, "sudo reboot");
                            }
                            
                            return; // Return here to prevent sending a second response
                        }
                    }
                    catch (Exception ex)
                    {
                        result = ex.Message;
                        Logging.Error("Service.Setup_SignalR", "Failed to execute file browser command.",
                            ex.ToString());
                    }

                    // Send the response back to the server (if we execute a single operation that doesnt require a response, we need to return the operation in that method to prevent this from executing)
                    if (!String.IsNullOrEmpty(command_object.type.ToString()))
                    {
                        if (String.IsNullOrEmpty(result))
                            result = "Command executed. No result returned.";

                        Logging.Debug("Client", "Sending response back to the server",
                            "result: " + result + "response_id: " + command_object.response_id);
                        await remote_server_client.InvokeAsync("ReceiveClientResponse", command_object.response_id,
                            result, false);
                        Logging.Debug("Client", "Response sent back to the server",
                            "result: " + result + "response_id: " + command_object.response_id);
                    }

                    await Task.CompletedTask;
                });

                // Start the connection
                await remote_server_client.StartAsync();

                remote_server_client_setup = true;
                
                if (Agent.debug_mode)
                    Logging.Debug("Service.Setup_SignalR", "Connected to the remote server.", "");
            }
            catch (Exception ex)
            {
                if (Agent.debug_mode)
                    Logging.Error("Service.Setup_SignalR", "Failed to start SignalR.", ex.ToString());
            }
        }
        
        #endregion

        #region User agent process monitoring

        private async Task CheckUserProcessStatus()
        {
            if (!OperatingSystem.IsWindows()) return;

            try
            {
                bool processIsRunning = false;

                var allProcesses = Process.GetProcessesByName("NetLock_RMM_User_Process");

                // Check if the user process is running
                foreach (var process in allProcesses)
                {
                    if (process != null && !process.HasExited)
                    {
                        processIsRunning = true;
                        
                        if (Agent.debug_mode)
                            Logging.Debug("Service.CheckUserProcess", "User process is running.", $"PID: {process.Id}");

                        break; // User process is running, no action needed
                    }
                }

                if (!processIsRunning)
                {
                    bool success = Windows.Helper.ScreenControl.Win32Interop.CreateInteractiveSystemProcess(
                        commandLine: Application_Paths.netlock_rmm_user_agent_path,
                        targetSessionId: 0,
                        hiddenWindow: false,
                        out var procInfo
                    );
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.CheckUserProcess", "Exception while checking or starting user processes.",
                    ex.ToString());
            }
        }

        #endregion
        
        #region Tray Icon Process Monitoring (Windows, Linux & MacOS)
        
        private async Task CheckTrayIconProcessStatus()
        {
            try
            {
                bool processIsRunning = false;

                // Check if the tray icon process is running (same for all platforms)
                var allProcesses = Process.GetProcessesByName("NetLock_RMM_Tray_Icon");
                
                foreach (var process in allProcesses)
                {
                    if (process != null && !process.HasExited)
                    {
                        processIsRunning = true;
                        Logging.Debug("Service.CheckTrayIconProcess", "Tray icon process is running.",
                            $"PID: {process.Id}");
                        break; // Tray icon process is running, no action needed
                    }
                }

                // Start process if not running (platform-specific)
                if (!processIsRunning)
                {
                    if (OperatingSystem.IsWindows())
                    {
                        bool success = Windows.Helper.ScreenControl.Win32Interop.CreateInteractiveSystemProcess(
                            commandLine: Application_Paths.program_files_tray_icon_path,
                            targetSessionId: 0,
                            hiddenWindow: false,
                            out var procInfo
                        );
                    }
                    else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = Application_Paths.program_files_tray_icon_path,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        Process.Start(startInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.CheckTrayIconProcess", "Exception while checking or starting tray icon process.",
                    ex.ToString());
            }
        }
        
        #endregion

        #region Remote Agent Local Server

        private const int Remote_Agent_Local_Port = 7338;

        private ConcurrentDictionary<string, TcpClient>
            _clients = new ConcurrentDictionary<string, TcpClient>(); // Clients by username

        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSourceLocal = new CancellationTokenSource();
        private SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(100); // Limit to 100 concurrent clients
        private const int BufferSize = 10 * 1024 * 1024; // 10 MB Buffer

        public async Task Local_Server_Start()
        {
            try
            {
if (Agent.debug_mode)
    Logging.Debug("Service.Remote_Agent_Local_Server_Start", "Starting server...", "");

                _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Remote_Agent_Local_Port);
                _listener.Start();
                Logging.Debug("Service.Remote_Agent_Local_Server_Start", "Server started. Waiting for connections...",
                    "");

                while (!_cancellationTokenSourceLocal.Token.IsCancellationRequested)
                {
                    await _connectionSemaphore.WaitAsync(); // Throttle connections
                    var client = await _listener.AcceptTcpClientAsync();
                    if (client != null)
                    {
                        _ = Local_Server_Handle_Client(client,
                            _cancellationTokenSourceLocal.Token); // Handle client asynchronously
                    }
                }
            }
            catch (Exception ex)
            {
if (Agent.debug_mode)
    Logging.Error("Service.Remote_Agent_Local_Server_Start", "Error starting server.", ex.ToString());

            }
        }

        private async Task Local_Server_Handle_Client(TcpClient client, CancellationToken cancellationToken)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[BufferSize];

            try
            {
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive,
                    true); // Enable KeepAlive

                // Wait for the client to send the username first
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                string initialMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string[] messageParts = initialMessage.Split('$');

                if (messageParts[0] == "username")
                {
                    string username = messageParts[1];
if (Agent.debug_mode)
    Logging.Debug("Service.Remote_Agent_Local_Server_Handle_Client", "Client connected", username);


                    // Add the client to the dictionary
                    _clients[username] = client;

                    // Handle messages from the client
                    await HandleClientMessages(username, client, stream, cancellationToken);
                }
            }
            catch (IOException ex)
            {
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Remote_Agent_Local_Server_Handle_Client", "Error handling client.",
                    ex.ToString());
            }
            finally
            {
                stream.Close();
                client.Close();
                _clients.TryRemove(client.Client.RemoteEndPoint.ToString(), out _); // Remove from dictionary
                _connectionSemaphore.Release(); // Release the connection slot
            }
        }

        private async Task HandleClientMessages(string username, TcpClient client, NetworkStream stream,
            CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[BufferSize];
        
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) // Client disconnected
                        break;
        
                    // Check if message starts with "screen_capture$" - if so, treat as binary
                    string header = Encoding.UTF8.GetString(buffer, 0, Math.Min(bytesRead, 100));
        
                    if (header.StartsWith("screen_capture$"))
                    {
                        // Binary message: screen_capture$response_id$[binary data]
                        int firstDollar = header.IndexOf('$');
                        int secondDollar = header.IndexOf('$', firstDollar + 1);
        
                        if (secondDollar > firstDollar)
                        {
                            string responseId = header.Substring(firstDollar + 1, secondDollar - firstDollar - 1);
        
                            // Calculate where binary data starts
                            string headerPart = $"screen_capture${responseId}$";
                            int headerBytes = Encoding.UTF8.GetByteCount(headerPart);
        
                            // Only log if debug mode is enabled
                            if (Agent.debug_mode)
                            {
                                Logging.DebugLazy("HandleClientMessages", "Binary message detected",
                                    () => $"ResponseID: {responseId}, HeaderBytes: {headerBytes}");
                            }
        
                            // Collect binary data from initial buffer
                            List<byte> binaryData = new List<byte>();
                            if (bytesRead > headerBytes)
                            {
                                binaryData.AddRange(buffer.Skip(headerBytes).Take(bytesRead - headerBytes));
                            }
        
                            // Read remaining data (if any)
                            while (true)
                            {
                                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                                if (bytesRead == 0)
                                    break;
        
                                binaryData.AddRange(buffer.Take(bytesRead));
        
                                if (bytesRead < buffer.Length)
                                    break;
                            }
        
                            // Validate JPEG header
                            bool isValidJpeg = binaryData.Count >= 2 && binaryData[0] == 0xFF && binaryData[1] == 0xD8;
        
                            // Only log in debug mode - this is called VERY frequently
                            if (Agent.debug_mode)
                            {
                                Logging.DebugLazy("HandleClientMessages", "Binary data collected",
                                    () => $"Size: {binaryData.Count} bytes, Valid JPEG: {isValidJpeg}");
                                
                                if (binaryData.Count >= 20)
                                {
                                    Logging.DebugLazy("HandleClientMessages", "First 20 bytes", 
                                        () => string.Join(" ", binaryData.Take(20).Select(b => $"0x{b:X2}")));
                                }
                            }
        
                            if (!isValidJpeg && Agent.debug_mode)
                            {
                                Logging.ErrorLazy("HandleClientMessages", "Invalid JPEG data received!",
                                    () => $"First bytes: 0x{binaryData[0]:X2} 0x{binaryData[1]:X2}, Username: {username}");
                            }
        
                            // Send binary data directly to server
                            try
                            {
                                await Remote_Control_Send_Screen(responseId, binaryData.ToArray());
                            }
                            catch (Exception ex)
                            {
if (Agent.debug_mode)
    Logging.ErrorLazy("HandleClientMessages", "Failed to send binary data to server", () => ex.ToString());

                            }
                        }
                    }
                    else
                    {
                        // Text message - handle normally
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        
                        if (Agent.debug_mode)
                        {
                            Logging.Debug("HandleClientMessages", $"Message from user {username}",
                                "not displaying due to high data usage");
                        }
        
                        try
                        {
                            if (Agent.debug_mode)
                            {
if (Agent.debug_mode)
    Logging.Remote_Control("HandleClientMessages", "Processing message", "Task triggered");

                            }
                            await ProcessMessage(message);
                        }
                        catch (Exception ex)
                        {
if (Agent.debug_mode)
    Logging.ErrorLazy("ProcessMessage", "Error invoking client response.", () => ex.ToString());

                        }
                    }
                }
            }
            catch (IOException ex)
            {
                if (Agent.debug_mode)
                {
                    Logging.ErrorLazy("HandleClientMessages", "Client disconnected due to IOException",
                        () => ex.ToString());
                }
            }
            catch (Exception ex)
            {
if (Agent.debug_mode)
    Logging.ErrorLazy("HandleClientMessages", "Error handling messages from client.", () => ex.ToString());

            }
            finally
            {
                stream.Close();
                client.Close();
                _clients.TryRemove(username, out _);
                
                if (Agent.debug_mode)
                {
if (Agent.debug_mode)
    Logging.Debug("HandleClientMessages", "Client disconnected", username);

                }
            }
        }

        private async Task ProcessMessage(string message)
        {
            try
            {
                Logging.Remote_Control("Service.Remote_Agent_Server_ProcessMessage", "Processing message",
                    "Task triggered");

                // Split message per $
                string[] messageParts = message.Split('$');

                // Handle specific commands, e.g., template code
                if (messageParts[0] == "screen_capture")
                {
                    // Respond with device identity
if (Agent.debug_mode)
    Logging.Debug("Service.Remote_Agent_Server_ProcessMessage", "Message Received", messageParts[1]);


                    try
                    {
                        //await remote_server_client.InvokeAsync("ReceiveClientResponse", messageParts[1], messageParts[2]);

                        await Remote_Control_Send_Screen(messageParts[1], messageParts[2]);
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Service.Remote_Agent_Server_ProcessMessage", "Error invoking client response.",
                            ex.ToString());
                    }
                }
                else if (messageParts[0] == "screen_indexes")
                {
                    await remote_server_client.SendAsync("ReceiveClientResponse", messageParts[1], messageParts[2], false);
                }
                else if (messageParts[0] == "users")
                {
                    await remote_server_client.SendAsync("ReceiveClientResponse", messageParts[1], messageParts[2], false);
                }
                else if (messageParts[0] == "clipboard_content")
                {
                    await remote_server_client.SendAsync("ReceiveClientResponse", messageParts[1], messageParts[2], false);
                }
                else if (messageParts[0] == "chat_message")
                {
                    await remote_server_client.SendAsync("ReceiveClientResponse", messageParts[1], messageParts[2], true);
                }
                else if (messageParts[0] == "remote_access_response")
                {

                    if (messageParts[2] == "accepted")
                    {
                        _remoteScreenControlGrantedUsers.Add(messageParts[3]);
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(messageParts[3]))
                            if (_remoteScreenControlGrantedUsers.Contains(messageParts[3]))
                                _remoteScreenControlGrantedUsers.Remove(messageParts[3]);
                    }

                    await remote_server_client.SendAsync("ReceiveClientResponse", messageParts[1], messageParts[2], false);
                }
                else
                {
                    Logging.Debug("Service.Remote_Agent_Server_ProcessMessage", "Unknown message type",
                        messageParts[0]);
                }
            }
            catch (Exception ex)
            {
if (Agent.debug_mode)
    Logging.Error("Service.Remote_Agent_Server_ProcessMessage", "Error processing message.", ex.ToString());

            }
        }

        public async Task SendToClient(string username, string message)
        {
            try
            {
if (Agent.debug_mode)
    Logging.Debug("Service.Remote_Agent_Server_SendToClient", "Sending message to client", message);


                if (_clients.TryGetValue(username, out TcpClient client) && client.Connected)
                {
                    NetworkStream stream = client.GetStream();
                    // Append a newline character to the message to signify the end of the message
                    string messageWithDelimiter = message + "\n"; // Use "\n" or any other delimiter
                    byte[] messageBytes = Encoding.UTF8.GetBytes(messageWithDelimiter);
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    await stream.FlushAsync();
if (Agent.debug_mode)
    Logging.Debug("Service.Remote_Agent_Server_SendToClient", "Sent message to client", message);

                }
                else
                {
                    Logging.Error("Service.Remote_Agent_Server_SendToClient", "No client connected with username",
                        username);
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Remote_Agent_Server_SendToClient", "Failed to send message to client",
                    ex.ToString());
            }
        }

        private async Task Remote_Control_Send_Screen(string response_id, string message)
        {
            try
            {
                // Deserialise device identity
                var jsonDocument = JsonDocument.Parse(device_identity_json);
                var deviceIdentityElement = jsonDocument.RootElement.GetProperty("device_identity");

                Device_Identity device_identity_object = JsonSerializer.Deserialize<Device_Identity>(deviceIdentityElement.ToString());
if (Agent.debug_mode)
    Logging.Debug("device_identity_object", "", device_identity_object.package_guid);


                // Create the new full JSON object
                var fullJson = new
                {
                    device_identity = device_identity_object, // Original deserialized device identity
                    remote_control = new // Manually added section
                    {
                        response_id = response_id,
                        result = message
                    }
                };

                // Serialize the full JSON back into a string
                string outputJson =
                    JsonSerializer.Serialize(fullJson, new JsonSerializerOptions { WriteIndented = true });
if (Agent.debug_mode)
    Logging.Debug("Remote_Control_Send_Screen", "outputJson", outputJson);


                // Create a HttpClient instance
                using (var httpClient = new HttpClient())
                {
                    // Set the content type header
                    httpClient.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Package-Guid", device_identity_object.package_guid);

                    Logging.Debug("Remote_Control_Send_Screen", "communication_server",
                        Global.Configuration.Agent.http_https + remote_server_url_command + "/Agent/Windows/Remote/Command");

                    // Send the JSON data to the server
                    var response = await httpClient.PostAsync(
                        Global.Configuration.Agent.http_https + remote_server_url_command + "/Agent/Windows/Remote/Command",
                        new StringContent(outputJson, Encoding.UTF8, "application/json"));

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, handle the response
                        var result = await response.Content.ReadAsStringAsync();
if (Agent.debug_mode)
    Logging.Debug("Remote_Control_Send_Screen", "result", result);

                    }
                    else
                    {
                        // Request failed, handle the error
                        Logging.Debug("Remote_Control_Send_Screen", "request",
                            "Request failed: " + response.StatusCode + " " + response.Content.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
if (Agent.debug_mode)
    Logging.Error("Remote_Control_Send_Screen", "failed", ex.ToString());

            }
        }

        private async Task Remote_Control_Send_Screen(string response_id, byte[] binaryData)
        {
            try
            {
                // ✅ DEBUGGING: Validate JPEG before sending
                bool isValidJpeg = binaryData.Length >= 2 && binaryData[0] == 0xFF && binaryData[1] == 0xD8;
        
                if (Agent.debug_mode)
                    Logging.Debug("Remote_Control_Send_Screen", "Preparing to send binary data",
                        $"Size: {binaryData.Length} bytes, Valid JPEG: {isValidJpeg}");
        
                if (binaryData.Length >= 20)
                {
                    string firstBytes = string.Join(" ", binaryData.Take(20).Select(b => $"0x{b:X2}"));
                    
                    if (Agent.debug_mode)
if (Agent.debug_mode)
    Logging.Debug("Remote_Control_Send_Screen", "First 20 bytes before sending", firstBytes);

                }
        
                var jsonDocument = JsonDocument.Parse(device_identity_json);
                var deviceIdentityElement = jsonDocument.RootElement.GetProperty("device_identity");
                Device_Identity device_identity_object = JsonSerializer.Deserialize<Device_Identity>(deviceIdentityElement.ToString());
        
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Package-Guid", device_identity_object.package_guid);
        
                    var multipartContent = new MultipartFormDataContent();
        
                    var fullJson = new { device_identity = device_identity_object };
                    var deviceIdentityJson = JsonSerializer.Serialize(fullJson);
                    multipartContent.Add(new StringContent(deviceIdentityJson, Encoding.UTF8, "application/json"), "device_identity");
                    multipartContent.Add(new StringContent(response_id), "response_id");
                    multipartContent.Add(new ByteArrayContent(binaryData), "screenshot", "screenshot.jpg");
        
                    if (Agent.debug_mode)
if (Agent.debug_mode)
    Logging.Debug("Remote_Control_Send_Screen", "Sending HTTP POST", $"{binaryData.Length} bytes");

        
                    var response = await httpClient.PostAsync(
                        Global.Configuration.Agent.http_https + remote_server_url_command + "/Agent/Windows/Remote/Command",
                        multipartContent);
        
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
    
                        if (Agent.debug_mode)
if (Agent.debug_mode)
    Logging.Debug("Remote_Control_Send_Screen", "Upload successful", result);

                    }
                    else
                    {
                        Logging.Error("Remote_Control_Send_Screen", "Upload failed",
                            $"{response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                    }
                }
            }
            catch (Exception ex)
            {
if (Agent.debug_mode)
    Logging.Error("Remote_Control_Send_Screen", "Binary upload failed", ex.ToString());

            }
        }

        public void Stop()
        {
            _cancellationTokenSourceLocal.Cancel();
            _listener.Stop();
if (Agent.debug_mode)
    Logging.Debug("Service.Remote_Agent_Server_Stop", "Server stopped.", "");

        }

        #endregion

        #region ServerConfig
    
        private async Task LoadServerConfig()
        {
            try
            {
                string agentSettingsJson = await File.ReadAllTextAsync(Application_Paths.agent_settings_json_path);

                agentSettingsJson = String_Encryption.Decrypt(agentSettingsJson, Application_Settings.NetLock_Local_Encryption_Key);

                // Deserialize the JSON string into AgentSettings object
                var agentSettings = JsonSerializer.Deserialize<AgentSettings>(agentSettingsJson);

                if (agentSettings != null)
                {
                    _agentSettingsRemoteServiceEnabled = agentSettings.RemoteServiceEnabled;
                    _agentSettingsRemoteShellEnabled = agentSettings.RemoteShellEnabled;
                    _agentSettingsRemoteFileBrowserEnabled = agentSettings.RemoteFileBrowserEnabled;
                    _agentSettingsRemoteTaskManagerEnabled = agentSettings.RemoteTaskManagerEnabled;
                    _agentSettingsRemoteServiceManagerEnabled = agentSettings.RemoteServiceManagerEnabled;
                    _agentSettingsRemoteScreenControlEnabled = agentSettings.RemoteScreenControlEnabled;
                    _agentSettingsRemoteScreenControlUnattendedAccess = agentSettings.RemoteScreenControlUnattendedAccess;
 
                    Logging.Debug("Service.LoadServerConfig", "Agent settings loaded",
                        $"RemoteServiceEnabled: {_agentSettingsRemoteServiceEnabled}, RemoteShellEnabled: {_agentSettingsRemoteShellEnabled}, RemoteFileBrowserEnabled: {_agentSettingsRemoteFileBrowserEnabled}, RemoteTaskManagerEnabled: {_agentSettingsRemoteTaskManagerEnabled}, RemoteServiceManagerEnabled: {_agentSettingsRemoteServiceManagerEnabled}, RemoteScreenControlEnabled: {_agentSettingsRemoteScreenControlEnabled}");
                }
                
                // Get access key & authorized state
                access_key = Global.Initialization.Server_Config.Access_Key();
                authorized = Global.Initialization.Server_Config.Authorized();
                
                // Read device_identity.json file & decrypt
                string jsonString = await File.ReadAllTextAsync(Application_Paths.device_identity_json_path);
                device_identity_json = String_Encryption.Decrypt(jsonString, Application_Settings.NetLock_Local_Encryption_Key);
if (Agent.debug_mode)
    Logging.Debug("Service.LoadServerConfig", "Device identity loaded", device_identity_json);

                
                // Check servers | We do not want to spam the server with requests here. 
                if (authorized && !remote_server_status || !file_server_status)
                    await Global.Initialization.Check_Connection.Check_Servers();
            }
            catch (Exception ex)
            {
if (Agent.debug_mode)
    Logging.Error("Service.LoadServerConfig", "Error loading server configuration", ex.ToString());

            }
        }

        #endregion
        
    } 
}
