using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Helper;

class UserClient
{
    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public class Command
    {
        public string response_id { get; set; }
        public string type { get; set; }
        public string remote_control_screen_index { get; set; }
        public string remote_control_mouse_action { get; set; }
        public string remote_control_mouse_xyz { get; set; }
        public string remote_control_keyboard_input { get; set; }
    }

    public async Task Local_Server_Connect()
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync("127.0.0.1", 7338);
            _stream = _client.GetStream();
            // Send username to identify the user
            string username = Environment.UserName;
            await Local_Server_Send_Message($"username${username}");

            // Start listening for messages from the server
            _ = Local_Server_Handle_Server_Messages(_cancellationTokenSource.Token);
            Console.WriteLine("Connected to the local server.");

            // Start checking the connection status in a separate task
            _ = MonitorConnectionAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to the local server: {ex.Message}");
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
            switch (command.type)
            {
                case "0": // Screen Capture
                    string base64Image = await ScreenCapture.CaptureScreenToBase64(Convert.ToInt32(command.remote_control_screen_index));
                    await Local_Server_Send_Message($"screen_capture${command.response_id}${base64Image}");
                    break;

                case "1": // Move Mouse
                    string[] mouseCoordinates = command.remote_control_mouse_xyz.Split(',');
                    int x = Convert.ToInt32(mouseCoordinates[0]);
                    int y = Convert.ToInt32(mouseCoordinates[1]);
                    await MouseControl.MoveMouse(x, y, Convert.ToInt32(command.remote_control_screen_index));

                    if (command.remote_control_mouse_action == "0") // Left Click
                        await MouseControl.LeftClickMouse();
                    else if (command.remote_control_mouse_action == "1") // Right Click
                        await MouseControl.RightClickMouse();
                    break;

                case "2": // Keyboard Input
                    if (command.remote_control_keyboard_input == "Ctrl+A")
                        KeyboardControl.SendCtrlA();
                    else if (command.remote_control_keyboard_input == "Ctrl+C")
                        KeyboardControl.SendCtrlC();
                    else if (command.remote_control_keyboard_input == "Ctrl+V")
                        KeyboardControl.SendCtrlV();
                    else if (command.remote_control_keyboard_input == "Ctrl+X")
                        KeyboardControl.SendCtrlX();
                    else if (command.remote_control_keyboard_input == "Ctrl+Z")
                        KeyboardControl.SendCtrlZ();
                    else if (command.remote_control_keyboard_input == "Ctrl+Y")
                        KeyboardControl.SendCtrlY();
                    else
                    {
                        byte asciiCode = Convert.ToByte(command.remote_control_keyboard_input);
                        await KeyboardControl.SendKey(asciiCode); // Send the ASCII code of the key
                    }
                    break;
                case "3":
                    int screen_indexes = ScreenCapture.Get_Screen_Indexes();
                    await Local_Server_Send_Message($"screen_indexes${command.response_id}${screen_indexes}");
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

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Starting User Process...");

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
