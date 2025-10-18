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

        public class Command
        {
            public string response_id { get; set; } = string.Empty;
            public string type { get; set; } = string.Empty;
            public string command { get; set; } = string.Empty;
        }

        private ChatWindow? _chatWindow;

        public async Task Local_Server_Connect()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync("127.0.0.1", 7338);
                _stream = _client.GetStream();
                // Send username to identify the user
                string username = Environment.UserName;
                await Local_Server_Send_Message($"username${username}tray");

                // Start listening for messages from the server
                _ = Local_Server_Handle_Server_Messages(_cancellationTokenSource.Token);
                Console.WriteLine("Connected to the local server.");
                Logging.Debug("UserClient", "Local_Server_Connect", "Connected to the local server.");
                    
                // Start checking the connection status in a separate task
                _ = MonitorConnectionAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to the local server: {ex.Message}");
                Logging.Debug("UserClient", "Local_Server_Connect", $"Failed to connect to the local server: {ex.Message}");
            }
        }
        
        private ConcurrentQueue<Command> _commandQueue = new ConcurrentQueue<Command>();

        private async Task Local_Server_Handle_Server_Messages(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Listening for messages from the server...");

                // Buffer to read incoming data
                byte[] buffer = new byte[4096]; // Adjust the size of the buffer as needed
                StringBuilder messageBuilder = new StringBuilder();

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_stream.CanRead)
                    {
                        // Read the incoming message length
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

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
                    }

                    // Process commands from the queue
                    while (_commandQueue.TryDequeue(out Command queuedCommand))
                    {
                        // Process the command asynchronously
                        await ProcessCommand(queuedCommand);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Listening was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while listening for server messages: {ex.Message}");
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
            while (!cancellationToken.IsCancellationRequested)
            {
                // Check if the client is still connected
                if (_client == null || !_client.Connected)
                {
                    Console.WriteLine("Disconnected from the server. Attempting to reconnect...");

                    // Try to reconnect
                    while (!_client.Connected && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            _client = new TcpClient();
                            await _client.ConnectAsync("127.0.0.1", 7338);
                            _stream = _client.GetStream();

                            // Send username to identify the user
                            string username = Environment.UserName;
                            await Local_Server_Send_Message($"username${username}");
                            Console.WriteLine("Reconnected to the local server.");

                            // Restart listening for messages
                            _ = Local_Server_Handle_Server_Messages(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Reconnect attempt failed: {ex.Message}");
                            await Task.Delay(5000, cancellationToken); // Wait before retrying
                        }
                    }
                }

                // Wait for a while before the next check
                await Task.Delay(5000, cancellationToken); // Check every 5 seconds
            }

            // Clean up resources if disconnected
            Disconnect();
        }

        public void Disconnect()
        {
            _cancellationTokenSource.Cancel();
            _stream?.Close();
            _client?.Close();
            Console.WriteLine("Disconnected from the server.");
        }
    }
}
