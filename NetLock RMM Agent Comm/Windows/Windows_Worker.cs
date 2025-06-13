using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using _x101.HWID_System;
using Global.Helper;
using Microsoft.Extensions.Logging;
using NetLock_RMM_Agent_Comm;

namespace NetLock_RMM_Agent_Comm
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

        public static System.Timers.Timer microsoft_defender_antivirus_events_timer;
        public static System.Timers.Timer microsoft_defender_antivirus_check_hourly_sig_updates_timer;
        public static System.Timers.Timer microsoft_defender_antivirus_scan_job_time_scheduler_timer;

        // Status
        //public static bool process_events = false; //Indicates if events should be processed. Its being locked by the client settings loader
        public static bool microsoft_defender_antivirus_events_crawling = false;
        public static bool microsoft_defender_antivirus_events_timer_running = false;
        public static bool microsoft_defender_antivirus_sig_updates_timer_running = false;
        public static bool microsoft_defender_antivirus_scan_job_time_scheduler_timer_running = false;
        
        //Policy
        public static string policy_antivirus_settings_json = string.Empty;
        public static string policy_antivirus_exclusions_json = string.Empty;
        public static string policy_antivirus_scan_jobs_json = string.Empty;
        public static string policy_antivirus_controlled_folder_access_folders_json = string.Empty;
        public static string policy_antivirus_controlled_folder_access_ruleset_json = string.Empty;
        
        // Microsoft Defender Antivirus
        public static string microsoft_defender_antivirus_notifications_json = string.Empty;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            bool firstRun = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (firstRun)
                {
                    await InitializeOnFirstRun(stoppingToken); // 
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
            _logger.LogInformation("Windows worker first run: {time}", DateTimeOffset.Now);
            Logging.Debug("Windows.ExecuteAsync", "Service started", "Service started");

            // Check OS version
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

            // Check directories and registration asynchronously
            Windows.Initialization.Health.Handler.Check_Directories();
            Windows.Initialization.Health.Handler.Check_Registry();
            Windows.Initialization.Health.Handler.User_Process();
            Windows.Initialization.Health.Handler.SaS();

            // Set up synchronisation timer
            try
            {
                sync_timer?.Dispose(); // Falls vorher schon ein Timer existierte, aufräumen
                sync_timer = new System.Timers.Timer(600000); // 10 Minuten
                sync_timer.Elapsed += Initialize_Timer_Tick;
                sync_timer.AutoReset = true;
                sync_timer.Enabled = true;
            }
            catch (Exception ex)
            {
                Logging.Error("Windows.ExecuteAsync", "Start sync_timer", ex.ToString());
            }

            // Start timer for immediate start
            try
            {
                start_timer?.Dispose();
                start_timer = new System.Timers.Timer(2500);
                start_timer.Elapsed += Initialize_Timer_Tick;
                start_timer.AutoReset = false; // Nur einmal ausführen
                start_timer.Enabled = true;
            }
            catch (Exception ex)
            {
                Logging.Debug("Windows.ExecuteAsync", "Start start_timer", ex.ToString());
            }
        }

        private async void Initialize_Timer_Tick(object sender, ElapsedEventArgs e)
        {
            await Initialize(false);
        }

        private async Task Initialize(bool forced)
        {
            Logging.Debug("Windows_Worker.Initialize", "Initialize", "Start");

            //Disable start timer to prevent concurrent executions
            if (start_timer.Enabled)
                start_timer.Dispose();

            // Trigger Pre_Boot
            Pre_Boot();

            // Trigger module handler
            Module_Handler();
        }

        private void Pre_Boot()
        {
            if (Device_Worker.authorized)
            {
                Logging.Debug("Windows_Worker.Pre_Boot", "", "Authorized.");

                // Load rulesets

                // Will be used later for windows features such as app control
            }
            else if (!Device_Worker.authorized)
            {
                Logging.Debug("Windows_Worker.Pre_Boot", "", "Not authorized.");
            }
        }

        private void Module_Handler()
        {
            Logging.Debug("Service.Module_Handler", "Start", "Module_Handler");

            if (!Device_Worker.authorized)
                return;

            // Antivirus
            Windows.Microsoft_Defender_Antivirus.Handler.Initalization();

            //Start Windows Defender AntiVirus Event timer, trigger every ten seconds
            try
            {
                if (!microsoft_defender_antivirus_events_timer_running)
                {
                    microsoft_defender_antivirus_events_timer = new System.Timers.Timer(10000); //Check every ten seconds
                    microsoft_defender_antivirus_events_timer.Elapsed += new ElapsedEventHandler(Windows.Microsoft_Defender_Antivirus.Handler.Events_Tick);
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
                    microsoft_defender_antivirus_check_hourly_sig_updates_timer.Elapsed += new ElapsedEventHandler(Windows.Microsoft_Defender_Antivirus.Handler.Check_Hourly_Sig_Updates_Tick);
                    microsoft_defender_antivirus_check_hourly_sig_updates_timer.Enabled = true;
                    microsoft_defender_antivirus_sig_updates_timer_running = true;

                    // Trigger the first check
                    Windows.Microsoft_Defender_Antivirus.Handler.Check_Hourly_Sig_Updates();
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
                    microsoft_defender_antivirus_scan_job_time_scheduler_timer.Elapsed += new ElapsedEventHandler(Windows.Microsoft_Defender_Antivirus.Handler.Scan_Job_Scheduler_Tick);
                    microsoft_defender_antivirus_scan_job_time_scheduler_timer.Enabled = true;
                    microsoft_defender_antivirus_scan_job_time_scheduler_timer_running = true;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Service.Module_Handler", "Start microsoft_defender_antivirus_events_timer", ex.ToString());
            }

            // IF THE DEFENDER IS SCANNING, THIS WILL DELAY THE TIMER EXECUTION BELOW

            Logging.Debug("Service.Module_Handler", "Stop", "Module_Handler");
        }
    }
}
