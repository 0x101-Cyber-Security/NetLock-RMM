using Global.Helper;
using Helper;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Timers;

namespace NetLock_RMM_Agent_Health
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public static bool debug_mode = false; //enables/disables logging

        public static string update_server = string.Empty;
        public static string trust_server = string.Empty;

        public static bool update_server_status = false;
        public static bool trust_server_status = false;

        int failed_count = 0;
        public static System.Timers.Timer check_health_timer;

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
        {
            bool first_run = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (first_run)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                        // Load server config
                        Server_Config.Load();

                        Check_Health();

                        check_health_timer = new System.Timers.Timer(60000); //Do every minute
                        check_health_timer.Elapsed += new ElapsedEventHandler(Tick);
                        check_health_timer.Enabled = true;

                        first_run = false;
                    }
                }
                
                await Task.Delay(1000, stoppingToken);
            }
            await Task.Delay(5000, stoppingToken);
        });

        private async void Check_Health()
        {
            Console.WriteLine("Check_Health");

            bool comm_agent_healthy = false;
            bool remote_agent_healthy = false;

            //If still not running, reinstall the client.
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    ServiceController sc = new ServiceController("NetLock_RMM_Agent_Comm");
                    if (sc.Status.Equals(ServiceControllerStatus.StartPending) || sc.Status.Equals(ServiceControllerStatus.Running))
                    {
                        Logging.Debug("Check_Health (NetLock_RMM_Agent_Comm)", "Running", "Installed & running.");
                        comm_agent_healthy = true;
                    }
                    else if (sc.Status.Equals(ServiceControllerStatus.Stopped) || sc.Status.Equals(ServiceControllerStatus.StopPending))
                    {
                        Logging.Debug("Check_Health (NetLock_RMM_Agent_Comm)", "Stopped|StopPending", "Installed, but stopped or stop is pending.");
                    }

                    sc = new ServiceController("NetLock_RMM_Agent_Remote");
                    if (sc.Status.Equals(ServiceControllerStatus.StartPending) || sc.Status.Equals(ServiceControllerStatus.Running))
                    {
                        Logging.Debug("Check_Health (NetLock_RMM_Agent_Remote)", "Running", "Installed & running.");
                        remote_agent_healthy = true;
                    }
                    else if (sc.Status.Equals(ServiceControllerStatus.Stopped) || sc.Status.Equals(ServiceControllerStatus.StopPending))
                    {
                        Logging.Debug("Check_Health (NetLock_RMM_Agent_Remote)", "Stopped|StopPending", "Installed, but stopped or stop is pending.");
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    string[] servicesToCheck = { "netlock-rmm-agent-comm", "netlock-rmm-agent-remote" };

                    foreach (var serviceName in servicesToCheck)
                    {
                        string serviceCommand = $"systemctl list-units --type=service --all | grep -w {serviceName}.service";

                        // Execute bash script and save the output
                        string output = Bash.Execute_Script("Service Sensor", false, serviceCommand);

                        if (string.IsNullOrWhiteSpace(output))
                        {
                            //Console.WriteLine($"Service {sensor_item.service_name} not found or no output received.");
                            continue;
                        }

                        bool isServiceRunning = false;

                        // Check status
                        // Use Regex to match running status
                        var match = Regex.Match(output, $@"running", RegexOptions.Multiline);

                        if (match.Success)
                            isServiceRunning = true;

                        if (isServiceRunning)
                        {
                            Logging.Debug($"Check_Health ({serviceName})", "Running", "Installed & running.");
                            if (serviceName == "netlock-rmm-agent-comm")
                                comm_agent_healthy = true;
                            else if (serviceName == "netlock-rmm-agent-remote")
                                remote_agent_healthy = true;
                        }
                        else
                        {
                            Logging.Debug($"Check_Health ({serviceName})", "Stopped", "Installed, but stopped.");
                        }
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    string[] servicesToCheck = { "netlock-rmm-agent-comm", "netlock-rmm-agent-remote" };

                    foreach (var serviceName in servicesToCheck)
                    {
                        string serviceCommand = $"launchctl list | grep -w {serviceName}";

                        // Execute bash script and save the output
                        string output = Bash.Execute_Script("Service Health Check", false, serviceCommand);

                        if (!string.IsNullOrEmpty(output))
                        {
                            // Check if the service is running by inspecting the output
                            if (output.Contains("PID") || output.Contains("running", StringComparison.OrdinalIgnoreCase))
                            {
                                Logging.Debug($"Check_Health ({serviceName})", "Running", "Installed & running.");
                                if (serviceName == "netlock-rmm-agent-comm")
                                    comm_agent_healthy = true;
                                else if (serviceName == "netlock-rmm-agent-remote")
                                    remote_agent_healthy = true;
                            }
                            else
                            {
                                Logging.Debug($"Check_Health ({serviceName})", "Stopped", "Installed, but stopped.");
                            }
                        }
                        else
                        {
                            Logging.Debug($"Check_Health ({serviceName})", "Unknown", $"Service {serviceName} not found or in an unknown state.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Debug("Check_Health", "Catch", "Not installed or error: " + ex.Message);
                comm_agent_healthy = false;
                remote_agent_healthy = false;
            }

            //If service is healthy do nothing, else reinstall the service
            if (!comm_agent_healthy || !remote_agent_healthy)
            {
                failed_count++;

                Logging.Debug("Check_Health", "Service_Healthy", "False. Try to start service. Failed " + failed_count + " times already.");

                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        ServiceController sc;

                        if (!comm_agent_healthy)
                        {
                            sc = new ServiceController("NetLock_RMM_Agent_Comm");
                            sc.Start();
                            failed_count = 0;

                            Logging.Debug("Check_Health", "Service_Healthy", "Service started.");
                        }

                        if (!remote_agent_healthy)
                        {
                            sc = new ServiceController("NetLock_RMM_Agent_Remote");
                            sc.Start();
                            failed_count = 0;

                            Logging.Debug("Check_Health", "Service_Healthy", "Service started.");
                        }
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        if (!comm_agent_healthy)
                        {
                            // Start the service
                            Bash.Execute_Script("Service Start", false, "systemctl start netlock-rmm-agent-comm.service");

                            // Check status
                            string output = Bash.Execute_Script("Service status", false, "systemctl list-units --type=service --all | grep -w netlock-rmm-agent-comm.service");

                            Logging.Debug("Check_Health", "Service status output", output);

                            if (string.IsNullOrWhiteSpace(output))
                            {
                                Logging.Debug("Check_Health", "Service status output", "Service not found or no output received.");
                                //Console.WriteLine($"Service {sensor_item.service_name} not found or no output received.");
                                return;
                            }

                            // Check status
                            // Use Regex to match running status
                            var match = Regex.Match(output, $@"running", RegexOptions.Multiline);

                            if (match.Success)
                                failed_count = 0;

                            Logging.Debug("Check_Health", "Service status match", match.Success.ToString());
                        }

                        if (!remote_agent_healthy)
                        {
                            // Start the service
                            Bash.Execute_Script("Service Start", false, "systemctl start netlock-rmm-agent-remote.service");
                            
                            // Check status
                            string output = Bash.Execute_Script("Service Sensor", false, "systemctl list-units --type=service --all | grep -w netlock-rmm-agent-remote.service");
                            
                            if (string.IsNullOrWhiteSpace(output))
                            {
                                //Console.WriteLine($"Service {sensor_item.service_name} not found or no output received.");
                                return;
                            }
                                                        
                            // Check status
                            // Use Regex to match running status
                            var match = Regex.Match(output, $@"running", RegexOptions.Multiline);
                         
                            if (match.Success)
                                failed_count = 0;

                            Logging.Debug("Check_Health", "Service status match", match.Success.ToString());
                        }
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        if (!comm_agent_healthy)
                        {
                            // Start the service
                            MacOS.Helper.Zsh.Execute_Script("Service Start", false, "launchctl start netlock-rmm-agent-comm");

                            string output = MacOS.Helper.Zsh.Execute_Script("Service Sensor", false, $"launchctl list | grep netlock-rmm-agent-comm");

                            Logging.Debug("Check_Health", "Service status output", output);

                            // Regex to extract only the line for the specific service
                            string pattern = $@"^\S+\s+\S+\s+netlock-rmm-agent-comm$";
                            var match = Regex.Match(output, pattern, RegexOptions.Multiline);

                            bool isServiceRunning = false;

                            if (match.Success)
                            {
                                // Extrahiere den PID-Wert oder das "-" am Anfang der Zeile
                                string[] parts = match.Value.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                isServiceRunning = parts[0] != "-"; // "-" means the service is not running
                            }

                            if (isServiceRunning)
                                failed_count = 0;

                            Logging.Debug("Check_Health", "Service status match", match.Success.ToString());
                        }

                        if (!remote_agent_healthy)
                        {
                            // Start the service
                            MacOS.Helper.Zsh.Execute_Script("Service Start", false, "launchctl start netlock-rmm-agent-remote");
                         
                            string output = MacOS.Helper.Zsh.Execute_Script("Service Sensor", false, $"launchctl list | grep netlock-rmm-agent-remote");

                            Logging.Debug("Check_Health", "Service status output", output);

                            // Regex to extract only the line for the specific service
                            string pattern = $@"^\S+\s+\S+\s+netlock-rmm-agent-remote$";
                            var match = Regex.Match(output, pattern, RegexOptions.Multiline);
                            
                            bool isServiceRunning = false;

                            if (match.Success)
                            {
                                // Extract the PID value or the `-` at the beginning of the line
                                string[] parts = match.Value.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                isServiceRunning = parts[0] != "-"; // "-" means the service is not running
                            }

                            if (isServiceRunning)
                                failed_count = 0;

                            Logging.Debug("Check_Health", "Service status match", match.Success.ToString());
                        }
                    }

                    Logging.Debug("Check_Health", "Service_Healthy", "Service started.");
                }
                catch (Exception ex)
                {
                    Logging.Debug("Check_Health", "Service_Healthy", "False. Failed to start: " + ex.ToString());
                }
            }

            if (failed_count == 3 || failed_count > 3)
            {
                Logging.Debug("Check_Health", "Service_Healthy", "Not healthy. Attempting reinstallation.");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Service not healthy. Attempting reinstallation.");
                await Global.Helper.Check_Connection.Check_Servers();

                // Check if online
                if (!update_server_status || !trust_server_status)
                {
                    Logging.Debug("Check_Health", "Check_Servers", "Failed. Update server: " + update_server_status + " Trust server: " + trust_server_status);
                    Logging.Debug("Check_Health", "Check_Servers", "Failed. Cannot connect to update or trust server.");
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Update server: " + update_server_status + " Trust server: " + trust_server_status);
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Cannot connect to update or trust server.");
                    return;
                }

                // If installer exists, delete it and download the new one.
                Delete_Installer();

                // Create installer directory
                if (!Directory.Exists(Application_Paths.c_temp_netlock_installer_dir))
                    Directory.CreateDirectory(Application_Paths.c_temp_netlock_installer_dir);

                // Check OS & Architecture
                string installer_package_url = string.Empty;

                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Detecting OS & Architecture.");
                Logging.Debug("Main", "Detecting OS & Architecture", "");

                var arch = RuntimeInformation.ProcessArchitecture;

                if (OperatingSystem.IsWindows())
                {
                    if (arch == Architecture.X64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Windows x64 detected.");
                        Logging.Debug("Main", "Windows x64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_winx64;
                    }
                    else if (arch == Architecture.Arm64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Windows ARM64 detected.");
                        Logging.Debug("Main", "Windows ARM64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_winarm64;
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    if (arch == Architecture.X64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Linux x64 detected.");
                        Logging.Debug("Main", "Linux x64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_linuxx64;
                    }
                    else if (arch == Architecture.Arm64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Linux ARM64 detected.");
                        Logging.Debug("Main", "Linux ARM64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_linuxarm64;
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    if (arch == Architecture.X64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> MacOS x64 detected.");
                        Logging.Debug("Main", "MacOS x64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_osx64;
                    }
                    else if (arch == Architecture.Arm64)
                    {
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> MacOS ARM64 detected.");
                        Logging.Debug("Main", "MacOS ARM64 detected", "");
                        installer_package_url = Application_Paths.installer_package_url_osxarm64;
                    }
                }
                else
                {
                    Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Unsupported OS & Architecture.");
                    Logging.Error("Main", "Unsupported OS & Architecture", "");
                    Thread.Sleep(5000);
                    return;
                }

                // Download Installer
                bool http_status = true;

                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Downloading installer package: " + update_server + installer_package_url);
                Logging.Debug("Main", "Downloading installer package", update_server + installer_package_url);

                // Download comm agent package
                http_status = await Http.DownloadFileAsync(Global.Configuration.Agent.ssl, update_server + installer_package_url, Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.installer_package_path), Global.Configuration.Agent.package_guid);
                Logging.Debug("Main", "Download installer package", http_status.ToString());

                // Get hash uninstaller
                string installer_hash = await Http.GetHashAsync(Global.Configuration.Agent.ssl, trust_server + installer_package_url + ".sha512", Global.Configuration.Agent.package_guid);
                Logging.Debug("Main", "Get hash installer", installer_hash);

                if (http_status && !String.IsNullOrEmpty(installer_hash) && IO.Get_SHA512(Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.installer_package_path)) == installer_hash)
                {
                    Logging.Debug("Main", "Check hash installer package", "OK");

                    // Extract the installer
                    try
                    {
                        ZipFile.ExtractToDirectory(Path.Combine(Application_Paths.c_temp_netlock_dir, Application_Paths.installer_package_path), Application_Paths.c_temp_netlock_installer_dir);
                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Extract installer package: OK");
                        Logging.Debug("Main", "Extract installer package", "OK");

                        Logging.Debug("Main", "Argument", $"fix \"{Application_Paths.program_data_server_config_json}\"");

                        // Run the installer
                        if (OperatingSystem.IsWindows())
                        {
                            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Run installer: Windows");
                            Logging.Debug("Main", "Run installer", "Windows");
                            Process installer = new Process();
                            installer.StartInfo.FileName = Application_Paths.c_temp_netlock_installer_path;
                            installer.StartInfo.ArgumentList.Add("fix");
                            installer.StartInfo.ArgumentList.Add(Application_Paths.program_data_server_config_json);
                            installer.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                            installer.Start();
                            await installer.WaitForExitAsync();
                        }
                        else if (OperatingSystem.IsLinux())
                        {
                            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Run installer: Linux");
                            Logging.Debug("Main", "Run installer", "Linux");

                            // Create installer
                            Linux.Helper.Linux.CreateInstallerService();

                            Process installer = new Process();
                            installer.StartInfo.FileName = "/bin/bash";
                            installer.StartInfo.ArgumentList.Add("-c");
                            installer.StartInfo.ArgumentList.Add($"systemctl start netlock-rmm-agent-installer");
                            installer.StartInfo.RedirectStandardOutput = true;
                            installer.StartInfo.RedirectStandardError = true;
                            installer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            installer.Start();
                            await installer.WaitForExitAsync();
                        }
                        else if (OperatingSystem.IsMacOS())
                        {
                            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Run installer: MacOS");
                            Logging.Debug("Main", "Run installer", "MacOS");

                            // Create installer service
                            MacOS.Helper.MacOS.CreateInstallerService();

                            Process installer = new Process();
                            installer.StartInfo.FileName = "zsh";
                            installer.StartInfo.ArgumentList.Add("-c");
                            installer.StartInfo.ArgumentList.Add($"sudo launchctl load -w /Library/LaunchDaemons/com.netlock.rmm.installer.plist");
                            installer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            installer.StartInfo.RedirectStandardOutput = true;
                            installer.StartInfo.RedirectStandardError = true;
                            installer.Start();
                            await installer.WaitForExitAsync();
                        }

                        Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Run installer: OK");
                        Logging.Debug("Main", "Run installer", "OK");
                    }
                    catch (Exception ex)
                    {
                        Logging.Debug("Main", "Extract installer package", "Failed: " + ex.ToString());
                        return;
                    }
                }
                else
                {
                    Logging.Debug("Main", "Download installer package", "Failed.");
                    return;
                }

                failed_count = 0;
            }
        }

        private void Tick(object source, ElapsedEventArgs e)
        {
            Check_Health();
        }

        private void Delete_Installer()
        {
            try
            {
                if (Directory.Exists(Application_Paths.c_temp_netlock_installer_dir))
                    Directory.Delete(Application_Paths.c_temp_netlock_installer_dir, true);
            }
            catch (Exception ex)
            {
                Logging.Debug("Check_Health", "Installer_Check", "Couldn't check or delete the installer: " + ex.Message);
            }
        }
    }
}
