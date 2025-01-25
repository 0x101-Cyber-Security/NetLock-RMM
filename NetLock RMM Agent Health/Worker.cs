using Global.Helper;
using System.Diagnostics;
using System.IO.Compression;
using System.Timers;
using System.ServiceProcess;

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

        int failed_count = 0;
        public static System.Timers.Timer check_health_timer;

        // Server config
        public static bool ssl = false;
        public static string package_guid = String.Empty;
        public static string update_servers = String.Empty;
        public static string trust_servers = String.Empty;
        public static string tenant_guid = String.Empty;
        public static string location_guid = String.Empty;
        public static string language = String.Empty;
        public static string access_key = String.Empty;
        public static bool authorized = false;

        // Server communication
        public static string update_server = String.Empty;
        public static string trust_server = String.Empty;
        public static bool communication_server_status = false;
        public static bool remote_server_status = false;
        public static bool trust_server_status = false;
        public static bool update_server_status = false;
        public static string http_https = String.Empty;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        private void Init()
        {
            // Load server config
            Server_Config.Load();

            Check_Health();

            check_health_timer = new System.Timers.Timer(300000); //Do every 5 minutes
            check_health_timer.Elapsed += new ElapsedEventHandler(Tick);
            check_health_timer.Enabled = true;
        }

        private async void Check_Health()
        {
            bool comm_agent_healthy = false;
            bool remote_agent_healthy = false;

            //If still not running, reinstall the client.
            try
            {
                ServiceController sc = new ServiceController("NetLock_RMM_Comm_Agent_Windows");
                if (sc.Status.Equals(ServiceControllerStatus.StartPending) || sc.Status.Equals(ServiceControllerStatus.Running))
                {
                    Logging.Debug("Check_Health (NetLock_RMM_Comm_Agent_Windows)", "Running", "Installed & running.");
                    comm_agent_healthy = true;
                }
                else if (sc.Status.Equals(ServiceControllerStatus.Stopped) || sc.Status.Equals(ServiceControllerStatus.StopPending))
                {
                    Logging.Debug("Check_Health (NetLock_RMM_Comm_Agent_Windows)", "Stopped|StopPending", "Installed, but stopped or stop is pending.");
                }

                sc = new ServiceController("NetLock_RMM_Remote_Agent_Windows");
                if (sc.Status.Equals(ServiceControllerStatus.StartPending) || sc.Status.Equals(ServiceControllerStatus.Running))
                {
                    Logging.Debug("Check_Health (NetLock_RMM_Remote_Agent_Windows)", "Running", "Installed & running.");
                    remote_agent_healthy = true;
                }
                else if (sc.Status.Equals(ServiceControllerStatus.Stopped) || sc.Status.Equals(ServiceControllerStatus.StopPending))
                {
                    Logging.Debug("Check_Health (NetLock_RMM_Remote_Agent_Windows)", "Stopped|StopPending", "Installed, but stopped or stop is pending.");
                }
            }
            catch (Exception ex)
            {
                Logging.Debug("Check_Health", "Catch", "Not installed or error: " + ex.Message);
            }

            //If service is healthy do nothing, else reinstall the service
            if (!comm_agent_healthy || !remote_agent_healthy)
            {
                failed_count++;

                Logging.Debug("Check_Health", "Service_Healthy", "False. Try to start service. Failed " + failed_count + " times already.");

                try
                {
                    ServiceController sc;

                    if (!comm_agent_healthy)
                    {
                        sc = new ServiceController("NetLock_RMM_Comm_Agent_Windows");
                        sc.Start();
                        failed_count = 0;
                    }

                    if (!remote_agent_healthy)
                    {
                        sc = new ServiceController("NetLock_RMM_Remote_Agent_Windows");
                        sc.Start();
                        failed_count = 0;
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
                await Global.Helper.Check_Connection.Check_Servers();

                // If installer exists, delete it and download the new one.
                Delete_Installer();

                // Create installer directory
                if (!Directory.Exists(Application_Paths.c_temp_netlock_installer_dir))
                    Directory.CreateDirectory(Application_Paths.c_temp_netlock_installer_dir);

                // Download Installer
                bool http_status = true;

                // Download comm agent package
                http_status = await Helper.Http.DownloadFileAsync(ssl, update_server + Application_Paths.installer_package_url, Application_Paths.c_temp_netlock_dir + Application_Paths.installer_package_path, package_guid);
                Logging.Debug("Main", "Download installer package", http_status.ToString());

                // Get hash uninstaller
                string installer_hash = await Helper.Http.GetHashAsync(ssl, trust_server + Application_Paths.installer_package_url + ".sha512", package_guid);
                Logging.Debug("Main", "Get hash installer", installer_hash);

                if (http_status && !String.IsNullOrEmpty(installer_hash) && Helper.IO.Get_SHA512(Application_Paths.c_temp_netlock_dir + Application_Paths.installer_package_path) == installer_hash)
                {
                    Logging.Debug("Main", "Check hash installer package", "OK");

                    // Extract the installer
                    try
                    {
                        ZipFile.ExtractToDirectory(Application_Paths.c_temp_netlock_dir + Application_Paths.installer_package_path, Application_Paths.c_temp_netlock_installer_dir);
                        Logging.Debug("Main", "Extract installer package", "OK");

                        Logging.Debug("Main", "Argument", "fix " + Application_Paths.program_data_server_config_json);

                        // Run the installer
                        Process installer = new Process();
                        installer.StartInfo.FileName = Application_Paths.c_temp_netlock_installer_path;
                        installer.StartInfo.Arguments = $"fix \"{Application_Paths.program_data_server_config_json}\"";
                        installer.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        installer.Start();
                        installer.WaitForExit();

                        Logging.Debug("Main", "Run installer", "OK");
                    }
                    catch (Exception ex)
                    {
                        Logging.Debug("Main", "Extract installer package", "Failed: " + ex.Message);
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
