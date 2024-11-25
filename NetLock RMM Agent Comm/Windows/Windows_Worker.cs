using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using _x101.HWID_System;
using Global.Helper;
using Microsoft.Extensions.Logging;
using NetLock_RMM_Agent_Comm;

namespace Windows.Workers
{
    public class Windows_Worker : BackgroundService
    {
        private readonly ILogger<Windows_Worker> _logger;
        private IHostApplicationLifetime _lifetime;

        public Windows_Worker(ILogger<Windows_Worker> logger, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;
        }
        
        // Timers
        public static System.Timers.Timer start_timer;
        public static System.Timers.Timer sync_timer;
        public static System.Timers.Timer events_timer;
        public static System.Timers.Timer microsoft_defender_antivirus_events_timer;
        public static System.Timers.Timer microsoft_defender_antivirus_check_hourly_sig_updates_timer;
        public static System.Timers.Timer microsoft_defender_antivirus_scan_job_time_scheduler_timer;
        public static System.Timers.Timer jobs_time_scheduler_timer;
        public static System.Timers.Timer sensors_time_scheduler_timer;

        // Status
        //public static bool process_events = false; //Indicates if events should be processed. Its being locked by the client settings loader
        public static bool microsoft_defender_antivirus_events_crawling = false;
        public static bool microsoft_defender_antivirus_events_timer_running = false;
        public static bool microsoft_defender_antivirus_sig_updates_timer_running = false;
        public static bool microsoft_defender_antivirus_scan_job_time_scheduler_timer_running = false;
        public static bool jobs_time_scheduler_timer_running = false;
        public static bool sensors_time_scheduler_timer_running = false;

        //Policy
        public static string policy_antivirus_settings_json = string.Empty;
        public static string policy_antivirus_exclusions_json = string.Empty;
        public static string policy_antivirus_scan_jobs_json = string.Empty;
        public static string policy_antivirus_controlled_folder_access_folders_json = string.Empty;
        public static string policy_antivirus_controlled_folder_access_ruleset_json = string.Empty;
        public static string policy_sensors_json = string.Empty;
        public static string policy_jobs_json = string.Empty;

        // Microsoft Defender Antivirus
        public static string microsoft_defender_antivirus_notifications_json = string.Empty;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logging.Debug("Windows.ExecuteAsync", "Service started", "Service started");

            Logging.Check_Debug_Mode();
            //Health.Check_Directories();
            //Health.Check_Registry();
            //Health.Check_Firewall();
            //Health.Check_Databases();
            //Health.User_Process();

            // Check OS version (legacy code for Windows 7. Need to verify it's still working and not causing security issues)
            string osVersion = Environment.OSVersion.Version.ToString();

            if (!string.IsNullOrEmpty(osVersion))
            {
                char osVersionChar = osVersion[0];

                if (osVersionChar == '6')
                {
                    Logging.Debug("Windows.ExecuteAsync", "OS_Version", $"OS ({osVersionChar}) is old. Switching to compatibility mode.");
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                }
                else
                {
                    Logging.Debug("Windows.ExecuteAsync", "OS_Version", $"OS ({osVersionChar}) is new.");
                }
            }
            else
            {
                Logging.Debug("Windows.ExecuteAsync", "OS_Version", "OS version could not be determined.");
            }

            // Load server config
            if (Global.Initialization.Server_Config.Load(0) == "error") // 
            {
                Logging.Debug("Service.OnStart", "Server_Config_Handler.Load", "Failed to load server config");
                _lifetime.StopApplication();
            }

            // Setup virtual datatables
            //Initialization.Health.Setup_Events_Virtual_Datatable();

            // Setup local server
            _ = Task.Run(async () => await Local_Server_Start());


            // Setup synchronize timer
            try
            {
                sync_timer = new System.Timers.Timer(600000); //sync 10 minutes
                sync_timer.Elapsed += new ElapsedEventHandler(Initialize_Timer_Tick);
                sync_timer.Enabled = true;
            }
            catch (Exception ex)
            {
                Logging.Error("Windows.ExecuteAsync", "Start sync_timer", ex.ToString());
            }

            //Start Init Timer. We are doing this to get the service instantly running on service manager. Afterwards we will dispose the timer in Synchronize function
            try
            {
                start_timer = new System.Timers.Timer(2500);
                start_timer.Elapsed += new ElapsedEventHandler(Initialize_Timer_Tick);
                start_timer.Enabled = true;
            }
            catch (Exception ex)
            {
                Logging.Debug("Windows.ExecuteAsync", "Start start_timer", ex.ToString());
            }

            //Start events timer (testing it to run at the end, to prevent a locked service)
            try
            {
                events_timer = new System.Timers.Timer(10000);
                events_timer.Elapsed += new ElapsedEventHandler(Process_Events_Timer_Tick);
                events_timer.Enabled = true;
            }
            catch (Exception ex)
            {
                Logging.Error("Windows.ExecuteAsync", "Start Event_Processor_Timer", ex.ToString());
            }

            /*while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }*/
        }

