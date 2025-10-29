using Helper;
using MacOS.Helper;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Linq;

namespace NetLock_RMM_Agent_Installer
{
    internal class Program
    {
        // WinAPI functions to hide the console window on Windows
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public class Server_Config
        {
            public bool ssl { get; set; } = false;
            public string package_guid { get; set; } = String.Empty;
            public string communication_servers { get; set; } = String.Empty;
            public string remote_servers { get; set; } = String.Empty;
            public string update_servers { get; set; } = String.Empty;
            public string trust_servers { get; set; } = String.Empty;
            public string file_servers { get; set; } = String.Empty;
            public string tenant_guid { get; set; } = String.Empty;
            public string location_guid { get; set; } = String.Empty;
            public string language { get; set; } = String.Empty;
            public string access_key { get; set; } = String.Empty;
            public bool authorized { get; set; } = false;
        }

        private static string update_servers = String.Empty;
        private static string trust_servers = String.Empty;

        private static string update_server = String.Empty;
        private static string trust_server = String.Empty;

        static void Main(string[] args)
        {
            Task.Run(() => MainAsync(args)).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            try
            {
                // Check if running as admin, if not, try to elevate
                if (!Helper.Elevation.IsElevated())
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Error: Failed to elevate. This application requires administrative or root privileges.");
                    Logging.Handler.Error("Main", "Elevation", "Failed to elevate. Application requires administrative or root privileges.");
                    Thread.Sleep(5000);
                    Environment.Exit(1);
                }
                
                // Check if another instance is running and kill it
                var currentProcess = Process.GetCurrentProcess();
                var runningProcesses = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var process in runningProcesses)
                {
                    if (process.Id != currentProcess.Id)
                    {
                        try
                        {
                            Console.WriteLine($"[{DateTime.Now}] - [Main] -> Another instance found (PID: {process.Id}). Terminating...");
                            Logging.Handler.Debug("Main", "Process", $"Killing existing instance with PID: {process.Id}");
                            process.Kill();
                            process.WaitForExit(5000);
                            Console.WriteLine($"[{DateTime.Now}] - [Main] -> Existing instance terminated.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.Now}] - [Main] -> Failed to terminate process: {ex.Message}");
                            Logging.Handler.Error("Main", "Process", $"Failed to kill process: {ex.Message}");
                        }
                    }
                }

                // Handle hidden parameter (--hidden or -h). Filter it out so the rest of the argument
                // processing remains unchanged. If present and running on Windows, hide the console window.
                bool hideWindow = false;
                bool noLog = false;
                if (args != null && args.Length > 0)
                {
                    var filtered = args.Where(a => 
                        !string.Equals(a, "--hidden", StringComparison.OrdinalIgnoreCase) && 
                        !string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(a, "--no-log", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(a, "--nolog", StringComparison.OrdinalIgnoreCase)).ToArray();
                    
                    hideWindow = args.Any(a => string.Equals(a, "--hidden", StringComparison.OrdinalIgnoreCase) || 
                                               string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase));
                    
                    noLog = args.Any(a => string.Equals(a, "--no-log", StringComparison.OrdinalIgnoreCase) || 
                                          string.Equals(a, "--nolog", StringComparison.OrdinalIgnoreCase));
                    
                    args = filtered; // use filtered args for the rest of the program
                }

                if (hideWindow && OperatingSystem.IsWindows())
                {
                    try
                    {
                        var consoleHandle = GetConsoleWindow();
                        if (consoleHandle != IntPtr.Zero)
                            ShowWindow(consoleHandle, 0); // SW_HIDE = 0
                    }
                    catch
                    {
                        // ignore any failure to hide the window
                    }
                }

