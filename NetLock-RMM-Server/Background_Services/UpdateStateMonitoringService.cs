using MySqlConnector;
using System.Data.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NetLock_RMM_Server.Background_Services
{
    public class UpdateStateMonitoringService : BackgroundService
    {
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckPendingUpdates();
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

        private async Task CheckPendingUpdates()
        {
            try
            {
                // Check for each device if there are pending updates based on update_pending = 1 & update_started (DATETIME)
                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM devices WHERE update_pending = 1;";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    
                    Logging.Handler.Debug("Example", "MySQL_Prepared_Query", query);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                // compare update_started with datetimenow and if it is older than 15 minutes, set update_pending = 0
                                DateTime updateStarted = reader.GetDateTime(reader.GetOrdinal("update_started"));

                                if (DateTime.Now - updateStarted > TimeSpan.FromMinutes(15))
                                {
                                    string device_id = reader["id"].ToString();

                                    // Update the device to set update_pending = 0
                                    await MySQL.Handler.Execute_Command($"UPDATE devices SET update_pending = 0 WHERE id = {device_id};");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Example", "MySQL_Query", ex.ToString());
                }
                finally
                {
                    await conn.CloseAsync();
                }

                Logging.Handler.Debug("Server_Information_Service.UpdateServerInformation", "Server Information Update Task started at:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                await NetLock_RMM_Server.MySQL.Handler.Update_Server_Information();
                Logging.Handler.Debug("Server_Information_Service.UpdateServerInformation", "Server Information Update Task finished at:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("ServerInformationService", "UpdateServerInformation", ex.ToString());
            }
        }
    }
}