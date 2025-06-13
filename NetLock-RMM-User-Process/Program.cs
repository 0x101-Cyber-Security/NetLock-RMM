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
            Console.WriteLine($"Processing command: {command.type} with response ID: {command.response_id}");
            Console.WriteLine($"Mouse Action: {command.remote_control_mouse_action}, Mouse XYZ: {command.remote_control_mouse_xyz}, Keyboard Input: {command.remote_control_keyboard_input}");

            switch (command.type)
            {
                case "0": // Screen Capture
                    string base64Image = String.Empty;
                    
                    if (OperatingSystem.IsWindows())
                        base64Image = await ScreenCapture.CaptureScreenToBase64(Convert.ToInt32(command.remote_control_screen_index));
                    /*else if (OperatingSystem.IsMacOS())
                        base64Image = await ScreenCaptureMacOS.CaptureScreenToBase64(Convert.ToInt32(command.remote_control_screen_index));
                    */
                    await Local_Server_Send_Message($"screen_capture${command.response_id}${base64Image}");
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

                        if (inputLower == "ctrl+keya")
                            KeyboardControl.SendCtrlA();
                        else if (inputLower == "ctrl+keyc")
                            KeyboardControl.SendCtrlC();
                        else if (inputLower == "ctrl+keyv")
                            KeyboardControl.SendCtrlV();
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
                        else
                        {
                        var asciiCode = MapKeyStringToAscii(inputLower);
                        if (asciiCode.HasValue)
                        {
                            await KeyboardControl.SendKey(asciiCode.Value);
                        }
                        else
                        {
                            Console.WriteLine($"Unknown keyboard input: {input}");
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

    private byte? MapKeyStringToAscii(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            key = key.Trim().ToLowerInvariant();

            return key switch
            {
                // Buchstaben
                "keya" => KeyCodes.VK_A,
                "keyb" => KeyCodes.VK_B,
                "keyc" => KeyCodes.VK_C,
                "keyd" => KeyCodes.VK_D,
                "keye" => KeyCodes.VK_E,
                "keyf" => KeyCodes.VK_F,
                "keyg" => KeyCodes.VK_G,
                "keyh" => KeyCodes.VK_H,
                "keyi" => KeyCodes.VK_I,
                "keyj" => KeyCodes.VK_J,
                "keyk" => KeyCodes.VK_K,
                "keyl" => KeyCodes.VK_L,
                "keym" => KeyCodes.VK_M,
                "keyn" => KeyCodes.VK_N,
                "keyo" => KeyCodes.VK_O,
                "keyp" => KeyCodes.VK_P,
                "keyq" => KeyCodes.VK_Q,
                "keyr" => KeyCodes.VK_R,
                "keys" => KeyCodes.VK_S,
                "keyt" => KeyCodes.VK_T,
                "keyu" => KeyCodes.VK_U,
                "keyv" => KeyCodes.VK_V,
                "keyw" => KeyCodes.VK_W,
                "keyx" => KeyCodes.VK_X,
                "keyy" => KeyCodes.VK_Y,
                "keyz" => KeyCodes.VK_Z,

                // Zahlen (Top Row)
                "digit0" => KeyCodes.VK_0,
                "digit1" => KeyCodes.VK_1,
                "digit2" => KeyCodes.VK_2,
                "digit3" => KeyCodes.VK_3,
                "digit4" => KeyCodes.VK_4,
                "digit5" => KeyCodes.VK_5,
                "digit6" => KeyCodes.VK_6,
                "digit7" => KeyCodes.VK_7,
                "digit8" => KeyCodes.VK_8,
                "digit9" => KeyCodes.VK_9,

                // Steuerungstasten
                "controlleft" => KeyCodes.VK_CONTROL,
                "controlright" => KeyCodes.VK_CONTROL,
                "shiftleft" => KeyCodes.VK_SHIFT,
                "shiftright" => KeyCodes.VK_SHIFT,
                "altleft" => KeyCodes.VK_ALT,
                "altright" => KeyCodes.VK_ALT,
                "metaleft" => KeyCodes.VK_META,
                "metaright" => KeyCodes.VK_META,
                "enter" => KeyCodes.VK_ENTER,
                "escape" => KeyCodes.VK_ESCAPE,
                "space" => KeyCodes.VK_SPACE,
                "tab" => KeyCodes.VK_TAB,
                "backspace" => KeyCodes.VK_BACK,
                "insert" => KeyCodes.VK_INSERT,
                "delete" => KeyCodes.VK_DELETE,
                "home" => KeyCodes.VK_HOME,
                "end" => KeyCodes.VK_END,
                "pageup" => KeyCodes.VK_PRIOR,
                "pagedown" => KeyCodes.VK_NEXT,

                // Pfeiltasten
                "arrowleft" => KeyCodes.VK_LEFT,
                "arrowup" => KeyCodes.VK_UP,
                "arrowright" => KeyCodes.VK_RIGHT,
                "arrowdown" => KeyCodes.VK_DOWN,

                // Sonderzeichen
                "minus" => KeyCodes.VK_MINUS,
                "equal" => KeyCodes.VK_EQUALS,
                "bracketleft" => KeyCodes.VK_LBRACKET,
                "bracketright" => KeyCodes.VK_RBRACKET,
                "backslash" => KeyCodes.VK_BACKSLASH,
                "semicolon" => KeyCodes.VK_SEMICOLON,
                "quote" => KeyCodes.VK_APOSTROPHE,
                "comma" => KeyCodes.VK_COMMA,
                "period" => KeyCodes.VK_PERIOD,
                "slash" => KeyCodes.VK_SLASH,
                "backquote" => KeyCodes.VK_GRAVE,

                // Funktionstasten
                "f1" => KeyCodes.VK_F1,
                "f2" => KeyCodes.VK_F2,
                "f3" => KeyCodes.VK_F3,
                "f4" => KeyCodes.VK_F4,
                "f5" => KeyCodes.VK_F5,
                "f6" => KeyCodes.VK_F6,
                "f7" => KeyCodes.VK_F7,
                "f8" => KeyCodes.VK_F8,
                "f9" => KeyCodes.VK_F9,
                "f10" => KeyCodes.VK_F10,
                "f11" => KeyCodes.VK_F11,
                "f12" => KeyCodes.VK_F12,

                // NumPad Tasten
                "numpad0" => KeyCodes.VK_NUMPAD0,
                "numpad1" => KeyCodes.VK_NUMPAD1,
                "numpad2" => KeyCodes.VK_NUMPAD2,
                "numpad3" => KeyCodes.VK_NUMPAD3,
                "numpad4" => KeyCodes.VK_NUMPAD4,
                "numpad5" => KeyCodes.VK_NUMPAD5,
                "numpad6" => KeyCodes.VK_NUMPAD6,
                "numpad7" => KeyCodes.VK_NUMPAD7,
                "numpad8" => KeyCodes.VK_NUMPAD8,
                "numpad9" => KeyCodes.VK_NUMPAD9,
                "numpadmultiply" => KeyCodes.VK_MULTIPLY,
                "numpadadd" => KeyCodes.VK_ADD,
                "numpadsubtract" => KeyCodes.VK_SUBTRACT,
                "numpaddecimal" => KeyCodes.VK_DECIMAL,
                "numpaddivide" => KeyCodes.VK_DIVIDE,

                // Zusätzliche Tasten
                "capslock" => KeyCodes.VK_CAPITAL,
                "scrolllock" => KeyCodes.VK_SCROLL,
                "numlock" => KeyCodes.VK_NUMLOCK,
                "pause" => KeyCodes.VK_PAUSE,
                "printscreen" => KeyCodes.VK_SNAPSHOT,
                "contextmenu" => KeyCodes.VK_APPS,

                _ => null,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error mapping key string to ASCII: {ex.Message}");
            return null;
        }
    }

    public static class KeyCodes
    {
        // Letters
        public const byte VK_A = 0x41;
        public const byte VK_B = 0x42;
        public const byte VK_C = 0x43;
        public const byte VK_D = 0x44;
        public const byte VK_E = 0x45;
        public const byte VK_F = 0x46;
        public const byte VK_G = 0x47;
        public const byte VK_H = 0x48;
        public const byte VK_I = 0x49;
        public const byte VK_J = 0x4A;
        public const byte VK_K = 0x4B;
        public const byte VK_L = 0x4C;
        public const byte VK_M = 0x4D;
        public const byte VK_N = 0x4E;
        public const byte VK_O = 0x4F;
        public const byte VK_P = 0x50;
        public const byte VK_Q = 0x51;
        public const byte VK_R = 0x52;
        public const byte VK_S = 0x53;
        public const byte VK_T = 0x54;
        public const byte VK_U = 0x55;
        public const byte VK_V = 0x56;
        public const byte VK_W = 0x57;
        public const byte VK_X = 0x58;
        public const byte VK_Y = 0x59;
        public const byte VK_Z = 0x5A;

        // Numbers
        public const byte VK_0 = 0x30;
        public const byte VK_1 = 0x31;
        public const byte VK_2 = 0x32;
        public const byte VK_3 = 0x33;
        public const byte VK_4 = 0x34;
        public const byte VK_5 = 0x35;
        public const byte VK_6 = 0x36;
        public const byte VK_7 = 0x37;
        public const byte VK_8 = 0x38;
        public const byte VK_9 = 0x39;

        // Control buttons
        public const byte VK_CONTROL = 0x11;
        public const byte VK_SHIFT = 0x10;
        public const byte VK_ALT = 0x12;
        public const byte VK_META = 0x5B; // Windows / Command-Taste
        public const byte VK_ENTER = 0x0D;
        public const byte VK_ESCAPE = 0x1B;
        public const byte VK_SPACE = 0x20;
        public const byte VK_TAB = 0x09;
        public const byte VK_BACK = 0x08; // Backspace
        public const byte VK_INSERT = 0x2D;
        public const byte VK_DELETE = 0x2E;
        public const byte VK_HOME = 0x24;
        public const byte VK_END = 0x23;
        public const byte VK_PRIOR = 0x21; // Page Up
        public const byte VK_NEXT = 0x22;  // Page Down

        // Arrow keys
        public const byte VK_LEFT = 0x25;
        public const byte VK_UP = 0x26;
        public const byte VK_RIGHT = 0x27;
        public const byte VK_DOWN = 0x28;

        // Special characters
        public const byte VK_MINUS = 0xBD;      // -
        public const byte VK_EQUALS = 0xBB;     // =
        public const byte VK_LBRACKET = 0xDB;   // [
        public const byte VK_RBRACKET = 0xDD;   // ]
        public const byte VK_BACKSLASH = 0xDC;  // \
        public const byte VK_SEMICOLON = 0xBA;  // ;
        public const byte VK_APOSTROPHE = 0xDE; // '
        public const byte VK_COMMA = 0xBC;      // ,
        public const byte VK_PERIOD = 0xBE;     // .
        public const byte VK_SLASH = 0xBF;      // /
        public const byte VK_GRAVE = 0xC0;      // `

        // Function keys
        public const byte VK_F1 = 0x70;
        public const byte VK_F2 = 0x71;
        public const byte VK_F3 = 0x72;
        public const byte VK_F4 = 0x73;
        public const byte VK_F5 = 0x74;
        public const byte VK_F6 = 0x75;
        public const byte VK_F7 = 0x76;
        public const byte VK_F8 = 0x77;
        public const byte VK_F9 = 0x78;
        public const byte VK_F10 = 0x79;
        public const byte VK_F11 = 0x7A;
        public const byte VK_F12 = 0x7B;

        // NumPad buttons
        public const byte VK_NUMPAD0 = 0x60;
        public const byte VK_NUMPAD1 = 0x61;
        public const byte VK_NUMPAD2 = 0x62;
        public const byte VK_NUMPAD3 = 0x63;
        public const byte VK_NUMPAD4 = 0x64;
        public const byte VK_NUMPAD5 = 0x65;
        public const byte VK_NUMPAD6 = 0x66;
        public const byte VK_NUMPAD7 = 0x67;
        public const byte VK_NUMPAD8 = 0x68;
        public const byte VK_NUMPAD9 = 0x69;
        public const byte VK_MULTIPLY = 0x6A;
        public const byte VK_ADD = 0x6B;
        public const byte VK_SUBTRACT = 0x6D;
        public const byte VK_DECIMAL = 0x6E;
        public const byte VK_DIVIDE = 0x6F;

        // Additional buttons
        public const byte VK_CAPITAL = 0x14; // Caps Lock
        public const byte VK_SCROLL = 0x91;  // Scroll Lock
        public const byte VK_NUMLOCK = 0x90; // Num Lock
        public const byte VK_PAUSE = 0x13;   // Pause
        public const byte VK_SNAPSHOT = 0x2C; // Print Screen
        public const byte VK_APPS = 0x5D;    // Context menu key (Applications key)
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
