using Microsoft.Extensions.Hosting;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

public class Server_Information_Service : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Update_Server_Information();
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

    private async Task Update_Server_Information()
    {
        try
        {
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
