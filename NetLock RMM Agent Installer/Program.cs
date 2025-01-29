using Helper;
using MacOS.Helper;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;

namespace NetLock_RMM_Agent_Installer
{
    internal class Program
    {
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
            // Set the execution directory
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            Task.Run(() => MainAsync(args)).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            try
            {
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
                Console.WriteLine("[" + DateTime.Now + "] - [Execution path] -> " + Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                Console.ForegroundColor = ConsoleColor.White;

                string arg1 = String.Empty;
                string arg2 = String.Empty;

                string server_config_json_new = String.Empty;
                bool arguments = true;

                // Verify arguments
                try
                {
                    arg1 = args[0]; // clean or fix
                    Logging.Handler.Debug("Main", "Mode", arg1);
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Mode: " + arg1);
                    arg2 = args[1]; // server_config.json path
                    Logging.Handler.Debug("Main", "Server_Config_Path", arg2);
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Server_Config_Path: " + arg2);
                }
                catch
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Error: Invalid or missing arguments. Falling back to embedded server config.");
                    arguments = false;
                }

                if (arguments)
                {
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
                    // Check if 1 click installer
                    // Access custom resource
                    string resources = Resources.Read_Resource(Assembly.GetExecutingAssembly().Location);

                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> " + resources);

                    if (resources != "No embedded server config found.")
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Server config found.");
                        server_config_json_new = resources;
                        arg1 = "clean"; // Set mode to clean if using embedded server config. This will reinstall & unauthorize the agent.
                    }
                    else
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Error: Neither arguments provided or embedded server config found. Aborting.");
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

                // Delete local packages
                // Comm agent
                if (File.Exists(Application_Paths.comm_agent_package_path))
                {
                    File.Delete(Application_Paths.comm_agent_package_path);
                    Logging.Handler.Debug("Main", "Delete old agent.package", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete old agent.package: Done.");
                }
                else
                {
                    Logging.Handler.Debug("Main", "Delete old agent.package", "Not present.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete old agent.package: Not present.");
                }

                // Remote agent
                if (File.Exists(Application_Paths.remote_agent_package_path))
                {
                    File.Delete(Application_Paths.remote_agent_package_path);
                    Logging.Handler.Debug("Main", "Delete old remote.package", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete old remote.package: Done.");
                }
                else
                {
                    Logging.Handler.Debug("Main", "Delete old remote.package", "Not present.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete old remote.package: Not present.");
                }

                // Health agent
                if (File.Exists(Application_Paths.health_agent_package_path))
                {
                    File.Delete(Application_Paths.health_agent_package_path);
                    Logging.Handler.Debug("Main", "Delete old health.package", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete old health.package: Done.");
                }
                else
                {
                    Logging.Handler.Debug("Main", "Delete old health.package", "Not present.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete old health.package: Not present.");
                }

                // Check OS & Architecture
                string comm_package_url = String.Empty;
                string remote_package_url = String.Empty;
                string health_package_url = String.Empty;
                string user_process_package_url = String.Empty;

                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Detecting OS & Architecture.");

                if (OperatingSystem.IsWindows() && Environment.Is64BitOperatingSystem)
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Windows x64 detected.");
                    comm_package_url = Application_Paths.comm_agent_package_url_winx64;
                    remote_package_url = Application_Paths.remote_agent_package_url_winx64;
                    health_package_url = Application_Paths.health_agent_package_url_winx64;
                    user_process_package_url = Application_Paths.user_process_package_url_winx64;
                }
                else if (OperatingSystem.IsWindows() && !Environment.Is64BitOperatingSystem)
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Switching to WindowsARM.");
                    comm_package_url = Application_Paths.comm_agent_package_url_winarm64;
                    remote_package_url = Application_Paths.remote_agent_package_url_winarm64;
                    health_package_url = Application_Paths.health_agent_package_url_winarm64;
                    user_process_package_url = Application_Paths.user_process_package_url_winarm64;
                }
                else if (OperatingSystem.IsLinux() && Environment.Is64BitOperatingSystem)
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Linux x64 detected.");
                    comm_package_url = Application_Paths.comm_agent_package_url_linuxx64;
                    remote_package_url = Application_Paths.remote_agent_package_url_linuxx64;
                    health_package_url = Application_Paths.health_agent_package_url_linuxx64;
                    user_process_package_url = Application_Paths.user_process_package_url_linuxx64;
                }
                else if (OperatingSystem.IsLinux() && !Environment.Is64BitOperatingSystem)
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Switching to LinuxARM.");
                    comm_package_url = Application_Paths.comm_agent_package_url_linuxarm64;
                    remote_package_url = Application_Paths.remote_agent_package_url_linuxarm64;
                    health_package_url = Application_Paths.health_agent_package_url_linuxarm64;
                    user_process_package_url = Application_Paths.user_process_package_url_linuxarm64;
                }
                else if (OperatingSystem.IsMacOS() && Environment.Is64BitOperatingSystem)
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> MacOS x64 detected.");
                    comm_package_url = Application_Paths.comm_agent_package_url_osx64;
                    remote_package_url = Application_Paths.remote_agent_package_url_osx64;
                    health_package_url = Application_Paths.health_agent_package_url_osx64;
                    user_process_package_url = Application_Paths.user_process_package_url_osx64;
                }
                else if (OperatingSystem.IsMacOS() && !Environment.Is64BitOperatingSystem)
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Switching to MacOSARM.");
                    comm_package_url = Application_Paths.comm_agent_package_url_osxarm64;
                    remote_package_url = Application_Paths.remote_agent_package_url_osxarm64;
                    health_package_url = Application_Paths.health_agent_package_url_osxarm64;
                    user_process_package_url = Application_Paths.user_process_package_url_osxarm64;
                }
                else
                {
                    Logging.Handler.Error("Main", "Unsupported OS & Architecture", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Unsupported OS & Architecture.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                bool http_status = true;

                // Download comm agent package
                http_status = await Helper.Http.DownloadFileAsync(server_config_new.ssl, update_server + comm_package_url, Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.comm_agent_package_path), server_config_new.package_guid);

                Logging.Handler.Debug("Main", "Download comm agent package", http_status.ToString());
                Logging.Handler.Debug("Main", "Download comm agent package", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Download comm agent package: Done.");

                // Download remote agent package
                http_status = await Helper.Http.DownloadFileAsync(server_config_new.ssl, update_server + remote_package_url, Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.remote_agent_package_path), server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Download remote agent package", http_status.ToString());
                Logging.Handler.Debug("Main", "Download remote agent package", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Download remote agent package: Done.");

                // Download health agent package
                http_status = await Helper.Http.DownloadFileAsync(server_config_new.ssl, update_server + health_package_url, Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.health_agent_package_path), server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Download health agent package", http_status.ToString());
                Logging.Handler.Debug("Main", "Download health agent package", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Download health agent package: Done.");

                // Download User Process
                if (OperatingSystem.IsWindows())
                {
                    http_status = await Helper.Http.DownloadFileAsync(server_config_new.ssl, update_server + user_process_package_url, Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.user_process_package_path), server_config_new.package_guid);
                    Logging.Handler.Debug("Main", "Download user process package", http_status.ToString());
                    Logging.Handler.Debug("Main", "Download user process package", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Download user process package: Done.");
                }

                // Get hash comm agent package
                string comm_agent_package_hash = await Helper.Http.GetHashAsync(server_config_new.ssl, trust_server + comm_package_url + ".sha512", server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Get hash comm agent package", comm_agent_package_hash);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Get hash comm agent package: " + comm_agent_package_hash);

                // Get hash remote agent package
                string remote_agent_package_hash = await Helper.Http.GetHashAsync(server_config_new.ssl, trust_server + remote_package_url + ".sha512", server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Get hash remote agent package", remote_agent_package_hash);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Get hash remote agent package: " + remote_agent_package_hash);

                // Get hash health agent package
                string health_agent_package_hash = await Helper.Http.GetHashAsync(server_config_new.ssl, trust_server + health_package_url + ".sha512", server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Get hash health agent package", health_agent_package_hash);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Get hash health agent package: " + health_agent_package_hash);

                // Get hash user process package
                string user_process_package_hash = String.Empty;
                if (OperatingSystem.IsWindows())
                {
                    user_process_package_hash = await Helper.Http.GetHashAsync(server_config_new.ssl, trust_server + user_process_package_url + ".sha512", server_config_new.package_guid);
                    Logging.Handler.Debug("Main", "Get hash user process package", user_process_package_hash);
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Get hash user process package: " + user_process_package_hash);
                }

                if (String.IsNullOrEmpty(comm_agent_package_hash) || String.IsNullOrEmpty(remote_agent_package_hash) || String.IsNullOrEmpty(health_agent_package_hash) || http_status == false)
                {
                    Logging.Handler.Debug("Main", "Error receiving data.", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Error receiving data.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Aborting installation.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                // Check hash comm agent package
                string comm_agent_package_path = Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.comm_agent_package_path);
                string comm_agent_package_hash_local = Helper.IO.Get_SHA512(comm_agent_package_path);

                Logging.Handler.Debug("Main", "Check hash comm agent package", comm_agent_package_hash_local);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash comm agent package: " + comm_agent_package_hash_local);

                if (comm_agent_package_hash_local == comm_agent_package_hash)
                {
                    Logging.Handler.Debug("Main", "Check hash comm agent package", "OK");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash comm agent package: OK");
                }
                else
                {
                    Logging.Handler.Debug("Main", "Check hash comm agent package", "KO");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash comm agent package: KO");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Aborting installation.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                // Check hash remote agent package
                string remote_agent_package_path = Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.remote_agent_package_path);
                string remote_agent_package_hash_local = Helper.IO.Get_SHA512(remote_agent_package_path);

                Logging.Handler.Debug("Main", "Check hash remote agent package", remote_agent_package_hash_local);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash remote agent package: " + remote_agent_package_hash_local);

                if (remote_agent_package_hash_local == remote_agent_package_hash)
                {
                    Logging.Handler.Debug("Main", "Check hash remote agent package", "OK");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash remote agent package: OK");
                }
                else
                {
                    Logging.Handler.Debug("Main", "Check hash remote agent package", "KO");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash remote agent package: KO");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Aborting installation.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                // Check hash health agent package
                string health_agent_package_path = Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.health_agent_package_path);
                string health_agent_package_hash_local = Helper.IO.Get_SHA512(health_agent_package_path);

                Logging.Handler.Debug("Main", "Check hash health agent package", health_agent_package_hash_local);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash health agent package: " + health_agent_package_hash_local);

                if (health_agent_package_hash_local == health_agent_package_hash)
                {
                    Logging.Handler.Debug("Main", "Check hash health agent package", "OK");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash health agent package: OK");
                }
                else
                {
                    Logging.Handler.Debug("Main", "Check hash health agent package", "KO");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash health agent package: KO");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Aborting installation.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                // Check hash user process package
                if (OperatingSystem.IsWindows())
                {
                    string user_process_package_path = Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.user_process_package_path);
                    string user_process_package_hash_local = Helper.IO.Get_SHA512(user_process_package_path);

                    Logging.Handler.Debug("Main", "Check hash user process package", user_process_package_hash_local);
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash user process package: " + user_process_package_hash_local);

                    if (user_process_package_hash_local == user_process_package_hash)
                    {
                        Logging.Handler.Debug("Main", "Check hash user process package", "OK");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash user process package: OK");
                    }
                    else
                    {
                        Logging.Handler.Debug("Main", "Check hash user process package", "KO");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash user process package: KO");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Aborting installation.");
                        Thread.Sleep(5000);
                        Environment.Exit(0);
                    }
                }

                // Backup old server_config.json if fix mode
                if (arg1 == "fix")
                {
                    Logging.Handler.Debug("Main", "Fix mode", "Enabled.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Fix mode: Enabled.");

                    if (!File.Exists(Application_Paths.program_data_comm_agent_server_config))
                    {
                        Logging.Handler.Debug("Main", "Backup old server_config.json", "Not present.");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Old server_config.json: Not present. Fix argument wont work. Please use clean.");
                        Thread.Sleep(5000);
                        Environment.Exit(0);
                    }

                    // Delete old backup server config
                    if (File.Exists(Application_Paths.c_temp_server_config_backup_path))
                    {
                        File.Delete(Application_Paths.c_temp_server_config_backup_path);
                        Logging.Handler.Debug("Main", "Delete old server_config.json backup", "Done.");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete old server_config.json backup: Done.");
                    }

                    // Backup old server_config.json
                    if (File.Exists(Application_Paths.program_data_comm_agent_server_config))
                    {
                        File.Copy(Application_Paths.program_data_comm_agent_server_config, Application_Paths.c_temp_server_config_backup_path, true);
                        Logging.Handler.Debug("Main", "Backup old server_config.json", "Done.");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Backup old server_config.json: Done.");
                    }
                    else
                    {
                        Logging.Handler.Debug("Main", "Backup old server_config.json", "Not present.");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Backup old server_config.json: Not present.");
                    }

                    // continues after uninstaller
                }

                // Execute uninstaller and wait for it to finish
                if (arg1 == "fix")
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Fix mode.");

                    // Stop services
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Stopping services.");
                    if (OperatingSystem.IsWindows())
                    {
                        Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Agent_Comm");
                        Helper.Service.Stop("NetLock_RMM_Agent_Comm");
                        Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Agent_Remote");
                        Helper.Service.Stop("NetLock_RMM_Agent_Remote");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Stopping services", false, "systemctl stop netlock-rmm-agent-comm");
                        Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Stopping services", false, "systemctl stop netlock-rmm-agent-remote");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Stopping services", false, "launchctl stop netlock-rmm-agent-comm");
                        Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Stopping services", false, "launchctl stop netlock-rmm-agent-remote");
                    }
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services stopped.");

                    // Kill processes
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminating processes.");
                    if (OperatingSystem.IsWindows())
                    {
                        Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM Agent Comm.exe");
                        Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM Agent Comm.exe\"");
                        Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM Agent Remote.exe");
                        Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM Agent Remote.exe\"");
                        Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM User Process.exe");
                        Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM User Process.exe\"");
                        //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"yara64.exe\""); // yara64.exe is (currently) not used in the project, its part of a netlock legacy feature
                        //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"devcon_x64.exe\""); // devcon_x64.exe is (currently) not used in the project, its part of a netlock legacy feature
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-comm");
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-remote");
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-user-process");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-user-process");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-comm");
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-remote");
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-user-process");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-user-process");
                    }
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminated processes.");

                    // Delete services
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting services.");
                    if (OperatingSystem.IsWindows())
                    {
                        Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Agent_Comm");
                        Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Agent_Comm");
                        Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Agent_Remote");
                        Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Agent_Remote");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        Logging.Handler.Debug("Main", "Deleting services.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Deleting services", false, "systemctl disable netlock-rmm-agent-comm");
                        Logging.Handler.Debug("Main", "Deleting services.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Deleting services", false, "systemctl disable netlock-rmm-agent-remote");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        Logging.Handler.Debug("Main", "Deleting services.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Deleting services", false, "launchctl unload /Library/LaunchDaemons/netlock-rmm-agent-comm.plist");
                        Logging.Handler.Debug("Main", "Deleting services.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Deleting services", false, "launchctl unload /Library/LaunchDaemons/netlock-rmm-agent-remote.plist");
                    }
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services deleted.");

                    // Delete files
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting files.");
                    Logging.Handler.Debug("Main", "Deleting files.", Application_Paths.program_data_comm_agent_events_path);
                    Helper.IO.Delete_File(Application_Paths.program_data_comm_agent_events_path);
                    Logging.Handler.Debug("Main", "Deleting files.", Application_Paths.program_data_comm_agent_policies_path);
                    Helper.IO.Delete_File(Application_Paths.program_data_comm_agent_policies_path);
                    Logging.Handler.Debug("Main", "Deleting files.", Application_Paths.program_data_comm_agent_server_config);
                    Helper.IO.Delete_File(Application_Paths.program_data_comm_agent_version_path);
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleted files.");

                    // Delete directories
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting directories.");
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_files_comm_agent_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_files_comm_agent_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_logs_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_logs_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_jobs_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_jobs_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_msdav_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_msdav_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_scripts_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_scripts_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_backups_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_backups_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_sensors_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_sensors_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_dumps_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_dumps_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_temp_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_temp_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_files_remote_agent_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_files_remote_agent_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_remote_agent_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_remote_agent_dir);

                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleted directories.");
                }
                else if (arg1 == "clean" || !arguments)
                {
                    // Stop services
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Stopping services.");
                    if (OperatingSystem.IsWindows())
                    {
                        Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Agent_Comm");
                        Helper.Service.Stop("NetLock_RMM_Agent_Comm");
                        Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Agent_Remote");
                        Helper.Service.Stop("NetLock_RMM_Agent_Remote");
                        Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Agent_Health");
                        Helper.Service.Stop("NetLock_RMM_Agent_Health");

                        // For legacy installations
                        Logging.Handler.Debug("Main", "Stopping services (legacy).", "NetLock_RMM_Comm_Agent_Windows");
                        Helper.Service.Stop("NetLock_RMM_Agent_Comm");
                        Logging.Handler.Debug("Main", "Stopping services (legacy).", "NetLock_RMM_Health_Agent_Windows");
                        Helper.Service.Stop("NetLock_RMM_Agent_Remote");
                        Logging.Handler.Debug("Main", "Stopping services (legacy).", "NetLock_RMM_Remote_Agent_Windows");
                        Helper.Service.Stop("NetLock_RMM_Agent_Health");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Stopping services", false, "systemctl stop netlock-rmm-agent-comm");
                        Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Stopping services", false, "systemctl stop netlock-rmm-agent-remote");
                        Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-health-agent");
                        Bash.Execute_Script("Stopping services", false, "systemctl stop netlock-rmm-health-agent");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Stopping services", false, "launchctl stop netlock-rmm-agent-comm");
                        Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Stopping services", false, "launchctl stop netlock-rmm-agent-remote");
                        Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-health-agent");
                        Bash.Execute_Script("Stopping services", false, "launchctl stop netlock-rmm-health-agent");
                    }
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services stopped.");

                    // Kill processes
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminating processes.");
                    if (OperatingSystem.IsWindows())
                    {
                        Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM Agent Comm.exe");
                        Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM Agent Comm.exe\"");
                        Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM Agent Remote.exe");
                        Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM Agent Remote.exe\"");
                        Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM Agent Health.exe");
                        Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM Agent Health.exe\"");
                        Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM User Process.exe");
                        Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM User Process.exe\"");
                        //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"yara64.exe\""); // yara64.exe is (currently) not used in the project, its part of a netlock legacy feature
                        //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"devcon_x64.exe\""); // devcon_x64.exe is (currently) not used in the project, its part of a netlock legacy feature
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-comm");
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-remote");
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-health-agent");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-health-agent");
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-user-process");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-user-process");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-comm");
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-remote");
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-health-agent");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-health-agent");
                        Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-user-process");
                        Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-user-process");
                    }
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminated processes.");

                    // Delete services
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting services.");
                    if (OperatingSystem.IsWindows())
                    {
                        Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Agent_Comm");
                        Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Agent_Comm");
                        Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Agent_Remote");
                        Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Agent_Remote");
                        Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Agent_Health");
                        Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Agent_Health");
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        Logging.Handler.Debug("Main", "Deleting services.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Deleting services", false, "systemctl disable netlock-rmm-agent-comm");
                        Logging.Handler.Debug("Main", "Deleting services.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Deleting services", false, "systemctl disable netlock-rmm-agent-remote");
                        Logging.Handler.Debug("Main", "Deleting services.", "netlock-rmm-health-agent");
                        Bash.Execute_Script("Deleting services", false, "systemctl disable netlock-rmm-health-agent");
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        Logging.Handler.Debug("Main", "Deleting services.", "netlock-rmm-agent-comm");
                        Bash.Execute_Script("Deleting services", false, "launchctl unload /Library/LaunchDaemons/netlock-rmm-agent-comm.plist");
                        Logging.Handler.Debug("Main", "Deleting services.", "netlock-rmm-agent-remote");
                        Bash.Execute_Script("Deleting services", false, "launchctl unload /Library/LaunchDaemons/netlock-rmm-agent-remote.plist");
                        Logging.Handler.Debug("Main", "Deleting services.", "netlock-rmm-health-agent");
                        Bash.Execute_Script("Deleting services", false, "launchctl unload /Library/LaunchDaemons/netlock-rmm-health-agent.plist");
                    }
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services deleted.");

                    // Delete directories
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting directories.");
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_files_0x101_cyber_security_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_files_0x101_cyber_security_dir);
                    Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_0x101_cyber_security_dir);
                    Helper.IO.Delete_Directory(Application_Paths.program_data_0x101_cyber_security_dir);
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Directories deleted.");
                }

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

                // Create program data dir (user process)
                if (!Directory.Exists(Application_Paths.program_data_user_process_dir))
                    Directory.CreateDirectory(Application_Paths.program_data_user_process_dir);

                // Extract comm agent package
                Logging.Handler.Debug("Main", "Extracting comm agent package", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting comm agent package.");
                ZipFile.ExtractToDirectory(Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.comm_agent_package_path), Application_Paths.program_files_comm_agent_dir);

                // Fix server_config.json
                if (arg1 == "fix")
                {
                    // Fix server_config.json
                    // Read old server_config.json
                    string server_config_json_old = File.ReadAllText(Application_Paths.c_temp_server_config_backup_path);
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

                // Extract health agent package
                if (arg1 == "clean")
                {
                    // Extract health agent package
                    Logging.Handler.Debug("Main", "Extracting health agent package", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting health agent package.");
                    ZipFile.ExtractToDirectory(Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.health_agent_package_path), Application_Paths.program_files_health_agent_dir);
                }

                // Extract remote agent package
                Logging.Handler.Debug("Main", "Extracting remote agent package", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting remote agent package.");
                ZipFile.ExtractToDirectory(Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.remote_agent_package_path), Application_Paths.program_files_remote_agent_dir);

                // Extract user process package
                Logging.Handler.Debug("Main", "Extracting user process package", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting user process package.");
                ZipFile.ExtractToDirectory(Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.user_process_package_path), Application_Paths.program_files_user_process_dir);

                // Copy server config json to program data dir
                if (arg1 == "clean")
                {
                    Logging.Handler.Debug("Main", "Copy server_config.json", "Clean mode.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Copy server_config.json: Clean mode.");
                    File.Copy(arg2, Application_Paths.program_data_comm_agent_server_config, true);
                    File.Copy(arg2, Application_Paths.program_data_health_agent_server_config, true);
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
                    Bash.Execute_Script("Registering comm agent as service", false,
                        "cp /path/to/comm-agent /usr/bin/netlock-rmm-agent-comm");
                    Bash.Execute_Script("Registering comm agent as service", false,
                        "chmod +x /usr/bin/netlock-rmm-agent-comm");

                    // Create service file for comm agent
                    Linux.Helper.Linux.CreateServiceFile("/etc/systemd/system/netlock-rmm-agent-comm.service", "netlock-rmm-agent-comm", "/usr/bin/netlock-rmm-agent-comm");

                    Bash.Execute_Script("Registering comm agent as service", false,
                        "systemctl enable netlock-rmm-agent-comm");
                    Logging.Handler.Debug("Main", "Register comm agent as service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register comm agent as service: Done.");

                    // Register remote agent as service
                    Logging.Handler.Debug("Main", "Registering remote agent as service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering remote agent as service.");
                    Bash.Execute_Script("Registering remote agent as service", false,
                        "cp /path/to/remote-agent /usr/bin/netlock-rmm-agent-remote");
                    Bash.Execute_Script("Registering remote agent as service", false,
                        "chmod +x /usr/bin/netlock-rmm-agent-remote");

                    // Create service file for remote agent
                    Linux.Helper.Linux.CreateServiceFile("/etc/systemd/system/netlock-rmm-agent-remote.service", "netlock-rmm-agent-remote", "/usr/bin/netlock-rmm-agent-remote");

                    Bash.Execute_Script("Registering remote agent as service", false,
                        "systemctl enable netlock-rmm-agent-remote");
                    Logging.Handler.Debug("Main", "Register remote agent as service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register remote agent as service: Done.");

                    // Register health agent as service
                    if (arg1 == "clean")
                    {
                        Logging.Handler.Debug("Main", "Registering health agent as service", "");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering health agent as service.");
                        Bash.Execute_Script("Registering health agent as service", false,
                            "cp /path/to/health-agent /usr/bin/netlock-rmm-health-agent");
                        Bash.Execute_Script("Registering health agent as service", false,
                            "chmod +x /usr/bin/netlock-rmm-health-agent");

                        // Create service file for health agent
                        Linux.Helper.Linux.CreateServiceFile("/etc/systemd/system/netlock-rmm-health-agent.service", "netlock-rmm-health-agent", "/usr/bin/netlock-rmm-health-agent");

                        Bash.Execute_Script("Registering health agent as service", false,
                            "systemctl enable netlock-rmm-health-agent");
                        Logging.Handler.Debug("Main", "Register health agent as service", "Done.");
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register health agent as service: Done.");
                    }
                }


                // Start comm agent service
                Logging.Handler.Debug("Main", "Starting comm agent service", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting comm agent service.");
                Helper.Service.Start("NetLock_RMM_Agent_Comm");
                Logging.Handler.Debug("Main", "Start comm agent service", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start comm agent service: Done.");

                // Start remote agent service
                Logging.Handler.Debug("Main", "Starting remote agent service", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting remote agent service.");
                Helper.Service.Start("NetLock_RMM_Agent_Remote");
                Logging.Handler.Debug("Main", "Start remote agent service", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start remote agent service: Done.");

                // Start health agent service
                if (arg1 == "clean")
                {
                    Logging.Handler.Debug("Main", "Starting health agent service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting health agent service.");
                    Helper.Service.Start("NetLock_RMM_Agent_Health");
                    Logging.Handler.Debug("Main", "Start health agent service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start health agent service: Done.");
                }

                // Delete temp dir
                /*Logging.Handler.Debug("Main", "Delete temp dir", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete temp dir.");
                Helper.IO.Delete_Directory(Application_Paths.c_temp_netlock_dir);
                Logging.Handler.Debug("Main", "Delete temp dir", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete temp dir: Done.");
                */
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Installation finished.");

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
