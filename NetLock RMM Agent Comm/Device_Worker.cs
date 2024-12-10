using Global.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
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

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
        {
            bool first_run = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (first_run)
                {
                    _logger.LogInformation("Device worker first run: {time}", DateTimeOffset.Now);

                    Logging.Debug("Device_Worker.ExecuteAsync", "Service started", "Service started");

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
                        sync_timer = new System.Timers.Timer(600000); //sync 10 minutes
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

                    first_run = false;
                }
            }

            await Task.Delay(5000, stoppingToken);
        });

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
            Logging.Debug("Device_Worker.Initialize", "Initialize", "Start");

            //Disable start timer to prevent concurrent executions
            if (start_timer.Enabled)
                start_timer.Dispose();

            // Check if connection to communication server is available
            await Global.Initialization.Check_Connection.Check_Servers();

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
    }
}
