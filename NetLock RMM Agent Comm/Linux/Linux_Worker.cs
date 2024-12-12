using Global.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NetLock_RMM_Agent_Comm
{
    public class Linux_Worker : BackgroundService
    {
        private readonly ILogger<Linux_Worker> _logger;
        private IHostApplicationLifetime _lifetime;

        public Linux_Worker(ILogger<Linux_Worker> logger, IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _lifetime = lifetime;
        }

        // Timers
        public static System.Timers.Timer start_timer;
        public static System.Timers.Timer sync_timer;

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
        {
            bool first_run = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                if (first_run)
                {
                    _logger.LogInformation("Linux Worker running at: {time}", DateTimeOffset.Now);

                    Logging.Debug("Linux.ExecuteAsync", "Linux Worker running at: " + DateTimeOffset.Now, "");

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

                    first_run = false;
                }
            }
            await Task.Delay(5000, stoppingToken);
        });

        private async void Initialize_Timer_Tick(object sender, ElapsedEventArgs e)
        {
            Logging.Debug("Linux_Worker.Initialize", "Initialize", "Start");

            await Initialize(false);

            Logging.Debug("Linux_Worker.Initialize", "Initialize", "Stop");
        }

        private async Task Initialize(bool forced)
        {
            Logging.Debug("Linux_Worker.Initialize", "Initialize", "Start");

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
                Logging.Debug("Service.Pre_Boot", "", "Authorized.");

                // Load rulesets

                // Will be used later for features to startup as early as possible
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

            // init modules here

            Logging.Debug("Service.Module_Handler", "Stop", "Module_Handler");
        }
    }
}