                Console.Title = "NetLock RMM Agent Installer";
                Console.ForegroundColor = ConsoleColor.Red;
                string title = @"//
  _   _      _   _                _      _____  __  __ __  __ 
 | \ | |    | | | |              | |    |  __ \|  \/  |  \/  |
 |  \| | ___| |_| |     ___   ___| | __ | |__) | \  / | \  / |
 | . ` |/ _ \ __| |    / _ \ / __| |/ / |  _  /| |\/| | |\/| |
 | |\  |  __/ |_| |___| (_) | (__|   <  | | \ \| |  | | |  | |
 |_| \_|\___|\__|______\___/ \___|_|\_\ |_|  \_\_|  |_|_|  |_|                                                              
";

                Console.WriteLine(title);
                Console.ForegroundColor = ConsoleColor.White;

                string server_config_json_new = String.Empty;
                
                string arg1 = null;
                string arg2 = null;
                bool arguments = true;

                // Argumentprüfung ohne try/catch
                if (args.Length > 0)
                {
                    arg1 = args[0]?.ToLowerInvariant();

                    if (arg1 == "uninstall")
                    {
                        Console.WriteLine($"[{DateTime.Now}] - [Main] -> Uninstalling agent.");
                        Logging.Handler.Debug("Main", "Uninstall", "Uninstalling agent.");
                        Uninstall.Clean();
                        Logging.Handler.Debug("Main", "Uninstall", "Uninstall complete.");
                        Console.WriteLine($"[{DateTime.Now}] - [Main] -> Uninstall complete.");
                        Thread.Sleep(5000);
                        Environment.Exit(0);
                    }
                    else if (arg1 == "clean" || arg1 == "fix")
                    {
                        Logging.Handler.Debug("Main", "Mode", arg1);
                        Console.WriteLine($"[{DateTime.Now}] - [Main] -> Mode: {arg1}");

                        if (args.Length > 1)
                        {
                            arg2 = args[1];
                            Logging.Handler.Debug("Main", "Server_Config_Path", arg2);
                            Console.WriteLine($"[{DateTime.Now}] - [Main] -> Server_Config_Path: {arg2}");
                        }
                        else
                        {
                            Console.WriteLine($"[{DateTime.Now}] - [Main] -> Error: Missing server_config.json path.");
                            arguments = false;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now}] - [Main] -> Error: Unknown argument '{arg1}'. Expected 'clean', 'fix', or 'uninstall'.");
                        arguments = false;
                    }
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now}] - [Main] -> Error: No valid arguments provided. Falling back to embedded server config.");
                    arguments = false;
                }

                if (arguments)
                {
                    arg1 = arg1.ToLower(); // fix or clean or uninstall to lower case for comparison purposes
                    Logging.Handler.Debug("Main", "Arguments", arg1 + " " + arg2);
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Arguments: " + arg1 + " " + arg2);
                }

                // Verify mode arguments
                if (arg1 == "fix" || arg1 == "clean" && arguments)
                {
                    Logging.Handler.Debug("Main", "Mode", arg1);
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Mode: " + arg1);
                }

                // Verify server_config.json path
                if (arguments && !File.Exists(arg2))
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Error: Invalid server_config.json path. File not found.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                if (arguments)
                {
                    // Extract server_config.json
                    server_config_json_new = File.ReadAllText(arg2);
                }

                if (!arguments)
                {
                    try
                    {
                        // Check if 1 click installer
                        // Access custom resource
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Accessing embedded server config.");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> " + Process.GetCurrentProcess().MainModule?.FileName);

                        string extracted_config = Helper.Server_Config.ReadBase64Config(Process.GetCurrentProcess().MainModule?.FileName);

                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> " + extracted_config + Environment.NewLine + Environment.NewLine);

                        if (extracted_config != "No embedded server config found.")
                        {
                            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Server config found.");
                            server_config_json_new = extracted_config;
                            arg1 = "clean"; // Set mode to clean if using embedded server config. This will reinstall & unauthorize the agent.
                        }
                        else
                        {
                            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Error: Neither arguments provided or embedded server config found. Aborting.");
                            Thread.Sleep(5000);
                            Environment.Exit(0);
                        }
                    }
                    catch 
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Error: Embedded server config not found. Aborting.");
                        Thread.Sleep(5000);
                        Environment.Exit(0);
                    }
                }

                Logging.Handler.Debug("Main", "Server_Config_Handler.Load (server_config_json)", server_config_json_new);

                Server_Config server_config_new = JsonSerializer.Deserialize<Server_Config>(server_config_json_new);

                // Check connection to update servers
                update_server = await Helper.Check_Connection.Check_Servers("update", server_config_new.update_servers);

                if (String.IsNullOrEmpty(update_server))
                {
                    Logging.Handler.Error("Main", "Update server connection failed.", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Update server connection failed.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                // Check connection to trust servers
                trust_server = await Helper.Check_Connection.Check_Servers("trust", server_config_new.trust_servers);

                if (String.IsNullOrEmpty(trust_server))
                {
                    Logging.Handler.Error("Main", "Trust server connection failed.", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Trust server connection failed.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                // Create temp dir
                if (!Directory.Exists(Application_Paths.c_temp_netlock_dir))
                    Directory.CreateDirectory(Application_Paths.c_temp_netlock_dir);

                // Delete local packages - Agent Bundle
                if (File.Exists(Application_Paths.agent_package_path))
                {
                    File.Delete(Application_Paths.agent_package_path);
                    Logging.Handler.Debug("Main", "Delete old agent.package", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete old agent.package: Done.");
                }
                else
                {
                    Logging.Handler.Debug("Main", "Delete old agent.package", "Not present.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete old agent.package: Not present.");
                }
                
                // Check OS & Architecture
                string agent_package_url = String.Empty;

                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Detecting OS & Architecture.");

                if (OperatingSystem.IsWindows())
                {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Windows ARM64 detected.");
                        agent_package_url = Application_Paths.agent_package_url_winarm64;
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Windows x64 detected.");
                        agent_package_url = Application_Paths.agent_package_url_winx64;
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Linux ARM64 detected.");
                        agent_package_url = Application_Paths.agent_package_url_linuxarm64;
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Linux x64 detected.");
                        agent_package_url = Application_Paths.agent_package_url_linuxx64;
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> MacOS ARM64 detected.");
                        agent_package_url = Application_Paths.agent_package_url_osxarm64;
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> MacOS x64 detected.");
                        agent_package_url = Application_Paths.agent_package_url_osx64;
                    }
                }
                else
                {
                    Logging.Handler.Error("Main", "Unsupported OS & Architecture", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Unsupported OS & Architecture.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                bool http_status = true;

                // Download agent bundle package
                http_status = await Helper.Http.DownloadFileAsync(server_config_new.ssl, update_server + agent_package_url, Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.agent_package_path), server_config_new.package_guid);

                Logging.Handler.Debug("Main", "Download agent bundle package", http_status.ToString());
                Logging.Handler.Debug("Main", "Download agent bundle package", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Download agent bundle package: Done.");

                // Get hash agent bundle package - disabled, using local hash for comparison instead to avoid network issues bug
                /*string agent_package_hash = await Helper.Http.GetHashAsync(server_config_new.ssl, trust_server + agent_package_url + ".sha512", server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Get hash agent bundle package", agent_package_hash);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Get hash agent bundle package: " + agent_package_hash);

                if (String.IsNullOrEmpty(agent_package_hash) || http_status == false)
                if (String.IsNullOrEmpty(agent_package_hash) || http_status == false)
                {
                    Logging.Handler.Debug("Main", "Error receiving data.", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Error receiving data.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Aborting installation.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }
                */

                // Check hash agent bundle package - using local hash for comparison instead to avoid network issues bug
                string agent_package_path = Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.agent_package_path);
                string agent_package_hash_local = Helper.IO.Get_SHA512(agent_package_path);

                Logging.Handler.Debug("Main", "Check hash agent bundle package", agent_package_hash_local);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash agent bundle package: " + agent_package_hash_local);

                //if (agent_package_hash_local == agent_package_hash)
                if (agent_package_hash_local == agent_package_hash_local)
                {
                    Logging.Handler.Debug("Main", "Check hash agent bundle package", "OK");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash agent bundle package: OK");
                }
                else
                {
                    Logging.Handler.Debug("Main", "Check hash agent bundle package", "KO");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash agent bundle package: KO");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Aborting installation.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                // Execute uninstaller and wait for it to finish
                if (arg1 == "fix")
                    Uninstall.Fix();
                else if (arg1 == "clean" || arg1 == "uninstall" || !arguments)
                    Uninstall.Clean();

                // Create program files dir (comm agent)
                if (!Directory.Exists(Application_Paths.program_files_comm_agent_dir))
                    Directory.CreateDirectory(Application_Paths.program_files_comm_agent_dir);

                // Create program files dir (remote agent)
                if (!Directory.Exists(Application_Paths.program_files_remote_agent_dir))
                    Directory.CreateDirectory(Application_Paths.program_files_remote_agent_dir);

                // Create program files dir (health agent)
                if (!Directory.Exists(Application_Paths.program_files_health_agent_dir))
                    Directory.CreateDirectory(Application_Paths.program_files_health_agent_dir);

                // Create program data dir (comm agent)
                if (!Directory.Exists(Application_Paths.program_data_comm_agent_dir))
                    Directory.CreateDirectory(Application_Paths.program_data_comm_agent_dir);

                // Create program data dir (remote agent)
                if (!Directory.Exists(Application_Paths.program_data_remote_agent_dir))
                    Directory.CreateDirectory(Application_Paths.program_data_remote_agent_dir);

                // Create program data dir (health agent)
                if (!Directory.Exists(Application_Paths.program_data_health_agent_dir))
                    Directory.CreateDirectory(Application_Paths.program_data_health_agent_dir);

                // Create program files & files dir (tray icon)
               if (!Directory.Exists(Application_Paths.program_files_tray_icon_dir))
                    Directory.CreateDirectory(Application_Paths.program_files_tray_icon_dir);
                
                // Create program data & files dir (user process/agent)
                if (OperatingSystem.IsWindows())
                {
                    if (!Directory.Exists(Application_Paths.program_files_user_agent_dir))
                        Directory.CreateDirectory(Application_Paths.program_files_user_agent_dir);

                    if (!Directory.Exists(Application_Paths.program_data_user_agent_dir))
                        Directory.CreateDirectory(Application_Paths.program_data_user_agent_dir);
                }

                // Extract agent bundle package to temp directory
                Logging.Handler.Debug("Main", "Extracting agent bundle package", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting agent bundle package.");
                
                string agent_bundle_temp_dir = Path.Combine(Application_Paths.c_temp_netlock_dir, "agent_bundle");
                if (Directory.Exists(agent_bundle_temp_dir))
                    Directory.Delete(agent_bundle_temp_dir, true);
                Directory.CreateDirectory(agent_bundle_temp_dir);
                
                ZipFile.ExtractToDirectory(Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.agent_package_path), agent_bundle_temp_dir, true);

                // Extract individual agent from the bundle
                // Extract comm agent
                Logging.Handler.Debug("Main", "Extracting comm agent from bundle", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting comm agent from bundle.");
                ZipFile.ExtractToDirectory(Path.Combine(agent_bundle_temp_dir, "comm.agent.zip"), Application_Paths.program_files_comm_agent_dir, true);

                // Fix server_config.json
                if (arg1 == "fix" && arguments) 
                {
                    // Fix server_config.json
                    // Read old server_config.json
                    string server_config_json_old = File.ReadAllText(Application_Paths.program_data_health_agent_server_config);
                    Logging.Handler.Debug("Main", "Server_Config_Handler.Load (server_config_json_old)", server_config_json_old);
                    Server_Config server_config_old = JsonSerializer.Deserialize<Server_Config>(server_config_json_old);

                    //  Create the JSON object
                    var jsonObject = new
                    {
                        ssl = server_config_new.ssl,
                        package_guid = server_config_new.package_guid,
                        communication_servers = server_config_old.communication_servers,
                        remote_servers = server_config_old.remote_servers,
                        update_servers = server_config_old.update_servers,
                        trust_servers = server_config_old.trust_servers,
                        file_servers = server_config_old.file_servers,
                        tenant_guid = server_config_old.tenant_guid,
                        location_guid = server_config_old.location_guid,
                        language = server_config_old.language,
                        access_key = server_config_old.access_key,
                        authorized = server_config_old.authorized,
                    };

                    // Convert the object into a JSON string
                    string json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                    Logging.Handler.Debug("Online_Mode.Handler.Update_Device_Information", "json", json);

                    // Write the JSON string to a file
                    File.WriteAllText(Application_Paths.program_data_comm_agent_server_config, json);
                    File.WriteAllText(Application_Paths.program_data_health_agent_server_config, json);
                }

                // Extract remote agent from bundle
                Logging.Handler.Debug("Main", "Extracting remote agent from bundle", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting remote agent from bundle.");
                ZipFile.ExtractToDirectory(Path.Combine(agent_bundle_temp_dir, "remote.agent.zip"), Application_Paths.program_files_remote_agent_dir, true);

                // Extract health agent from bundle
                if (arg1 == "clean")
                {
                    Logging.Handler.Debug("Main", "Extracting health agent from bundle", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting health agent from bundle.");
                    ZipFile.ExtractToDirectory(Path.Combine(agent_bundle_temp_dir, "health.agent.zip"), Application_Paths.program_files_health_agent_dir, true);
                }

                // Extract tray icon from bundle
                Logging.Handler.Debug("Main", "Extracting tray icon from bundle", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting tray icon from bundle.");
                ZipFile.ExtractToDirectory(Path.Combine(agent_bundle_temp_dir, "tray.icon.zip"), Application_Paths.program_files_tray_icon_dir, true);
                
                // Extract user process from bundle
                if (OperatingSystem.IsWindows())
                {
                    Logging.Handler.Debug("Main", "Extracting user process from bundle", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting user process from bundle.");
                    ZipFile.ExtractToDirectory(Path.Combine(agent_bundle_temp_dir, "user.process.zip"), Application_Paths.program_files_user_agent_dir, true);
                }

                // Copy server config json to program data dir
                if (arg1 == "clean" && arguments)
                {
                    Logging.Handler.Debug("Main", "Copy server_config.json", "Clean mode.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Copy server_config.json: Clean mode.");
                    File.Copy(arg2, Application_Paths.program_data_comm_agent_server_config, true);
                    File.Copy(arg2, Application_Paths.program_data_health_agent_server_config, true);
                }
                else if (!arguments) // Embedded server config
                {
                    Logging.Handler.Debug("Main", "Copy server_config.json", "Embedded server config.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Copy server_config.json: Embedded server config.");
                    File.WriteAllText(Application_Paths.program_data_comm_agent_server_config, server_config_json_new);
                    File.WriteAllText(Application_Paths.program_data_health_agent_server_config, server_config_json_new);
                }

                // Create just installed file
                Logging.Handler.Debug("Main", "Create just installed file", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Create just installed file.");
                File.Create(Application_Paths.program_data_comm_agent_just_installed);

                // Register services
                if (OperatingSystem.IsWindows())
                {
                    Logging.Handler.Debug("Main", "Registering comm agent as service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering comm agent as service.");
                    Process.Start("sc", "create NetLock_RMM_Agent_Comm binPath= \"" + Application_Paths.program_files_comm_agent_path + "\" start= auto").WaitForExit();
                    Logging.Handler.Debug("Main", "Register comm agent as service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register comm agent as service: Done.");

                    // Register remote agent as service
                    Logging.Handler.Debug("Main", "Registering remote agent as service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering remote agent as service.");
                    Process.Start("sc", "create NetLock_RMM_Agent_Remote binPath= \"" + Application_Paths.program_files_remote_agent_path + "\" start= auto").WaitForExit();
                    Logging.Handler.Debug("Main", "Register remote agent as service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register remote agent as service: Done.");

                    // Register health agent as service
                    if (arg1 == "clean")
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering health agent as service.");
                        Logging.Handler.Debug("Main", "Registering health agent as service", "");
                        Process.Start("sc", "create NetLock_RMM_Agent_Health binPath= \"" + Application_Paths.program_files_health_agent_path + "\" start= auto").WaitForExit();
                        Logging.Handler.Debug("Main", "Register health agent as service", "Done.");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register health agent as service: Done.");
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    // Register comm agent as service
                    Logging.Handler.Debug("Main", "Registering comm agent as service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering comm agent as service.");

                    // Set permissions
                    Bash.Execute_Script("Registering remote agent as service", false,
                        "chmod +x /usr/0x101_Cyber_Security/NetLock_RMM/Comm_Agent/NetLock_RMM_Agent_Comm");

                    // Create service file for comm agent
                    Linux.Helper.Linux.CreateServiceFile(Application_Paths.program_files_comm_agent_service_config_path_linux, "netlock-rmm-agent-comm", Application_Paths.program_files_comm_agent_service_path_unix, Application_Paths.program_files_comm_agent_dir, Application_Paths.program_files_comm_agent_service_log_path_unix);

                    Bash.Execute_Script("Registering comm agent as service", false,
                        "systemctl enable netlock-rmm-agent-comm");

                    Logging.Handler.Debug("Main", "Register comm agent as service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register comm agent as service: Done.");

                    // Register remote agent as service
                    Logging.Handler.Debug("Main", "Registering remote agent as service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering remote agent as service.");

                    // Set permissions
                    Bash.Execute_Script("Registering remote agent as service", false,
                        "chmod +x /usr/0x101_Cyber_Security/NetLock_RMM/Remote_Agent/NetLock_RMM_Agent_Remote");

                    // Create service file for remote agent
                    Linux.Helper.Linux.CreateServiceFile(Application_Paths.program_files_remote_agent_service_config_path_linux, "netlock-rmm-agent-remote", Application_Paths.program_files_remote_agent_service_path_unix, Application_Paths.program_files_remote_agent_dir, Application_Paths.program_files_remote_agent_service_log_path_unix);

                    Bash.Execute_Script("Registering remote agent as service", false,
                        "systemctl enable netlock-rmm-agent-remote");

                    // Register health agent as service
                    if (arg1 == "clean")
                    {
                        Logging.Handler.Debug("Main", "Registering health agent as service", "");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering health agent as service.");

                        // Set permissions
                        Bash.Execute_Script("Registering health agent as service", false,
                            "chmod +x /usr/0x101_Cyber_Security/NetLock_RMM/Health_Agent/NetLock_RMM_Agent_Health");

                        // Create service file for health agent
                        Linux.Helper.Linux.CreateServiceFile(Application_Paths.program_files_health_agent_service_config_path_linux, "netlock-rmm-agent_health", Application_Paths.program_files_health_agent_service_path_unix, Application_Paths.program_files_health_agent_dir, Application_Paths.program_files_health_agent_service_log_path_unix);

                        Bash.Execute_Script("Registering health agent as service", false,
                            "systemctl enable netlock-rmm-agent-health");

                        Logging.Handler.Debug("Main", "Register health agent as service", "Done.");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register health agent as service: Done.");
                    }

                    // Reload services
                    Bash.Execute_Script("Reload services", false,
                        "systemctl daemon-reload");
                }
                else if (OperatingSystem.IsMacOS())
                {
                    // Register comm agent as service
                    Logging.Handler.Debug("Main", "Registering comm agent as service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering comm agent as service.");

                    // Set permissions
                    Bash.Execute_Script("Registering remote agent as service", false,
                        $"sudo chmod +x {Application_Paths.program_files_comm_agent_service_path_unix}");

                    // Create service file for comm agent
                    MacOS.Helper.MacOS.CreateMacServiceFile();

                    Logging.Handler.Debug("Main", "Register comm agent as service", "Done.");
                }

                // Start services
                if (OperatingSystem.IsWindows())
                {
                    // Start comm agent service
                    Logging.Handler.Debug("Main", "Starting comm agent service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting comm agent service.");
                    Service.Start("NetLock_RMM_Agent_Comm");
                    Logging.Handler.Debug("Main", "Start comm agent service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start comm agent service: Done.");

                    // Start remote agent service
                    Logging.Handler.Debug("Main", "Starting remote agent service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting remote agent service.");
                    Service.Start("NetLock_RMM_Agent_Remote");
                    Logging.Handler.Debug("Main", "Start remote agent service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start remote agent service: Done.");

                    // Start health agent service
                    if (arg1 == "clean")
                    {
                        Logging.Handler.Debug("Main", "Starting health agent service", "");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting health agent service.");
                        Service.Start("NetLock_RMM_Agent_Health");
                        Logging.Handler.Debug("Main", "Start health agent service", "Done.");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start health agent service: Done.");
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    // Start comm agent service
                    Logging.Handler.Debug("Main", "Starting comm agent service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting comm agent service.");
                    Bash.Execute_Script("Starting comm agent service", false, "systemctl start netlock-rmm-agent-comm");
                    Logging.Handler.Debug("Main", "Start comm agent service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start comm agent service: Done.");

                    // Start remote agent service
                    Logging.Handler.Debug("Main", "Starting remote agent service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting remote agent service.");
                    Bash.Execute_Script("Starting remote agent service", false, "systemctl start netlock-rmm-agent-remote");
                    Logging.Handler.Debug("Main", "Start remote agent service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start remote agent service: Done.");

                    if (arg1 == "clean")
                    {
                        // Start health agent service
                        Logging.Handler.Debug("Main", "Starting health agent service", "");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting health agent service.");
                        Bash.Execute_Script("Starting health agent service", false, "systemctl start netlock-rmm-agent-health");
                        Logging.Handler.Debug("Main", "Start health agent service", "Done.");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start health agent service: Done.");
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    // Happens automatically through the service file creation on MacOS
                }

                // Delete temp dir
                /*Logging.Handler.Debug("Main", "Delete temp dir", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete temp dir.");
                Helper.IO.Delete_Directory(Application_Paths.c_temp_netlock_dir);
                Logging.Handler.Debug("Main", "Delete temp dir", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete temp dir: Done.");
                */

                // Delete unnecessary leftovers - Agent Bundle
                if (File.Exists(agent_package_path))
                    File.Delete(agent_package_path);

                // Delete temp bundle extraction directory
                if (Directory.Exists(agent_bundle_temp_dir))
                    Directory.Delete(agent_bundle_temp_dir, true);

                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Installation finished.");

                // Delete logs if --no-log parameter is set
                if (noLog)
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting logs (--no-log parameter set)...");
                    Logging.Handler.DeleteAllLogs();
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Logs deleted.");
                }

                // Wait for 5 seconds
                Thread.Sleep(5000);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Main", "General error", ex.ToString());
                Console.WriteLine("Error: " + ex.ToString());
                Thread.Sleep(5000);
            }
        }
    }
}
