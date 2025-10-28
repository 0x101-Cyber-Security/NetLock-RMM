using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NetLock_RMM_User_Process.Helper.Keyboard;
using NetLock_RMM_User_Process.Windows.Helper;
using NetLock_RMM_User_Process.Windows.Mouse;
using NetLock_RMM_User_Process.Windows.ScreenControl;
using WindowsInput;
using static System.Net.Mime.MediaTypeNames;

class UserClient
{
    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private Task _messageHandlerTask;
    private bool _isConnecting = false;

    public class Command
    {
        public string response_id { get; set; }
        public string type { get; set; }
        public string remote_control_screen_index { get; set; }
        public string remote_control_mouse_action { get; set; }
        public string remote_control_mouse_xyz { get; set; }
        public string remote_control_keyboard_input { get; set; }
        public string remote_control_keyboard_content { get; set; }
    }

    public async Task Local_Server_Connect()
    {
        // Start monitoring connection immediately - it will handle initial connection and reconnections
        _ = MonitorConnectionAsync(_cancellationTokenSource.Token);
        
        // Wait a moment to allow initial connection attempt
        await Task.Delay(1000);
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

            while (!cancellationToken.IsCancellationRequested && _client?.Connected == true)
            {
                if (_stream?.CanRead == true)
                {
                    // Read the incoming message length
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0)
                    {
                        // Server closed the connection
                        Console.WriteLine("Server closed the connection.");
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
                }
                else
                {
                    // Stream is not readable, likely disconnected
                    break;
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
            // Mark connection as broken so MonitorConnectionAsync can handle reconnection
            try
            {
                _client?.Close();
            }
            catch { }
        }
        finally
        {
            Console.WriteLine("Message handler task ended.");
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

    private async Task ProcessMessageAsync(string message)
    {
        try
        {
            // Deserialize the message to Command object
            Command command = JsonSerializer.Deserialize<Command>(message);

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
        catch (JsonException jsonEx)
        {
            Console.WriteLine($"Failed to deserialize message: {jsonEx.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while processing the message: {ex.Message}");
        }
    }

    // Big WIP
    private async Task ProcessCommand(Command command)
    {
        try
        {
            Console.WriteLine($"Processing command: {command.type} with response ID: {command.response_id}");
            Console.WriteLine($"Mouse Action: {command.remote_control_mouse_action}, Mouse XYZ: {command.remote_control_mouse_xyz}, Keyboard Input: {command.remote_control_keyboard_input}");

            switch (command.type)
            {
                    // Ersetze den Fall "0" in ProcessCommand wie folgt:
                    case "0": // Screen Capture
                        _ = Task.Run(() => CaptureAndSendScreenshot(command));
                    break;

                case "1": // Move Mouse / Clicks
                    string[] mouseCoordinates = command.remote_control_mouse_xyz.Split(',');
                    int x = Convert.ToInt32(mouseCoordinates[0]);
                    int y = Convert.ToInt32(mouseCoordinates[1]);
                    int screenIndex = Convert.ToInt32(command.remote_control_screen_index);

                    await MouseControl.MoveMouse(x, y, screenIndex);

                    switch (command.remote_control_mouse_action)
                    {
                        case "0": // Left Click
                            await MouseControl.LeftClickMouse();
                            break;

                        case "1": // Right Click
                            await MouseControl.RightClickMouse();
                            break;

                        case "2": // Mouse Down
                            await MouseControl.LeftMouseDown();
                            break;

                        case "3": // Mouse Up
                            await MouseControl.LeftMouseUp();
                            break;

                        case "4": // Move only (already done above)
                                  // Nothing more to do here
                            break;

                        default:
                            // Optional: Log unknown action
                            break;
                    }
                    break;
                case "2": // Keyboard Input
                {
                    var input = command.remote_control_keyboard_input?.Trim();

                    if (string.IsNullOrEmpty(input))
                        break;

                    var inputLower = input.ToLowerInvariant();

                        // If shift is in the first part of the input, we assume it's a modifier key
                        bool shift = inputLower.StartsWith("shift+");

                        Console.WriteLine($"Processing keyboard input: {inputLower}, Shift: {shift}");

                        if (inputLower == "ctrl+keya")
                            KeyboardControl.SendCtrlA();
                        else if (inputLower == "ctrl+keyc")
                            KeyboardControl.SendCtrlC();
                        else if (inputLower == "ctrl+keyv")
                            KeyboardControl.SendCtrlV(command.remote_control_keyboard_content);
                        else if (inputLower == "ctrl+keyx")
                            KeyboardControl.SendCtrlX();
                        else if (inputLower == "ctrl+keyz")
                            KeyboardControl.SendCtrlZ();
                        else if (inputLower == "ctrl+keyy")
                            KeyboardControl.SendCtrlY();
                        else if (inputLower == "ctrl+keys")
                            KeyboardControl.SendCtrlS();
                        else if (inputLower == "ctrl+keyn")
                            KeyboardControl.SendCtrlN();
                        else if (inputLower == "ctrl+keyp")
                            KeyboardControl.SendCtrlP();
                        else if (inputLower == "ctrl+keyf")
                            KeyboardControl.SendCtrlF();
                        else if (inputLower == "ctrl+shift+keyt")
                            KeyboardControl.SendCtrlShiftT();
                        else if (inputLower == "ctrlaltdel")
                            KeyboardControl.SendCtrlAltDelete();
                        else if (inputLower == "ctrl+backspace")
                            KeyboardControl.SendCtrlBackspace();
                        else if (inputLower == "ctrl+arrowleft")
                            KeyboardControl.SendCtrlArrowLeft();
                        else if (inputLower == "ctrl+arrowright")
                            KeyboardControl.SendCtrlArrowRight();
                        else if (inputLower == "ctrl+arrowup")
                            KeyboardControl.SendCtrlArrowUp();
                        else if (inputLower == "ctrl+arrowdown")
                            KeyboardControl.SendCtrlArrowDown();
                        else if (inputLower == "ctrl+shift+arrowleft")
                            KeyboardControl.SendCtrlArrowLeft();
                        else if (inputLower == "ctrl+shift+arrowright")
                            KeyboardControl.SendCtrlArrowRight();
                        else if (inputLower == "ctrl+keyr")
                            KeyboardControl.SendCtrlR();
                        else
                        {
                            if (command.remote_control_keyboard_input.Length > 1)
                            {
                                if (shift)
                                    inputLower = inputLower.Replace("shift+", ""); // Remove shift from the input for ASCII mapping

                                var asciiCode = Keys.MapKeyStringToAscii(inputLower);
                                if (asciiCode.HasValue)
                                {
                                    await KeyboardControl.SendKey(asciiCode.Value, shift);
                                }
                                else
                                {
                                    Console.WriteLine($"Unknown keyboard input: {input}");
                                }
                            }
                            else
                            {
                                var sim1 = new InputSimulator();
                                sim1.Keyboard.TextEntry(command.remote_control_keyboard_input);
                            }
                        }
                        break;
                }
                case "3":
                    int screen_indexes = 0;
                    if (OperatingSystem.IsWindows())
                        screen_indexes = ScreenCapture.Get_Screen_Indexes();
                    else if (OperatingSystem.IsMacOS())
                        screen_indexes = 0;

                    await Local_Server_Send_Message($"screen_indexes${command.response_id}${screen_indexes}");

                    break;
                case "6": // Get clipboard from user
                    KeyboardControl.SendCtrlC();

                    await Task.Delay(200); // Wait for clipboard to update

                    string clipboardContent = User32.GetClipboardText();
                    //Logging.Handler.Debug($"Clipboard content: {clipboardContent}", "", "");

                    await Local_Server_Send_Message($"clipboard_content${command.response_id}$clipboard_content%{clipboardContent}");
                    break;
                case "7": // Send text

                    Console.WriteLine($"Sending text: {command.remote_control_keyboard_input}");

                    var sim = new InputSimulator();
                    sim.Keyboard.TextEntry(command.remote_control_keyboard_input);

                    break;
                default:
                    Console.WriteLine("Unknown command type.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to process command: {ex.Message}");
        }
    }

    public async Task Local_Server_Send_Message(string message)
    {
        try
        {
            if (_stream != null && _client.Connected)
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

    // New method to send binary messages (e.g., image data)
    public async Task Local_Server_Send_Binary_Message(string messageType, string responseId, byte[] data)
    {
        try
        {
            if (_stream != null && _client.Connected)
            {
                // Create header with message type, response ID, and data length
                string header = $"{messageType}${responseId}${data.Length}$";
                byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                
                // Write header first
                await _stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                
                // Write binary data directly
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
                
                Console.WriteLine($"Sent binary message ({messageType}) with {data.Length} bytes to remote agent");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send binary message to the local server: {ex.Message}");
        }
    }

    private async Task MonitorConnectionAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Check if the client is still connected
            if (_client == null || !_client.Connected)
            {
                if (!_isConnecting)
                {
                    _isConnecting = true;
                    Console.WriteLine("Disconnected from the server. Attempting to reconnect...");

                    // Try to reconnect
                    while ((_client == null || !_client.Connected) && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            // Clean up existing connection
                            _stream?.Close();
                            _client?.Close();

                            _client = new TcpClient();
                            await _client.ConnectAsync("127.0.0.1", 7338);
                            _stream = _client.GetStream();

                            // Send username to identify the user
                            string username = Environment.UserName;
                            await Local_Server_Send_Message($"username${username}");
                            Console.WriteLine("Connected to the local server.");

                            // Start listening for messages (only if not already running)
                            if (_messageHandlerTask == null || _messageHandlerTask.IsCompleted)
                            {
                                _messageHandlerTask = Local_Server_Handle_Server_Messages(cancellationToken);
                            }
                            
                            _isConnecting = false;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Reconnect attempt failed: {ex.Message}");
                            await Task.Delay(5000, cancellationToken); // Wait before retrying
                        }
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

    private async Task CaptureAndSendScreenshot(Command command)
    {
        try
        {
            byte[] imageBytes = null;
            if (OperatingSystem.IsWindows())
                imageBytes = await ScreenCapture.CaptureScreenToBytes(Convert.ToInt32(command.remote_control_screen_index));
            // else if (OperatingSystem.IsMacOS()) ...

            if (imageBytes != null && imageBytes.Length > 0)
            {
                await Local_Server_Send_Binary_Message("screen_capture", command.response_id, imageBytes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Screenshot-Fehler: {ex.Message}");
        }
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Starting User Process...");

            Dpi.SetProcessDpiAwareness(Dpi.ProcessDpiAwareness.Process_Per_Monitor_DPI_Aware);

            /*IntPtr handle = User32.LoadLibrary("sas.dll");
            if (handle == IntPtr.Zero)
            {
                Console.WriteLine("Failed to load sas.dll. Ensure it is present in the application directory.");
                return;
            }*/
            //Console.WriteLine("sas.dll loaded successfully.");

            var client = new UserClient();
            await client.Local_Server_Connect();

            // Keep the application running until termination is requested.
            await Task.Delay(-1); // Block forever (or until you quit the app)
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}