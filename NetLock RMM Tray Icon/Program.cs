using System;
using Avalonia;
using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Global.Helper;

namespace NetLock_RMM_Tray_Icon
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
    
    class UserClient
    {
        private static TcpClient? _client;
        private static NetworkStream? _stream;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private bool _isConnected = false;
        private bool _isReconnecting = false;

        public class Command
        {
            public string response_id { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public string command { get; set; } = string.Empty;
        }
        
        private AccessRequestWindow? _accessRequestWindow;
        private ChatWindow? _chatWindow;
        
        public async Task Local_Server_Connect()
        {
            try
            {
                // Close existing connection if any
                if (_client != null)
                {
                    try
                    {
                        _stream?.Close();
                        _client?.Close();
                    }
                    catch { }
                }

                _client = new TcpClient();
                _client.ReceiveTimeout = 5000;
                _client.SendTimeout = 5000;
                
                await _client.ConnectAsync("127.0.0.1", 7338);
                _stream = _client.GetStream();
                _isConnected = true;
                
                // Send username to identify the user
                string username = Environment.UserName;
                await Local_Server_Send_Message($"username${username}tray");

                Console.WriteLine("Connected to the local server.");
                Logging.Debug("UserClient", "Local_Server_Connect", "Connected to the local server.");

                // Start listening for messages from the server
                _ = Local_Server_Handle_Server_Messages(_cancellationTokenSource.Token);
                    
                // Start checking the connection status in a separate task
                _ = MonitorConnectionAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _isConnected = false;
                Console.WriteLine($"Failed to connect to the local server: {ex.Message}");
                Logging.Debug("UserClient", "Local_Server_Connect", $"Failed to connect to the local server: {ex.Message}");
                
                // Start reconnection attempts immediately
                _ = MonitorConnectionAsync(_cancellationTokenSource.Token);
            }
        }
        
        private ConcurrentQueue<Command> _commandQueue = new ConcurrentQueue<Command>();

        private async Task Local_Server_Handle_Server_Messages(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Listening for messages from the server...");

                // Buffer to read incoming data
                byte[] buffer = new byte[4096];
                StringBuilder messageBuilder = new StringBuilder();

                while (!cancellationToken.IsCancellationRequested && _isConnected)
                {
                    try
                    {
                        if (_stream == null || !_stream.CanRead)
                        {
                            Console.WriteLine("Stream is no longer readable. Connection lost.");
                            _isConnected = false;
                            break;
                        }

                        // Read the incoming message length
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                        if (bytesRead == 0)
                        {
                            // Connection closed by server
                            Console.WriteLine("Server closed the connection (0 bytes read).");
                            _isConnected = false;
                            break;
                        }

                        if (bytesRead > 0)
                        {
                            // Convert the byte array to string
                            string messageChunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            messageBuilder.Append(messageChunk);

                            // Check if the message is complete
                            if (IsCompleteJson(messageBuilder.ToString(), out string completeMessage))
                            {
                                // Clear the builder for the next message
                                messageBuilder.Clear();

                                try
                                {
                                    Console.WriteLine($"Received complete message: {completeMessage}");

                                    // Deserialize the complete message to Command object
                                    Command command = JsonSerializer.Deserialize<Command>(completeMessage);

                                    if (command != null)
                                    {
                                        // Enqueue the command for processing
                                        _commandQueue.Enqueue(command);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Failed to deserialize command.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to deserialize message: {ex.Message}");
                                }
                            }
                        }

                        // Process commands from the queue
                        while (_commandQueue.TryDequeue(out Command queuedCommand))
                        {
                            // Process the command asynchronously
                            await ProcessCommand(queuedCommand);
                        }
                    }
                    catch (System.IO.IOException ioEx)
                    {
                        Console.WriteLine($"IO Error in message handler: {ioEx.Message}");
                        _isConnected = false;
                        break;
                    }
                    catch (SocketException sockEx)
                    {
                        Console.WriteLine($"Socket error in message handler: {sockEx.Message}");
                        _isConnected = false;
                        break;
                    }
                }
                
                if (!_isConnected)
                {
                    Console.WriteLine("Message listener detected connection loss. Triggering reconnect...");
                    Logging.Debug("UserClient", "Local_Server_Handle_Server_Messages", "Connection lost, exiting message handler.");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Listening was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while listening for server messages: {ex.Message}");
                _isConnected = false;
                Logging.Error("UserClient", "Local_Server_Handle_Server_Messages", ex.ToString());
            }
        }

        // Helper method to check if the message is complete JSON
        private bool IsCompleteJson(string json, out string completeJson)
        {
            completeJson = null;

            // Check for valid JSON (starting and ending braces)
            if (json.Trim().StartsWith("{") && json.Trim().EndsWith("}"))
            {
                // Count the braces
                int openBraces = 0;
                int closeBraces = 0;

                foreach (char c in json)
                {
                    if (c == '{') openBraces++;
                    if (c == '}') closeBraces++;
                }

                // If the count of open and close braces is the same, we have a complete JSON object
                if (openBraces == closeBraces)
                {
                    completeJson = json;
                    return true;
                }
            }

            return false;
        }
        
        // Work in progress - command processing
        private async Task ProcessCommand(Command command)
        {
            try
            {
                Console.WriteLine($"Processing command: {command.type} with response ID: {command.response_id}");
                Logging.Debug("UserClient", "ProcessCommand", $"Processing command: {command.type} with response ID: {command.response_id}");
                
                switch (command.type)
                {
                    case "show_chat_window": // Show chat window
                        try
                        {
                            Logging.Debug("UserClient", "ProcessCommand", "Showing chat window as per server command.");
                           
                            // Deserialize the command.command json to get additional parameters if needed
                            /*
                                var jsonObject = new
                                {
                                   firstName = firstName,
                                   lastName = lastName,
                                   command = 0, // 0 = show chat window
                                };
                            */

                            // Example of logging the raw JSON command
                            Logging.Debug("CommandJson", "json", command.command);

                            string firstName = "";
                            string lastName = "";
                            
                            // Deserialisierung des gesamten JSON-Strings
                            using (JsonDocument document = JsonDocument.Parse(command.command))
                            {
                                JsonElement firstNameElement = document.RootElement.GetProperty("firstName");
                                firstName = firstNameElement.ToString();
                                
                                JsonElement lastNameElement = document.RootElement.GetProperty("lastName");
                                lastName = lastNameElement.ToString();
                            }
                            
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                                {
                                    if (_chatWindow == null || !_chatWindow.IsVisible)
                                    {
                                        _chatWindow = new ChatWindow();
                                        _chatWindow.FirstName = firstName;
                                        _chatWindow.LastName = lastName;
                                        _chatWindow.ResponseId = command.response_id;
                                        _chatWindow.Show();
                                    }
                                    else
                                    {
                                        _chatWindow.FirstName = firstName;
                                        _chatWindow.LastName = lastName;
                                        _chatWindow.ResponseId = command.response_id;
                                        _chatWindow.WindowState = WindowState.Normal; // if minimized
                                        _chatWindow.Activate();
                                        _chatWindow.Topmost = true;  // Set topmost to bring to front
                                        _chatWindow.Topmost = false; // Reset topmost
                                        _chatWindow.Focus();
                                    }
                                }
                            });

                        }
                        catch (Exception ex)
                        {
                            Logging.Error("UserClient", "ProcessCommand", $"Failed to open chat window: {ex.ToString()}");
                            Console.WriteLine($"Failed to open chat window: {ex.Message}");
                        }
                        break;
                    case "hide_chat_window": // Hide chat window
                        try
                        {
                            Logging.Debug("UserClient", "ProcessCommand", "Hiding chat window as per server command.");

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (_chatWindow != null && _chatWindow.IsVisible)
                                {
                                    _chatWindow.Hide();
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("UserClient", "ProcessCommand", $"Failed to close chat window: {ex.ToString()}");
                            Console.WriteLine($"Failed to close chat window: {ex.Message}");
                        }
                        break;
                    case "exit_chat_window": // Close chat window
                        try
                        {
                            Logging.Debug("UserClient", "ProcessCommand", "Exiting chat window as per server command.");

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (_chatWindow != null && _chatWindow.IsVisible)
                                {
                                    _chatWindow.Close();
                                    _chatWindow = null;
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("UserClient", "ProcessCommand", $"Failed to exit chat window: {ex.ToString()}");
                            Console.WriteLine($"Failed to exit chat window: {ex.Message}");
                        }
                        break;
                    case "play_sound": // Respond to ping
                        Logging.Debug("UserClient", "ProcessCommand", "Playing sound as per server command.");
                        Console.Beep(); // Play a beep sound
                        break;
                    case "new_chat_message": // New chat message received
                        try
                        {
                            Logging.Debug("UserClient", "ProcessCommand", "New chat message received as per server command.");

                            string chatMessage = command.command;
                            
                            // If chat window is open, display the message
                            if (_chatWindow != null && _chatWindow.IsVisible)
                            {
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                        var bubble = _chatWindow.CreateMessageBubble(command.command, _chatWindow.FirstName, _chatWindow.LastName, false);
                                        _chatWindow.ChatMessagesPanel.Children.Add(bubble);
                                        
                                        // Scroll to bottom to show new message
                                        _chatWindow.ChatScrollViewer.ScrollToEnd();
                                });
                            }       
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("UserClient", "ProcessCommand", $"Failed to process new chat message: {ex.ToString()}");
                            Console.WriteLine($"Failed to process new chat message: {ex.Message}");
                        }
                        break;
                    case "access_request": // Access request
                        try
                        {
                            Logging.Debug("UserClient", "ProcessCommand", "Access request received as per server command.");

                            // Extract operator names from command JSON
                            string firstName = "";
                            string lastName = "";
                            
                            try
                            {
                                if (!string.IsNullOrEmpty(command.command))
                                {
                                    using (JsonDocument document = JsonDocument.Parse(command.command))
                                    {
                                        if (document.RootElement.TryGetProperty("firstName", out JsonElement firstNameElement))
                                        {
                                            firstName = firstNameElement.GetString() ?? "";
                                        }
                                        
                                        if (document.RootElement.TryGetProperty("lastName", out JsonElement lastNameElement))
                                        {
                                            lastName = lastNameElement.GetString() ?? "";
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.Debug("UserClient", "ProcessCommand", $"Could not parse operator names from access request: {ex.Message}");
                            }

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (_accessRequestWindow == null || !_accessRequestWindow.IsVisible)
                                {
                                    _accessRequestWindow = new AccessRequestWindow(command.response_id, firstName, lastName);
                                    _accessRequestWindow.Show();
                                    Console.Beep(); // Play a beep sound
                                }
                                else
                                {
                                    _accessRequestWindow.WindowState = WindowState.Normal; // if minimized
                                    _accessRequestWindow.Activate();
                                    _accessRequestWindow.Topmost = true;
                                    _accessRequestWindow.Topmost = false;
                                    _accessRequestWindow.Focus();
                                    Console.Beep(); // Play a beep sound
                                }
                            });
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("UserClient", "ProcessCommand", $"Failed to process access request: {ex.ToString()}");
                            Console.WriteLine($"Failed to process access request: {ex.Message}");
                        }
                        break;
                    case "end_remote_session": // End remote session
                        try
                        {
                            Logging.Debug("UserClient", "ProcessCommand", "Ending remote session as per server command.");

                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                // Close support overlay using static method
                                SupportOverlay.CloseSupportOverlay();
                                
                                // Close chat window if open
                                if (_chatWindow != null && _chatWindow.IsVisible)
                                {
                                    _chatWindow.Close();
                                    _chatWindow = null;
                                }
                                
                                // Close access request window if open
                                if (_accessRequestWindow != null && _accessRequestWindow.IsVisible)
                                {
                                    _accessRequestWindow.Close();
                                    _accessRequestWindow = null;
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("UserClient", "ProcessCommand", $"Failed to end remote session: {ex.ToString()}");
                            Console.WriteLine($"Failed to end remote session: {ex.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process command: {ex.Message}");
            }
        }

        public static async Task Local_Server_Send_Message(string message)
        {
            try
            {
                if (_stream != null && _client != null && _client.Connected)
                {
                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                    await _stream.FlushAsync();
                    Console.WriteLine("Sent message to remote agent");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send message to the local server: {ex.Message}");
            }
        }

        private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
        {
            // Start with immediate check (no initial delay)
            bool firstCheck = true;
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Only delay after first check
                    if (!firstCheck)
                    {
                        await Task.Delay(2000, cancellationToken); // Check every 2 seconds (faster)
                    }
                    firstCheck = false;
                    
                    // Check if the client is still connected
                    bool isConnected = _client != null && _client.Connected && _isConnected;
                    
                    // Additional check: try to detect if socket is really alive
                    if (_client != null && _client.Client != null && isConnected)
                    {
                        try
                        {
                            bool pollResult = _client.Client.Poll(100, SelectMode.SelectRead);
                            bool dataAvailable = _client.Client.Available > 0;
                            
                            // If poll returns true but no data available, connection is likely broken
                            if (pollResult && !dataAvailable)
                            {
                                Console.WriteLine("Poll detected broken connection.");
                                isConnected = false;
                            }
                        }
                        catch (Exception pollEx)
                        {
                            Console.WriteLine($"Poll check failed: {pollEx.Message}");
                            isConnected = false;
                        }
                    }
                    else if (_client == null || !_client.Connected)
                    {
                        isConnected = false;
                    }
                    
                    if (!isConnected && !_isReconnecting)
                    {
                        _isReconnecting = true;
                        _isConnected = false;
                        
                        Console.WriteLine("Connection lost. Attempting to reconnect...");
                        Logging.Debug("UserClient", "MonitorConnectionAsync", "Connection lost. Attempting to reconnect...");

                        // Close existing resources
                        try
                        {
                            _stream?.Close();
                            _client?.Close();
                        }
                        catch { }
                        
                        _client = null;
                        _stream = null;

                        // Try to reconnect with exponential backoff
                        int retryCount = 0;
                        int maxRetries = 10;
                        int baseDelay = 1000; // Start with 1 second (faster initial retry)
                        
                        while (!_isConnected && !cancellationToken.IsCancellationRequested && retryCount < maxRetries)
                        {
                            try
                            {
                                retryCount++;
                                int delay = Math.Min(baseDelay * (int)Math.Pow(2, retryCount - 1), 30000); // Max 30 seconds
                                
                                Console.WriteLine($"Reconnection attempt {retryCount}/{maxRetries} in {delay}ms...");
                                Logging.Debug("UserClient", "MonitorConnectionAsync", $"Reconnection attempt {retryCount}/{maxRetries}");
                                
                                await Task.Delay(delay, cancellationToken);
                                
                                _client = new TcpClient();
                                _client.ReceiveTimeout = 5000;
                                _client.SendTimeout = 5000;
                                
                                // Try to connect with timeout
                                var connectTask = _client.ConnectAsync("127.0.0.1", 7338);
                                var timeoutTask = Task.Delay(5000, cancellationToken);
                                
                                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                                
                                if (completedTask == connectTask && !connectTask.IsFaulted)
                                {
                                    // Wait a bit to ensure connection is stable
                                    await Task.Delay(100, cancellationToken);
                                    
                                    // Verify connection is really established
                                    if (_client.Connected)
                                    {
                                        _stream = _client.GetStream();
                                        _isConnected = true;

                                        // Send username to identify the user
                                        string username = Environment.UserName;
                                        await Local_Server_Send_Message($"username${username}tray");
                                        
                                        Console.WriteLine("Successfully reconnected to the local server.");
                                        Logging.Debug("UserClient", "MonitorConnectionAsync", "Successfully reconnected to the local server.");

                                        // Restart listening for messages with new cancellation token
                                        _cancellationTokenSource.Cancel();
                                        _cancellationTokenSource = new CancellationTokenSource();
                                        _ = Local_Server_Handle_Server_Messages(_cancellationTokenSource.Token);
                                        
                                        retryCount = 0; // Reset retry count on success
                                    }
                                    else
                                    {
                                        throw new Exception("Connection established but not in connected state");
                                    }
                                }
                                else
                                {
                                    // Connection timeout or failed
                                    try
                                    {
                                        _client?.Close();
                                    }
                                    catch { }
                                    _client = null;
                                    Console.WriteLine($"Connection attempt timed out or failed.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Reconnect attempt {retryCount} failed: {ex.Message}");
                                Logging.Debug("UserClient", "MonitorConnectionAsync", $"Reconnect attempt {retryCount} failed: {ex.Message}");
                                
                                try
                                {
                                    _client?.Close();
                                }
                                catch { }
                                
                                _client = null;
                                _stream = null;
                            }
                        }
                        
                        // If max retries reached, reset and try again after a longer delay
                        if (!_isConnected && retryCount >= maxRetries)
                        {
                            Logging.Error("UserClient", "MonitorConnectionAsync", $"Max reconnection attempts reached. Will retry.");
                            await Task.Delay(60000, cancellationToken); // Wait 1 minute before restarting attempts
                        }
                        
                        _isReconnecting = false;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Task was cancelled, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in connection monitor: {ex.Message}");
                    Logging.Error("UserClient", "MonitorConnectionAsync", ex.ToString());
                    _isReconnecting = false;
                }
            }
        }

        public void Disconnect()
        {
            _cancellationTokenSource.Cancel();
            _isConnected = false;
            
            try
            {
                _stream?.Close();
                _client?.Close();
            }
            catch { }
            
            Console.WriteLine("Disconnected from the server.");
            Logging.Debug("UserClient", "Disconnect", "Disconnected from the server.");
        }
    }
}
