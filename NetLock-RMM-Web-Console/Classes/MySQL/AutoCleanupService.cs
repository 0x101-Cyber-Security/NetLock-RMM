using Base64;
using BlazorMonaco.Editor;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;


namespace NetLock_RMM_Web_Console.Classes.MySQL
{
    public class AutoCleanupService : BackgroundService
    {
        private readonly TimeSpan _interval = TimeSpan.FromHours(1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Clean();
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException ex)
                {
                    Logging.Handler.Debug("Server_Information_Service.ExecuteAsync", "Task canceled: ", ex.ToString());
                    // Task was terminated, terminate cleanly
                    break;
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("ServerInformationService", "ExecuteAsync", ex.ToString());
                }
            }
        }

        private async Task Clean()
        {
            try
            {
                Logging.Handler.Debug("Auto_Cleanup_Service.Clean", "Task started at:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                Console.WriteLine("Auto_Cleanup_Service.Clean -> Task started at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                // Automatic cleanup
                bool cleanup_applications_drivers_history_enabled = false;
                int cleanup_applications_drivers_history_days = 0;

                bool cleanup_applications_installed_history_enabled = false;
                int cleanup_applications_installed_history_days = 0;

                bool cleanup_applications_logon_history_enabled = false;
                int cleanup_applications_logon_history_days = 0;

                bool cleanup_applications_scheduled_tasks_history_enabled = false;
                int cleanup_applications_scheduled_tasks_history_days = 0;

                bool cleanup_applications_services_history_enabled = false;
                int cleanup_applications_services_history_days = 0;

                bool cleanup_device_information_antivirus_products_history_enabled = false;
                int cleanup_device_information_antivirus_products_history_days = 0;

                bool cleanup_device_information_cpu_history_enabled = false;
                int cleanup_device_information_cpu_history_days = 0;

                bool cleanup_device_information_cronjobs_history_enabled = false;
                int cleanup_device_information_cronjobs_history_days = 0;

                bool cleanup_device_information_disks_history_enabled = false;
                int cleanup_device_information_disks_history_days = 0;

                bool cleanup_device_information_general_history_enabled = false;
                int cleanup_device_information_general_history_days = 0;

                bool cleanup_device_information_history_enabled = false;
                int cleanup_device_information_history_days = 0;

                bool cleanup_device_information_network_adapters_history_enabled = false;
                int cleanup_device_information_network_adapters_history_days = 0;

                bool cleanup_device_information_ram_history_enabled = false;
                int cleanup_device_information_ram_history_days = 0;

                bool cleanup_device_information_task_manager_history_enabled = false;
                int cleanup_device_information_task_manager_history_days = 0;

                bool cleanup_events_history_enabled = false;
                int cleanup_events_history_days = 0;

                // Get configuration settings
                MySqlConnection conn = new MySqlConnection(NetLock_RMM_Web_Console.Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM settings;";

                    MySqlCommand cmd = new MySqlCommand(query, conn);

                    Logging.Handler.Debug("Auto_Cleanup_Service.Clean", "MySQL_Prepared_Query", query);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                cleanup_applications_drivers_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_applications_drivers_history_enabled"));
                                cleanup_applications_drivers_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_applications_drivers_history_days"));

                                cleanup_applications_installed_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_applications_installed_history_enabled"));
                                cleanup_applications_installed_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_applications_installed_history_days"));

                                cleanup_applications_logon_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_applications_logon_history_enabled"));
                                cleanup_applications_logon_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_applications_logon_history_days"));

                                cleanup_applications_scheduled_tasks_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_applications_scheduled_tasks_history_enabled"));
                                cleanup_applications_scheduled_tasks_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_applications_scheduled_tasks_history_days"));

                                cleanup_applications_services_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_applications_services_history_enabled"));
                                cleanup_applications_services_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_applications_services_history_days"));

                                cleanup_device_information_antivirus_products_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_device_information_antivirus_products_history_enabled"));
                                cleanup_device_information_antivirus_products_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_device_information_antivirus_products_history_days"));

                                cleanup_device_information_cpu_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_device_information_cpu_history_enabled"));
                                cleanup_device_information_cpu_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_device_information_cpu_history_days"));

                                cleanup_device_information_cronjobs_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_device_information_cronjobs_history_enabled"));
                                cleanup_device_information_cronjobs_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_device_information_cronjobs_history_days"));

                                cleanup_device_information_disks_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_device_information_disks_history_enabled"));
                                cleanup_device_information_disks_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_device_information_disks_history_days"));

                                cleanup_device_information_general_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_device_information_general_history_enabled"));
                                cleanup_device_information_general_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_device_information_general_history_days"));

                                cleanup_device_information_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_device_information_history_enabled"));
                                cleanup_device_information_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_device_information_history_days"));

                                cleanup_device_information_network_adapters_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_device_information_network_adapters_history_enabled"));
                                cleanup_device_information_network_adapters_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_device_information_network_adapters_history_days"));

                                cleanup_device_information_ram_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_device_information_ram_history_enabled"));
                                cleanup_device_information_ram_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_device_information_ram_history_days"));

                                cleanup_device_information_task_manager_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_device_information_task_manager_history_enabled"));
                                cleanup_device_information_task_manager_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_device_information_task_manager_history_days"));

                                cleanup_events_history_enabled = reader.GetBoolean(reader.GetOrdinal("cleanup_events_history_enabled"));
                                cleanup_events_history_days = reader.GetInt32(reader.GetOrdinal("cleanup_events_history_days"));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Auto_Cleanup_Service.Clean -> Get_Cleanup_Settings", "MySQL_Query", ex.ToString());
                }
                finally
                {
                    await conn.CloseAsync();
                }

                // Cleanup logic
                if (cleanup_applications_drivers_history_enabled)
                {
                    // Berechne das Datum, das der Anzahl der Tage entspricht
                    string query = $"DELETE FROM applications_drivers_history WHERE date < NOW() - INTERVAL {cleanup_applications_drivers_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_applications_installed_history_enabled)
                {
                    string query = $"DELETE FROM applications_installed_history WHERE date < NOW() - INTERVAL {cleanup_applications_installed_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_applications_logon_history_enabled)
                {
                    string query = $"DELETE FROM applications_logon_history WHERE date < NOW() - INTERVAL {cleanup_applications_logon_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_applications_scheduled_tasks_history_enabled)
                {
                    string query = $"DELETE FROM applications_scheduled_tasks_history WHERE date < NOW() - INTERVAL {cleanup_applications_scheduled_tasks_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_applications_services_history_enabled)
                {
                    string query = $"DELETE FROM applications_services_history WHERE date < NOW() - INTERVAL {cleanup_applications_services_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_device_information_antivirus_products_history_enabled)
                {
                    string query = $"DELETE FROM device_information_antivirus_products_history WHERE date < NOW() - INTERVAL {cleanup_device_information_antivirus_products_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_device_information_cpu_history_enabled)
                {
                    string query = $"DELETE FROM device_information_cpu_history WHERE date < NOW() - INTERVAL {cleanup_device_information_cpu_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_device_information_cronjobs_history_enabled)
                {
                    string query = $"DELETE FROM device_information_cronjobs_history WHERE date < NOW() - INTERVAL {cleanup_device_information_cronjobs_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_device_information_disks_history_enabled)
                {
                    string query = $"DELETE FROM device_information_disks_history WHERE date < NOW() - INTERVAL {cleanup_device_information_disks_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_device_information_general_history_enabled)
                {
                    string query = $"DELETE FROM device_information_general_history WHERE date < NOW() - INTERVAL {cleanup_device_information_general_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_device_information_history_enabled)
                {
                    string query = $"DELETE FROM device_information_history WHERE date < NOW() - INTERVAL {cleanup_device_information_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_device_information_network_adapters_history_enabled)
                {
                    string query = $"DELETE FROM device_information_network_adapters_history WHERE date < NOW() - INTERVAL {cleanup_device_information_network_adapters_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_device_information_ram_history_enabled)
                {
                    string query = $"DELETE FROM device_information_ram_history WHERE date < NOW() - INTERVAL {cleanup_device_information_ram_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_device_information_task_manager_history_enabled)
                {
                    string query = $"DELETE FROM device_information_task_manager_history WHERE date < NOW() - INTERVAL {cleanup_device_information_task_manager_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                if (cleanup_events_history_enabled)
                {
                    string query = $"DELETE FROM events WHERE date < NOW() - INTERVAL {cleanup_events_history_days} DAY;";
                    await NetLock_RMM_Web_Console.Classes.MySQL.Handler.Execute_Command(query);
                }

                Logging.Handler.Debug("Auto_Cleanup_Service.Clean", "Task finished at:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                Console.WriteLine("Auto_Cleanup_Service.Clean -> Task finished at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Auto_Cleanup_Service.Clean", "General error", ex.ToString());
            }
        }
    }
}

   
