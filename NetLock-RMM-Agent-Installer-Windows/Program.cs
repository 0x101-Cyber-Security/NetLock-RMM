using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.ServiceProcess;
using System.Threading;
using System.IO.Compression;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Security.Principal;
using System.Runtime.CompilerServices;

namespace NetLock_RMM_Agent_Installer_Windows
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

    internal class Program
    {
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
                string os_version = Environment.OSVersion.Version.ToString();
                char os_version_char = os_version[0];

                if (os_version_char.ToString() == "6")
                {
                    Logging.Handler.Debug("Main", "OS_Version", "OS (" + os_version_char + ") is old. Switch to compatibility mode.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> OS (" + os_version_char + ") is old. Switch to compatibility mode.");
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                }
                else
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> OS (" + os_version_char + ") is new.");
                    Logging.Handler.Debug("Main", "OS_Version", "OS (" + os_version_char + ") is new.");
                }

                Console.Title = "NetLock RMM Agent Installer Windows";
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

                string arg1 = String.Empty;
                string arg2 = String.Empty;

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
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Error: Invalid argument. Please pass the mode & then the server_config.json path. See in docs.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                Logging.Handler.Debug("Main", "Arguments", arg1 + " " + arg2);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Arguments: " + arg1 + " " + arg2);

                // Extract server_config.json
                string server_config_json_new = File.ReadAllText(arg2);

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

                bool http_status = true;

                // Download comm agent package
                http_status = await Helper.Http.DownloadFileAsync(server_config_new.ssl, update_server + Application_Paths.comm_agent_package_url, Application_Paths.c_temp_netlock_dir + Application_Paths.comm_agent_package_path, server_config_new.package_guid);

                Logging.Handler.Debug("Main", "Download comm agent package", http_status.ToString());
                Logging.Handler.Debug("Main", "Download comm agent package", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Download comm agent package: Done.");

                // Download remote agent package
                http_status = await Helper.Http.DownloadFileAsync(server_config_new.ssl, update_server + Application_Paths.remote_agent_package_url, Application_Paths.c_temp_netlock_dir + Application_Paths.remote_agent_package_path, server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Download remote agent package", http_status.ToString());
                Logging.Handler.Debug("Main", "Download remote agent package", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Download remote agent package: Done.");

                // Download health agent package
                http_status = await Helper.Http.DownloadFileAsync(server_config_new.ssl, update_server + Application_Paths.health_agent_package_url, Application_Paths.c_temp_netlock_dir + Application_Paths.health_agent_package_path, server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Download health agent package", http_status.ToString());
                Logging.Handler.Debug("Main", "Download health agent package", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Download health agent package: Done.");

                // Download User Process
                http_status = await Helper.Http.DownloadFileAsync(server_config_new.ssl, update_server + Application_Paths.user_process_package_url, Application_Paths.c_temp_netlock_dir + Application_Paths.user_process_package_path, server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Download user process package", http_status.ToString());
                Logging.Handler.Debug("Main", "Download user process package", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Download user process package: Done.");

                // Download uninstaller
                http_status = await Helper.Http.DownloadFileAsync(server_config_new.ssl, update_server + Application_Paths.uninstaller_package_url, Application_Paths.c_temp_netlock_dir + Application_Paths.uninstaller_package_path, server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Download uninstaller", http_status.ToString());
                Logging.Handler.Debug("Main", "Download uninstaller", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Download uninstaller: Done.");

                // Get hash comm agent package
                string comm_agent_package_hash = await Helper.Http.GetHashAsync(server_config_new.ssl, trust_server + Application_Paths.comm_agent_package_url + ".sha512", server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Get hash comm agent package", comm_agent_package_hash);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Get hash comm agent package: " + comm_agent_package_hash);

                // Get hash remote agent package
                string remote_agent_package_hash = await Helper.Http.GetHashAsync(server_config_new.ssl, trust_server + Application_Paths.remote_agent_package_url + ".sha512", server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Get hash remote agent package", remote_agent_package_hash);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Get hash remote agent package: " + remote_agent_package_hash);

                // Get hash health agent package
                string health_agent_package_hash = await Helper.Http.GetHashAsync(server_config_new.ssl, trust_server + Application_Paths.health_agent_package_url + ".sha512", server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Get hash health agent package", health_agent_package_hash);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Get hash health agent package: " + health_agent_package_hash);

                // Get hash user process package
                string user_process_package_hash = await Helper.Http.GetHashAsync(server_config_new.ssl, trust_server + Application_Paths.user_process_package_url + ".sha512", server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Get hash user process package", user_process_package_hash);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Get hash user process package: " + user_process_package_hash);

                // Get hash uninstaller
                string uninstaller_hash = await Helper.Http.GetHashAsync(server_config_new.ssl, trust_server + Application_Paths.uninstaller_package_url + ".sha512", server_config_new.package_guid);
                Logging.Handler.Debug("Main", "Get hash uninstaller", uninstaller_hash);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Get hash uninstaller: " + uninstaller_hash);

                if (String.IsNullOrEmpty(comm_agent_package_hash) || String.IsNullOrEmpty(remote_agent_package_hash) || String.IsNullOrEmpty(health_agent_package_hash) || String.IsNullOrEmpty(uninstaller_hash) || http_status == false)
                {
                    Logging.Handler.Debug("Main", "Error receiving data.", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Error receiving data.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Aborting installation.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
                }

                // Check hash comm agent package
                string comm_agent_package_path = Application_Paths.c_temp_netlock_dir + Application_Paths.comm_agent_package_path;
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
                string remote_agent_package_path = Application_Paths.c_temp_netlock_dir + Application_Paths.remote_agent_package_path;
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
                string health_agent_package_path = Application_Paths.c_temp_netlock_dir + Application_Paths.health_agent_package_path;
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
                string user_process_package_path = Application_Paths.c_temp_netlock_dir + Application_Paths.user_process_package_path;
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

                // Check hash uninstaller
                string uninstaller_path = Application_Paths.c_temp_netlock_dir + Application_Paths.uninstaller_package_path;
                string uninstaller_hash_local = Helper.IO.Get_SHA512(uninstaller_path);

                Logging.Handler.Debug("Main", "Check hash uninstaller", uninstaller_hash_local);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash uninstaller: " + uninstaller_hash_local);

                if (uninstaller_hash_local == uninstaller_hash)
                {
                    Logging.Handler.Debug("Main", "Check hash uninstaller", "OK");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash uninstaller: OK");
                }
                else
                {
                    Logging.Handler.Debug("Main", "Check hash uninstaller", "KO");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Check hash uninstaller: KO");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Aborting installation.");
                    Thread.Sleep(5000);
                    Environment.Exit(0);
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

                // Delete old uninstaller
                Helper.IO.Delete_Directory(Application_Paths.c_temp_uninstaller_dir);
                Logging.Handler.Debug("Main", "Delete old uninstaller", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Delete old uninstaller: Done.");

                // Extract uninstaller.package
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting uninstaller.");
                ZipFile.ExtractToDirectory(Application_Paths.c_temp_netlock_dir + Application_Paths.uninstaller_package_path, Application_Paths.c_temp_uninstaller_dir);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extract uninstaller: Done.");

                // Execute uninstaller and wait for it to finish
                Logging.Handler.Debug("Main", "Starting uninstaller", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting uninstaller.");

                Process uninstaller = new Process();
                uninstaller.StartInfo.FileName = Application_Paths.c_temp_uninstaller_path;
                uninstaller.StartInfo.Arguments = arg1;
                uninstaller.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                uninstaller.Start();
                uninstaller.WaitForExit();

                Logging.Handler.Debug("Main", "Uninstaller closed", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Uninstaller closed.");

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
                ZipFile.ExtractToDirectory(Application_Paths.c_temp_netlock_dir + Application_Paths.comm_agent_package_path, Application_Paths.program_files_comm_agent_dir);

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
                    ZipFile.ExtractToDirectory(Application_Paths.c_temp_netlock_dir + Application_Paths.health_agent_package_path, Application_Paths.program_files_health_agent_dir);
                }

                // Extract remote agent package
                Logging.Handler.Debug("Main", "Extracting remote agent package", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting remote agent package.");
                ZipFile.ExtractToDirectory(Application_Paths.c_temp_netlock_dir + Application_Paths.remote_agent_package_path, Application_Paths.program_files_remote_agent_dir);

                // Extract user process package
                Logging.Handler.Debug("Main", "Extracting user process package", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extracting user process package.");
                ZipFile.ExtractToDirectory(Application_Paths.c_temp_netlock_dir + Application_Paths.user_process_package_path, Application_Paths.program_files_user_process_dir);

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

                // Register comm agent as service
                Logging.Handler.Debug("Main", "Registering comm agent as service", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering comm agent as service.");
                Process.Start("sc", "create NetLock_RMM_Comm_Agent_Windows binPath= \"" + Application_Paths.program_files_comm_agent_path + "\" start= auto").WaitForExit();
                Logging.Handler.Debug("Main", "Register comm agent as service", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register comm agent as service: Done.");

                // Register remote agent as service
                Logging.Handler.Debug("Main", "Registering remote agent as service", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering remote agent as service.");
                Process.Start("sc", "create NetLock_RMM_Remote_Agent_Windows binPath= \"" + Application_Paths.program_files_remote_agent_path + "\" start= auto").WaitForExit();
                Logging.Handler.Debug("Main", "Register remote agent as service", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register remote agent as service: Done.");

                // Register health agent as service
                if (arg1 == "clean")
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Registering health agent as service.");
                    Logging.Handler.Debug("Main", "Registering health agent as service", "");
                    Process.Start("sc", "create NetLock_RMM_Health_Agent_Windows binPath= \"" + Application_Paths.program_files_health_agent_path + "\" start= auto").WaitForExit();
                    Logging.Handler.Debug("Main", "Register health agent as service", "Done.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Register health agent as service: Done.");
                }

                // Start comm agent service
                Logging.Handler.Debug("Main", "Starting comm agent service", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting comm agent service.");
                Helper.Service.Start("NetLock_RMM_Comm_Agent_Windows");
                Logging.Handler.Debug("Main", "Start comm agent service", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start comm agent service: Done.");

                // Start remote agent service
                Logging.Handler.Debug("Main", "Starting remote agent service", "");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting remote agent service.");
                Helper.Service.Start("NetLock_RMM_Remote_Agent_Windows");
                Logging.Handler.Debug("Main", "Start remote agent service", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Start remote agent service: Done.");

                // Start health agent service
                if (arg1 == "clean")
                {
                    Logging.Handler.Debug("Main", "Starting health agent service", "");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Starting health agent service.");
                    Helper.Service.Start("NetLock_RMM_Health_Agent_Windows");
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