        private async void Initialize_Timer_Tick(object sender, ElapsedEventArgs e)
        {
            await Initialize(false);
        }

        private async Task Initialize(bool forced)
        {
            Logging.Debug("Windows.Windows_Worker.Initialize", "Initialize", "Start");

            //Disable start timer to prevent concurrent executions
            if (start_timer.Enabled)
                start_timer.Dispose();

            // Check if connection to communication server is available
            await Global.Initialization.Check_Connection.Check_Servers();

            // Online mode
            if (Device_Worker.communication_server_status)
            {
                Logging.Debug("Windows.Windows_Worker.Initialize", "connection_status", "Online mode.");

                //Force client sync if settings are missing
                if (File.Exists(Application_Paths.program_data_netlock_policy_database) == false || File.Exists(Application_Paths.program_data_netlock_events_database) == false)
                {
                    //Initialization.Database.NetLock_Events_Setup(); //Create events database if its not existing (cause it was deleted somehow)
                    forced = true;
                }

                //If first run, skip module init (pre boot) and load client settings first
                if (File.Exists(Application_Paths.just_installed) == false && forced == false && Device_Worker.first_sync == true) //Enable the Preboot Modules to block shit on system boot
                    Pre_Boot();
                if (File.Exists(Application_Paths.just_installed) && forced == false) //Force the sync & set the config because its the first run (justinstalled.txt)
                    forced = true;
                else if (Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_reg_path, "Synced") == "0" || Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_reg_path, "Synced") == null)
                    forced = true;

                // Check version
                bool up2date = await Global.Initialization.Version.Check_Version();

