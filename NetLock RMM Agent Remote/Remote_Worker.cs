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

namespace NetLock_RMM_Agent_Remote
{
    public class Remote_Worker : BackgroundService
    {
        private readonly ILogger<Remote_Worker> _logger;

        public Remote_Worker(ILogger<Remote_Worker> logger)
        {
            _logger = logger;
        }

        private bool ssl = false;

        // Local Server
        private const int Port = 7337;
        private const string ServerIp = "127.0.0.1"; // Localhost
        private TcpClient local_server_client;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private Timer local_server_clientCheckTimer;

        // Remote Server Client
        string remote_server_url = String.Empty;
        string remote_server_url_command = String.Empty;
        private HubConnection remote_server_client;
        private Timer remote_server_clientCheckTimer;
        bool remote_server_client_setup = false;

        // User process monitoring
        private Timer user_process_monitoringCheckTimer;

        // File Server
        private string file_server_url = String.Empty;

        // Device Identity
        public string device_identity = String.Empty;

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
            public string command { get; set; } // used for service, task manager, screen capture
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool firstRun = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (firstRun)
                {
                    firstRun = false;

                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                    try
                    {
                        Logging.Debug("Service.OnStart", "Service started", "Information");

                        // Start the timer to check the remote server status
                        local_server_clientCheckTimer = new Timer(async (e) => await Local_Server_Check_Connection_Status(), null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
                        remote_server_clientCheckTimer = new Timer(async (e) => await Remote_Server_Check_Connection_Status(), null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
                        user_process_monitoringCheckTimer = new Timer(async (e) => await CheckUserProcessStatus(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

                        _ = Task.Run(async () => await Local_Server_Start());

                        // Establishing a connection to the local server
                        _ = Task.Run(async () => await Local_Server_Connect()); // Läuft im Hintergrund
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Service.OnStart", "Error during service startup.", ex.ToString());
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task Local_Server_Connect()
        {
            try
            {
                local_server_client = new TcpClient();
                await local_server_client.ConnectAsync(ServerIp, Port);

                _stream = local_server_client.GetStream();
                _ = Local_Server_Handle_Server_Messages(_cancellationTokenSource.Token);

                Console.WriteLine("Connected to the local server.");
                Logging.Debug("Service.Local_Server_Connect", "Connected to the local server.", "");

                // Get the device identity from the local server
                await Local_Server_Send_Message("get_device_identity");
            }
            catch (Exception ex)
            {
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
                    Logging.Debug("Service.Local_Server_Handle_Server_Messages", "Received message", message);

                    Console.WriteLine("Received message: " + message);

                    // Split the message per $
                    string[] messageParts = message.Split('$');

                    // device_identity
                    if (messageParts[0].ToString() == "device_identity")
                    {
                        Logging.Debug("Service.Local_Server_Handle_Server_Messages", "Device identity received", messageParts[1]);

                        if (String.IsNullOrEmpty(messageParts[1]))
                            Logging.Error("Service.Local_Server_Handle_Server_Messages", "Device identity is empty or not ready yet.", "");
                        else
                        {
                            ssl = Convert.ToBoolean(messageParts[2]);

                            device_identity = messageParts[1];
                            remote_server_url = $"{messageParts[3]}/commandHub";
                            remote_server_url_command = $"{messageParts[3]}";
                            file_server_url = $"{messageParts[4]}";

                            // if messageParts[2] (ssl) is true, use https, else http
                            if (ssl)
                            {
                                remote_server_url = "https://" + remote_server_url;
                                remote_server_url_command = "https://" + remote_server_url_command;
                            }
                            else
                            {
                                remote_server_url = "http://" + remote_server_url;
                                remote_server_url_command = "http://" + remote_server_url_command;
                            }

                            Logging.Debug("Service.Local_Server_Handle_Server_Messages", "Device identity", device_identity);
                            Logging.Debug("Service.Local_Server_Handle_Server_Messages", "Remote server URL", remote_server_url);
                            Logging.Debug("Service.Local_Server_Handle_Server_Messages", "Remote server URL (command)", remote_server_url_command);
                            Logging.Debug("Service.Local_Server_Handle_Server_Messages", "File server URL", file_server_url);

                            await Remote_Server_Check_Connection_Status();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Local_Server_Handle_Server_Messages", "Failed to handle server messages.", ex.ToString());
            }
        }

        private async Task Local_Server_Send_Message(string message)
        {
            try
            {
                if (_stream != null && local_server_client.Connected)
                {
                    Logging.Debug("Service.Local_Server_Send_Message", "Sent message", message);

                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    await _stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Local_Server_Send_Message", "Failed to send message to the local server.", ex.ToString());
            }
        }

        private async Task Local_Server_Check_Connection_Status()
        {
            try
            {
                if (local_server_client == null)
                {
                    Logging.Error("Service.Check_Connection_Status", "local_server_client is null.", "");
                    return;
                }

                // Check if the local_server_client is connected and if, execute regular tasks
                if (local_server_client.Connected)
                {
                    Logging.Debug("Service.Check_Connection_Status", "Local server connection is active.", "");

                    // Get the device identity from the local server
                    await Local_Server_Send_Message("get_device_identity");
                }
                else
                {
                    Logging.Debug("Service.Check_Connection_Status", "Local server connection lost, attempting to reconnect.", "");
                    await Local_Server_Connect();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Check_Connection_Status", "Failed to check remote_server_client or local_server_client status.", ex.ToString());
            }
        }

        private async Task Remote_Server_Check_Connection_Status()
        {
            try
            {
                if (!string.IsNullOrEmpty(device_identity))
                {
                    if (!remote_server_client_setup || remote_server_client == null || remote_server_client.State == HubConnectionState.Disconnected)
                    {
                        await Setup_SignalR();
                    }
                    else if (remote_server_client.State == HubConnectionState.Connected)
                    {
                        Logging.Debug("Service.Check_Connection_Status", "Remote server connection is already active.", "");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Check_Connection_Status", "Failed to check remote_server_client status.", ex.ToString());
            }
        }

        public async Task Setup_SignalR()
        {
            try
            {
                // Check if the device_identity is empty, if so, return
                if (String.IsNullOrEmpty(device_identity))
                {
                    Logging.Error("Service.Setup_SignalR", "Device identity is empty.", "");
                    return;
                }
                else
                    Logging.Debug("Service.Setup_SignalR", "Device identity is not empty. Preparing remote connection.", "");

                Logging.Debug("Service.Setup_SignalR", "Device identity JSON", device_identity);

                // Deserialise device identity
                var jsonDocument = JsonDocument.Parse(device_identity);
                var deviceIdentityElement = jsonDocument.RootElement.GetProperty("device_identity");

                Device_Identity device_identity_object = JsonSerializer.Deserialize<Device_Identity>(deviceIdentityElement.ToString());

                if (remote_server_client != null)
                    await remote_server_client.DisposeAsync();

                remote_server_client = new HubConnectionBuilder()
                .WithUrl(remote_server_url, options =>
                {
                    options.Headers.Add("Device-Identity", Uri.EscapeDataString(device_identity));
                    options.UseStatefulReconnect = true;
                    options.WebSocketConfiguration = socket =>
                    {
                        socket.KeepAliveInterval = TimeSpan.FromSeconds(30);
                    };
                }).ConfigureLogging(logging =>
                {
                    if (OperatingSystem.IsWindows())
                        logging.AddEventLog();

                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Warning);
                })
                .WithAutomaticReconnect()
                .Build();

                remote_server_client.On<string>("ReceiveMessage", async (command) =>
                {
                    Logging.Debug("Service.Setup_SignalR", "ReceiveMessage", command);

                    // Insert the logic here to execute the command

                    // Example: If the command is "sync", send a message to the local server to force a sync with the remote server
                    if (command == "sync")
                        await Local_Server_Send_Message("sync");
                });

                // Receive a message from the remote server, process the command and send a response back to the remote server
                remote_server_client.On<string>("SendMessageToClientAndWaitForResponse", async (command) =>
                {
                    Logging.Debug("Service.Setup_SignalR", "SendMessageToClientAndWaitForResponse", command);

                    // Deserialisation of the entire JSON string
                    Command_Entity command_object = JsonSerializer.Deserialize<Command_Entity>(command);
                    // Example: If the type is 0, execute the powershell code and send the response back to the remote server if wait_response is true

                    string result = string.Empty;

                    try
                    {
                        if (command_object.type == 0) // Remote Shell
                        {
                            if (OperatingSystem.IsWindows())
                                result = Windows.Helper.PowerShell.Execute_Script(command_object.type.ToString(), command_object.powershell_code);
                            else if (OperatingSystem.IsLinux())
                                result = Linux.Helper.Bash.Execute_Script("Remote Shell", true, command_object.powershell_code);
                            else if (OperatingSystem.IsMacOS())
                                result = MacOS.Helper.Zsh.Execute_Script("Remote Shell", true, command_object.powershell_code);

                            Logging.Debug("Client", "PowerShell executed", result);
                        }
                        else if (command_object.type == 1) // File Browser
                        {
                            // if linux or macos convert the path to linux/macos path
                            if (!String.IsNullOrEmpty(command_object.file_browser_path) && OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
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
                                result = JsonSerializer.Serialize(directoryDetails, new JsonSerializerOptions { WriteIndented = true });
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
                                result = IO.Move_Directory(command_object.file_browser_path, command_object.file_browser_path_move);
                            }
                            else if (command_object.file_browser_command == 5) // rename dir
                            {
                                result = IO.Rename_Directory(command_object.file_browser_path, command_object.file_browser_path_move);
                            }
                            else if (command_object.file_browser_command == 6) // create file
                            {
                                result = await IO.Create_File(command_object.file_browser_path, command_object.file_browser_file_content);
                            }
                            else if (command_object.file_browser_command == 7) // delete file
                            {
                                result = IO.Delete_File(command_object.file_browser_path).ToString();
                            }
                            else if (command_object.file_browser_command == 8) // move file
                            {
                                result = IO.Move_File(command_object.file_browser_path, command_object.file_browser_path_move);
                            }
                            else if (command_object.file_browser_command == 9) // rename file
                            {
                                result = IO.Rename_File(command_object.file_browser_path, command_object.file_browser_path_move);
                            }
                            else if (command_object.file_browser_command == 10) // download file from file server
                            {
                                // download url with tenant guid, location guid & device name
                                string download_url = file_server_url + "/admin/files/download/device" + "?guid=" + command_object.file_browser_file_guid + "&tenant_guid=" + device_identity_object.tenant_guid + "&location_guid=" + device_identity_object.location_guid + "&device_name=" + device_identity_object.device_name + "&access_key=" + device_identity_object.access_key + "&hwid=" + device_identity_object.hwid;

                                Logging.Debug("Service.Setup_SignalR", "Download URL", download_url);

                                result = await Http.DownloadFileAsync(ssl, download_url, command_object.file_browser_path, device_identity_object.package_guid);

                                Logging.Debug("Service.Setup_SignalR", "File downloaded", result);
                            }
                            else if (command_object.file_browser_command == 11) // upload file
                            {
                                string file_name = Path.GetFileName(command_object.file_browser_path);

                                // upload url with tenant guid, location guid & device name
                                string upload_url = file_server_url + "/admin/files/upload/device" + "?tenant_guid=" + device_identity_object.tenant_guid + "&location_guid=" + device_identity_object.location_guid + "&device_name=" + device_identity_object.device_name + "&access_key=" + device_identity_object.access_key + "&hwid=" + device_identity_object.hwid;

                                Logging.Debug("Service.Setup_SignalR", "Upload URL", upload_url);

                                // Upload the file to the server
                                result = await Http.UploadFileAsync(ssl, upload_url, command_object.file_browser_path, device_identity_object.package_guid);
                            }
                        }
                        else if (command_object.type == 2) // Service
                        {
                            // Deserialise the command_object.command json, using json document (action, name)
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
                            Logging.Debug("Service.Setup_SignalR", "Service Action", result);
                        }
                        else if (command_object.type == 3) // Task Manager Action
                        {
                            // Terminate process by pid
                            result = await Task_Manager.Terminate_Process_Tree(Convert.ToInt32(command_object.command));
                            Logging.Debug("Service.Setup_SignalR", "Terminate Process", result);
                        }
                        else if (command_object.type == 4) // Remote Control
                        {
                            // Check if the command requests connected users
                            if (command_object.command == "4")
                            {
                                // Get the connected users from _clients, seperate by comma
                                List<string> connected_users = _clients.Keys.ToList();

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
                                try
                                {
                                    Logging.Debug("Service.Setup_SignalR", "Remote Control command", command_object.command);

                                    // Check if ctrlaltdel and send through SAS instead
                                    if (command_object.remote_control_keyboard_input == "ctrlaltdel")
                                    {
                                        Logging.Debug("Service.Setup_SignalR", "Sending SAS for ctrlaltdel", command_object.remote_control_username);

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
                                    string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                                    Logging.Debug("Service.Setup_SignalR", "Remote Control json", json);

                                    // Send through local SignalR Hub to User
                                    await SendToClient(command_object.remote_control_username, json);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Error("Service.Setup_SignalR", "Failed to execute remote control command.", ex.ToString());
                                }

                                // Return because no further action is required
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result = ex.Message;
                        Logging.Error("Service.Setup_SignalR", "Failed to execute file browser command.", ex.ToString());
                    }

                    // Send the response back to the server
                    if (!String.IsNullOrEmpty(command_object.type.ToString()))
                    {
                        if (String.IsNullOrEmpty(result))
                            result = "Command executed. No result returned.";

                        Logging.Debug("Client", "Sending response back to the server", "result: " + result + "response_id: " + command_object.response_id);
                        await remote_server_client.InvokeAsync("ReceiveClientResponse", command_object.response_id, result);
                        Logging.Debug("Client", "Response sent back to the server", "result: " + result + "response_id: " + command_object.response_id);
                    }

                    await Task.CompletedTask;
                });

                // Start the connection
                await remote_server_client.StartAsync();

                remote_server_client_setup = true;

                Console.WriteLine("Connected to the remote server.");
                Logging.Debug("Service.Setup_SignalR", "Connected to the remote server.", "");
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Setup_SignalR", "Failed to start SignalR.", ex.ToString());
            }
        }

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
                Logging.Error("Service.CheckUserProcess", "Exception while checking or starting user processes.", ex.ToString());
            }
        }

        #endregion

        #region Remote Agent Local Server

        private const int Remote_Agent_Local_Port = 7338;
        private ConcurrentDictionary<string, TcpClient> _clients = new ConcurrentDictionary<string, TcpClient>(); // Clients by username
        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSourceLocal = new CancellationTokenSource();
        private SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(100); // Limit to 100 concurrent clients
        private const int BufferSize = 10 * 1024 * 1024; // 10 MB Buffer

        public async Task Local_Server_Start()
        {
            try
            {
                Logging.Debug("Service.Remote_Agent_Local_Server_Start", "Starting server...", "");
                Console.WriteLine("Starting server...");
                _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Remote_Agent_Local_Port);
                _listener.Start();
                Console.WriteLine("Remote Agent Server started. Waiting for connections...");
                Logging.Debug("Service.Remote_Agent_Local_Server_Start", "Server started. Waiting for connections...", "");

                while (!_cancellationTokenSourceLocal.Token.IsCancellationRequested)
                {
                    await _connectionSemaphore.WaitAsync(); // Throttle connections
                    var client = await _listener.AcceptTcpClientAsync();
                    if (client != null)
                    {
                        _ = Local_Server_Handle_Client(client, _cancellationTokenSourceLocal.Token); // Handle client asynchronously
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
                Logging.Error("Service.Remote_Agent_Local_Server_Start", "Error starting server.", ex.ToString());
            }
        }

        private async Task Local_Server_Handle_Client(TcpClient client, CancellationToken cancellationToken)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[BufferSize];

            try
            {
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true); // Enable KeepAlive

                // Wait for the client to send the username first
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                string initialMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string[] messageParts = initialMessage.Split('$');

                if (messageParts[0] == "username")
                {
                    string username = messageParts[1];
                    Console.WriteLine($"Client connected: {username}");
                    Logging.Debug("Service.Remote_Agent_Local_Server_Handle_Client", "Client connected", username);

                    // Add the client to the dictionary
                    _clients[username] = client;

                    // Handle messages from the client
                    await HandleClientMessages(username, client, stream, cancellationToken);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Client connection closed: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
                Logging.Error("Service.Remote_Agent_Local_Server_Handle_Client", "Error handling client.", ex.ToString());
            }
            finally
            {
                stream.Close();
                client.Close();
                _clients.TryRemove(client.Client.RemoteEndPoint.ToString(), out _); // Remove from dictionary
                _connectionSemaphore.Release(); // Release the connection slot
            }
        }

        private async Task HandleClientMessages(string username, TcpClient client, NetworkStream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[BufferSize];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) // Client disconnected
                        break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Logging.Debug("Service.Remote_Agent_Server_HandleClientMessages", $"Message from user {username}", "not displaying due to high data usage");

                    // Process client message
                    try
                    {
                        Logging.Remote_Control("Service.Remote_Agent_Server_HandleClientMessages", "Processing message", "Task triggered");
                        await ProcessMessage(message);
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Service.Remote_Agent_Server_ProcessMessage", "Error invoking client response.", ex.ToString());
                    }

                    // Der code wird nicht ausgeführt
                    /*_ = Task.Run(async () =>
                    {
                       
                    });*/
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Connection closed with {username}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling messages from {username}: {ex.Message}");
                Logging.Error("Service.Remote_Agent_Server_HandleClientMessages", "Error handling messages from client.", ex.ToString());
            }
            finally
            {
                stream.Close();
                client.Close();
                _clients.TryRemove(username, out _);
                Logging.Debug("Service.Remote_Agent_Server_HandleClientMessages", "Client disconnected", username);
            }
        }

        private async Task ProcessMessage(string message)
        {
            try
            {
                Logging.Remote_Control("Service.Remote_Agent_Server_ProcessMessage", "Processing message", "Task triggered");

                // Split message per $
                string[] messageParts = message.Split('$');

                // Handle specific commands, e.g., template code
                if (messageParts[0] == "screen_capture")
                {
                    // Respond with device identity
                    Logging.Debug("Service.Remote_Agent_Server_ProcessMessage", "Message Received", messageParts[1]);

                    try
                    {
                        //await remote_server_client.InvokeAsync("ReceiveClientResponse", messageParts[1], messageParts[2]);

                        await Remote_Control_Send_Screen(messageParts[1], messageParts[2]);
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Service.Remote_Agent_Server_ProcessMessage", "Error invoking client response.", ex.ToString());
                    }
                }
                else if (messageParts[0] == "screen_indexes")
                {
                    await remote_server_client.SendAsync("ReceiveClientResponse", messageParts[1], messageParts[2]);
                }
                else if (messageParts[0] == "users")
                {
                    await remote_server_client.SendAsync("ReceiveClientResponse", messageParts[1], messageParts[2]);
                }
                else if (messageParts[0] == "clipboard_content")
                { 
                    await remote_server_client.SendAsync("ReceiveClientResponse", messageParts[1], messageParts[2]);
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Remote_Agent_Server_ProcessMessage", "Error processing message.", ex.ToString());
            }
        }

        public async Task SendToClient(string username, string message)
        {
            try
            {
                Logging.Debug("Service.Remote_Agent_Server_SendToClient", "Sending message to client", message);

                if (_clients.TryGetValue(username, out TcpClient client) && client.Connected)
                {
                    NetworkStream stream = client.GetStream();
                    // Append a newline character to the message to signify the end of the message
                    string messageWithDelimiter = message + "\n"; // Use "\n" or any other delimiter
                    byte[] messageBytes = Encoding.UTF8.GetBytes(messageWithDelimiter);
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    await stream.FlushAsync();

                    Console.WriteLine($"Sent message to {username}: {message}");
                    Logging.Debug("Service.Remote_Agent_Server_SendToClient", "Sent message to client", message);
                }
                else
                {
                    Console.WriteLine($"No client connected with username: {username}");
                    Logging.Error("Service.Remote_Agent_Server_SendToClient", "No client connected with username", username);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to {username}: {ex.Message}");
                Logging.Error("Service.Remote_Agent_Server_SendToClient", "Failed to send message to client", ex.ToString());
            }
        }

        private async Task Remote_Control_Send_Screen(string response_id, string message)
        {
            try
            {
                // Deserialise device identity
                var jsonDocument = JsonDocument.Parse(device_identity);
                var deviceIdentityElement = jsonDocument.RootElement.GetProperty("device_identity");

                Device_Identity device_identity_object = JsonSerializer.Deserialize<Device_Identity>(deviceIdentityElement.ToString());

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
                string outputJson = JsonSerializer.Serialize(fullJson, new JsonSerializerOptions { WriteIndented = true });

                Logging.Debug("Remote_Control_Send_Screen", "outputJson", outputJson);

                // Create a HttpClient instance
                using (var httpClient = new HttpClient())
                {
                    // Set the content type header
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("Package-Guid", device_identity_object.package_guid);

                    Logging.Debug("Remote_Control_Send_Screen", "communication_server", remote_server_url_command + "/Agent/Windows/Remote/Command");

                    // Send the JSON data to the server
                    var response = await httpClient.PostAsync(remote_server_url_command + "/Agent/Windows/Remote/Command", new StringContent(outputJson, Encoding.UTF8, "application/json"));

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, handle the response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Debug("Remote_Control_Send_Screen", "result", result);
                    }
                    else
                    {
                        // Request failed, handle the error
                        Logging.Debug("Remote_Control_Send_Screen", "request", "Request failed: " + response.StatusCode + " " + response.Content.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Remote_Control_Send_Screen", "failed", ex.ToString());
            }
        }

        public void Stop()
        {
            _cancellationTokenSourceLocal.Cancel();
            _listener.Stop();
            Console.WriteLine("Server stopped.");
            Logging.Debug("Service.Remote_Agent_Server_Stop", "Server stopped.", "");
        }

    }
    #endregion
}
