using Global.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NetLock_RMM_Agent_Comm
{
    public class Device_Worker : BackgroundService
    {
        private readonly ILogger<Device_Worker> _logger;
        private IHostApplicationLifetime _lifetime;

        public Device_Worker(ILogger<Device_Worker> logger, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;
        }

        // Server config
        public static string access_key = string.Empty;
        public static bool authorized = false;

        // Server communication
        public static string communication_server = string.Empty;
        public static string remote_server = string.Empty;
        public static string update_server = string.Empty;
        public static string trust_server = string.Empty;
        public static string file_server = string.Empty;
        public static bool communication_server_status = false;
        public static bool remote_server_status = false;
        public static bool trust_server_status = false;
        public static bool update_server_status = false;
        public static bool file_server_status = false;

        // Device identity
        public static string device_identity_json = string.Empty;

        // Device information
        public static string ip_address_internal = string.Empty;
        public static string operating_system = string.Empty;
        public static string domain = string.Empty;
        public static string antivirus_solution = string.Empty;
        public static bool firewall_status = false;
        public static string architecture = string.Empty;
        public static string last_boot = string.Empty;
        public static string timezone = string.Empty;
        public static string cpu = string.Empty;
        public static string cpu_usage = string.Empty;
        public static string mainboard = string.Empty;
        public static string gpu = string.Empty;
        public static string ram = string.Empty;
        public static string ram_usage = string.Empty;
        public static string tpm = string.Empty;
        public static string last_active_user = string.Empty;

        // Device information - Detailed (we use it to hold a history and make sure we do not send the same information multiple times)
        public static string processesJson = string.Empty;
        public static string cpuInformationJson = string.Empty;
        public static string ramInformationJson = string.Empty;
        public static string networkAdaptersJson = string.Empty;
        public static string disksJson = string.Empty;
        public static string antivirusProductsJson = string.Empty;
        public static string applicationsInstalledJson = string.Empty;
        public static string applicationsLogonJson = string.Empty;
        public static string applicationsScheduledTasksJson = string.Empty;
        public static string applicationsServicesJson = string.Empty;
        public static string applicationsDriversJson = string.Empty;
        public static string antivirusInformationJson = string.Empty;
        public static string cronjobsJson = string.Empty;

        //Datatables
        public static DataTable events_data_table = new DataTable();

        // Status
        public static bool connection_status = false;
        public static bool first_sync = true;
        public static bool sync_active = true;
        public static bool events_processing = false; //Tells that the events are currently being processed and tells the Init to wait until its finished

        public static bool jobs_time_scheduler_timer_running = false;
        public static bool sensors_time_scheduler_timer_running = false;

        // Policy
        public static string policy_sensors_json = string.Empty;
        public static string policy_jobs_json = string.Empty;

        // Timers
        public static System.Timers.Timer start_timer;
        public static System.Timers.Timer sync_timer;
        public static System.Timers.Timer events_timer;
        public static System.Timers.Timer jobs_time_scheduler_timer;
        public static System.Timers.Timer sensors_time_scheduler_timer;
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool firstRun = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (firstRun)
                {
                    await InitializeOnFirstRun(stoppingToken); // Initialize all one-off tasks without blocking the main loop
                    firstRun = false;
                }

                await Task.Delay(5000, stoppingToken);
            }
        }

        /// <summary>
        /// Initialises all one-off tasks without blocking the main loop.
        /// </summary>
        private async Task InitializeOnFirstRun(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Device worker first run: {time}", DateTimeOffset.Now);

            Logging.Debug("Device_Worker.ExecuteAsync", "Service started", "Service started");

            // Setup local server
            _ = Task.Run(async () => await Local_Server_Start());

            //Start events timer (testing it to run at the end, to prevent a locked service)
            try
            {
                events_timer = new System.Timers.Timer(10000);
                events_timer.Elapsed += new ElapsedEventHandler(Process_Events_Timer_Tick);
                events_timer.Enabled = true;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Worker.ExecuteAsync", "Start Event_Processor_Timer", ex.ToString());
            }

            // Setup synchronize timer
            try
            {
                sync_timer = new System.Timers.Timer(1800000); //sync 30 minutes | currently testing with lower value
                sync_timer.Elapsed += new ElapsedEventHandler(Initialize_Timer_Tick);
                sync_timer.Enabled = true;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Worker.ExecuteAsync", "Start sync_timer", ex.ToString());
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
                Logging.Debug("Device_Worker.ExecuteAsync", "Start start_timer", ex.ToString());
            }
        }

        private async void Process_Events_Timer_Tick(object source, ElapsedEventArgs e)
        {
            Logging.Debug("Device_Worker.Process_Events_Tick", "Start", "");

            if (authorized && events_processing == false)
            {
                events_processing = true;

                Global.Events.Logger.Consume_Events();
                await Global.Events.Logger.Process_Events();

                events_processing = false;
            }

            Logging.Debug("Device_Worker.Process_Events_Tick", "Stop", "");
        }

        private async void Initialize_Timer_Tick(object sender, ElapsedEventArgs e)
        {
            await Initialize(false);
        }

        private async Task Initialize(bool forced)
        {
            try
            {
                Logging.Debug("Device_Worker.Initialize", "Initialize", "Start");

                //Disable start timer to prevent concurrent executions
                if (start_timer.Enabled)
                    start_timer.Dispose();

                // Check if connection to communication server is available
                await Global.Initialization.Check_Connection.Check_Servers();

                Logging.Debug("Device_Worker.Initialize", "", "COMM SERVER: " + Device_Worker.communication_server.ToString());
                Logging.Debug("Device_Worker.Initialize", "", "COMM server connection status: " + communication_server_status.ToString());

                // Online mode
                if (communication_server_status)
                {
                    Logging.Debug("Device_Worker.Initialize", "connection_status", "Online mode.");

                    //Force client sync if settings are missing
                    if (File.Exists(Application_Paths.program_data_netlock_policy_database) == false || File.Exists(Application_Paths.program_data_netlock_events_database) == false)
                    {
                        Global.Initialization.Database.NetLock_Events_Setup(); //Create events database if its not existing (cause it was deleted somehow)
                        forced = true;
                    }

                    //If first run, skip module init (pre boot) and load client settings first
                    if (File.Exists(Application_Paths.just_installed) == false && forced == false && first_sync == true) //Enable the Preboot Modules to block shit on system boot
                        Pre_Boot();
                    if (File.Exists(Application_Paths.just_installed) && forced == false) //Force the sync & set the config because its the first run (justinstalled.txt)
                        forced = true;
                    else if (Windows.Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_reg_path, "Synced") == "0" || Windows.Helper.Registry.HKLM_Read_Value(Application_Paths.netlock_reg_path, "Synced") == null)
                        forced = true;

                    Logging.Debug("Device_Worker.Initialize", "forced (sync)", forced.ToString());

                    // Check version
                    bool up2date = await Global.Initialization.Version.Check_Version();

                    if (up2date) // No update required. Continue logic
                    {
                        // Authenticate online
                        string auth_result = await Global.Online_Mode.Handler.Authenticate();

                        // Check authorization status
                        // if (auth_result == "authorized" || auth_result == "not_synced" || auth_result == "synced")
                        if (authorized)
                        {
                            Logging.Debug("Device_Worker.Initialize", "authorized", "Authorized. Update device information.");

                            // Update device information
                            await Global.Online_Mode.Handler.Update_Device_Information();
                        }

                        // Check sync status
                        if (authorized && auth_result == "not_synced" || authorized && forced)
                        {
                            // Set synced flag in registry to not synced
                            Windows.Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_reg_path, "Synced", "0");

                            // Sync
                            await Global.Online_Mode.Handler.Policy();

                            // Sync done. Set synced flag in registry to prevent re-sync
                            Windows.Helper.Registry.HKLM_Write_Value(Application_Paths.netlock_reg_path, "Synced", "1");

                            first_sync = false;
                        }
                        else if (authorized && auth_result == "synced")
                        {
                            // placeholder, nothing to do here right now
                            first_sync = false;
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
            catch (Exception ex)
            {
                Logging.Error("Device_Worker.Initialize", "Initialize", ex.ToString());
            }
        }

        private void Pre_Boot()
        {
            if (authorized)
            {
                Logging.Debug("Device_Worker.Pre_Boot", "", "Authorized.");
                //prepare_rulesets();
                Global.Offline_Mode.Handler.Policy();
                Module_Handler();
            }
            else if (!authorized)
            {
                Logging.Debug("Device_Worker.Pre_Boot", "", "Not authorized.");
            }
        }


        private void Module_Handler()
        {
            Logging.Debug("Device_Worker.Module_Handler", "Start", "Module_Handler");

            if (!authorized)
                return;

            //Start jobs timer, trigger every thirty seconds
            try
            {
                if (!jobs_time_scheduler_timer_running)
                {
                    jobs_time_scheduler_timer = new System.Timers.Timer(30000); //Check every thirty seconds
                    jobs_time_scheduler_timer.Elapsed += new ElapsedEventHandler(Global.Jobs.Handler.Scheduler_Tick);
                    jobs_time_scheduler_timer.Enabled = true;
                    jobs_time_scheduler_timer_running = true;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Worker.Module_Handler", "Start jobs_time_scheduler_timer", ex.ToString());
            }

            //Start sensors timer, trigger every thirty seconds
            try
            {
                if (!sensors_time_scheduler_timer_running)
                {
                    sensors_time_scheduler_timer = new System.Timers.Timer(30000); //Check every thirty seconds
                    sensors_time_scheduler_timer.Elapsed += new ElapsedEventHandler(Global.Sensors.Handler.Scheduler_Tick);
                    sensors_time_scheduler_timer.Enabled = true;
                    sensors_time_scheduler_timer_running = true;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Worker.Module_Handler", "Start sensors_time_scheduler_timer", ex.ToString());
            }
        }

        #region Local Server

        private const int Port = 7337;
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private async Task Local_Server_Start()
        {
            Console.WriteLine("Local server start");
            try
            {
                Console.WriteLine("Local server start try");
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
                Console.WriteLine("Local server start exception");
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