                if (up2date) // No update required. Continue logic
                {
                    // Authenticate online
                    string auth_result = await Global.Online_Mode.Handler.Authenticate();

                    // Check authorization status
                    // if (auth_result == "authorized" || auth_result == "not_synced" || auth_result == "synced")
                    if (Device_Worker.authorized)
                    {
                        // Update device information
                        await Global.Online_Mode.Handler.Update_Device_Information();
                    }

                    // Check sync status
                    if (Device_Worker.authorized && auth_result == "not_synced" || Device_Worker.authorized && forced)
                    {
                        // Set synced flag in registry to not synced
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_reg_path, "Synced", "0");

                        // Sync
                        await Global.Online_Mode.Handler.Policy();

                        // Sync done. Set synced flag in registry to prevent re-sync
                        Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_reg_path, "Synced", "1");
                    }
                    else if (Device_Worker.authorized && auth_result == "synced")
                    {
                        // placeholder, nothing to do here right now
                    }
                }
                else // Outdated. Trigger update
                {
                    await Global.Initialization.Version.Update();
                }
            }
            else // Offline mode
            {
                Global.Offline_Mode.Handler.Policy();
            }

            // Trigger module handler
            Module_Handler();
        }

        private void Pre_Boot()
        {
            if (Device_Worker.authorized)
            {
                Logging.Debug("Service.Pre_Boot", "", "Authorized.");
                //prepare_rulesets();
                Global.Offline_Mode.Handler.Policy();
                Module_Handler();
            }
            else if (!Device_Worker.authorized)
            {
                Logging.Debug("Service.Pre_Boot", "", "Not authorized.");
            }
        }

        private void Module_Handler()
        {
            Logging.Debug("Service.Module_Handler", "Start", "Module_Handler");

            if (!Device_Worker.authorized)
                return;

            // Antivirus
            Microsoft_Defender_Antivirus.Handler.Initalization();

            //Start Windows Defender AntiVirus Event timer, trigger every ten seconds
            try
            {
                if (!microsoft_defender_antivirus_events_timer_running)
                {
                    microsoft_defender_antivirus_events_timer = new System.Timers.Timer(10000); //Check every ten seconds
                    microsoft_defender_antivirus_events_timer.Elapsed += new ElapsedEventHandler(Microsoft_Defender_Antivirus.Handler.Events_Tick);
                    microsoft_defender_antivirus_events_timer.Enabled = true;
                    microsoft_defender_antivirus_events_timer_running = true;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Module_Handler", "Start microsoft_defender_antivirus_events_timer", ex.ToString());
            }

            //Start Windows Defender AntiVirus: Check hourly for sig updates timer
            try
            {
                if (!microsoft_defender_antivirus_sig_updates_timer_running)
                {
                    microsoft_defender_antivirus_check_hourly_sig_updates_timer = new System.Timers.Timer(3600000); //Check every 60 minutes
                    microsoft_defender_antivirus_check_hourly_sig_updates_timer.Elapsed += new ElapsedEventHandler(Microsoft_Defender_Antivirus.Handler.Check_Hourly_Sig_Updates_Tick);
                    microsoft_defender_antivirus_check_hourly_sig_updates_timer.Enabled = true;
                    microsoft_defender_antivirus_sig_updates_timer_running = true;

                    // Trigger the first check
                    Microsoft_Defender_Antivirus.Handler.Check_Hourly_Sig_Updates();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("OnStart", "Start Windows_Defender_Check_Hourly_Sig_Updates_Timer", ex.ToString());
            }

            //Start Windows Defender AntiVirus Scan Job Timer, trigger every ten seconds
            try
            {
                if (!microsoft_defender_antivirus_scan_job_time_scheduler_timer_running)
                {
                    microsoft_defender_antivirus_scan_job_time_scheduler_timer = new System.Timers.Timer(30000); //Check every thirty seconds
                    microsoft_defender_antivirus_scan_job_time_scheduler_timer.Elapsed += new ElapsedEventHandler(Microsoft_Defender_Antivirus.Handler.Scan_Job_Scheduler_Tick);
                    microsoft_defender_antivirus_scan_job_time_scheduler_timer.Enabled = true;
                    microsoft_defender_antivirus_scan_job_time_scheduler_timer_running = true;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Module_Handler", "Start microsoft_defender_antivirus_events_timer", ex.ToString());
            }

            // IF THE DEFENDER IS SCANNING, THIS WILL DELAY THE TIMER EXECUTION BELOW


            //Start jobs timer, trigger every thirty seconds
            try
            {
                if (!jobs_time_scheduler_timer_running)
                {
                    jobs_time_scheduler_timer = new System.Timers.Timer(30000); //Check every thirty seconds
                    jobs_time_scheduler_timer.Elapsed += new ElapsedEventHandler(Jobs.Handler.Scheduler_Tick);
                    jobs_time_scheduler_timer.Enabled = true;
                    jobs_time_scheduler_timer_running = true;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Module_Handler", "Start jobs_time_scheduler_timer", ex.ToString());
            }

            //Start sensors timer, trigger every thirty seconds
            try
            {
                if (!sensors_time_scheduler_timer_running)
                {
                    sensors_time_scheduler_timer = new System.Timers.Timer(30000); //Check every thirty seconds
                    sensors_time_scheduler_timer.Elapsed += new ElapsedEventHandler(Sensors.Handler.Scheduler_Tick);
                    sensors_time_scheduler_timer.Enabled = true;
                    sensors_time_scheduler_timer_running = true;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Module_Handler", "Start jobs_time_scheduler_timer", ex.ToString());
            }

            Logging.Debug("Service.Module_Handler", "Stop", "Module_Handler");
        }

        private async void Process_Events_Timer_Tick(object source, ElapsedEventArgs e)
        {
            Logging.Debug("Service.Process_Events_Tick", "Start", "");

            if (Device_Worker.authorized && Device_Worker.events_processing == false)
            {
                Device_Worker.events_processing = true;

                Global.Events.Logger.Consume_Events();
                await Global.Events.Logger.Process_Events();

                Device_Worker.events_processing = false;
            }

            Logging.Debug("Service.Process_Events_Tick", "Stop", "");
        }


        #region Local Server

        private const int Port = 7337;
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private async Task Local_Server_Start()
        {
            try
            {
                Logging.Local_Server("Service.Local_Server_Start", "Start", "Starting server...");
                TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
                listener.Start();
                Logging.Local_Server("Service.Local_Server_Start", "Start", "Server started. Waiting for connection...");

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    if (client != null)
                    {
                        _ = Local_Server_Handle_Client(client, _cancellationTokenSource.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Local_Server_Start", "Start", ex.ToString());
            }
        }

        private async Task Local_Server_Handle_Client(TcpClient client, CancellationToken cancellationToken)
        {
            Logging.Local_Server("Service.Local_Server_Handle_Client", "Start", "Handling client...");

            _client = client;
            _stream = _client.GetStream();

            byte[] buffer = new byte[2096];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0) // Client disconnected
                        break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Logging.Local_Server("Service.Local_Server_Handle_Client", "Start", $"Received message: {message}");

                    // Process the message and optionally send a response
                    await Local_Server_Send_Message("Message received.");

                    if (message == "get_device_identity")
                    {
                        Logging.Local_Server("Service.Local_Server_Handle_Client", "Get device identity", $"device_identity${Device_Worker.device_identity_json}${Global.Configuration.Agent.ssl}${Device_Worker.remote_server}${Device_Worker.file_server}");

                        if (!string.IsNullOrEmpty(Device_Worker.device_identity_json))
                            await Local_Server_Send_Message($"device_identity${Device_Worker.device_identity_json}${Global.Configuration.Agent.ssl}${Device_Worker.remote_server}${Device_Worker.file_server}");
                    }

                    // Force sync
                    if (message == "sync")
                    {
                        Logging.Local_Server("Service.Local_Server_Handle_Client", "Sync requested.", "");
                        await Initialize(true);
                    }

                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Local_Server_Handle_Client", "Error", ex.ToString());
            }
            finally
            {
                _stream.Close();
                _client.Close();
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
                    Logging.Local_Server("Service.Local_Server_Handle_Client", "Start", $"Sent message: {message}");
                }
                else
                    Logging.Local_Server("Service.Local_Server_Handle_Client", "Start", "No client connected to send the message.");
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Local_Server_Handle_Client", "Start", ex.ToString());
            }
        }

        #endregion
    }
}
