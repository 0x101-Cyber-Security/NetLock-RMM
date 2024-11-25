using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            }await Task.Delay(1000, stoppingToken);
        }
    }
}
