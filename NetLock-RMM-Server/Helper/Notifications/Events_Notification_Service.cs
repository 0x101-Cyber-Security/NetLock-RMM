using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Requests;

public class Events_Notification_Service : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEventsTask();
                await Task.Delay(_interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                Logging.Handler.Debug("Events_Notification_Service.ExecuteAsync", "Task canceled: ", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                // The task was completed cleanly
                break;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("EventsNotificationService", "ExecuteAsync", ex.ToString());
            }
        }
    }

    private async Task ProcessEventsTask()
    {
        string startedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Logging.Handler.Debug("Events_Notification_Service.ProcessEventsTask", "Periodic task executed at: ", startedTime);

        try
        {
            // Parallel processing of notifications
            /*var tasks = new List<Task>
            {
                NetLock_RMM_Server.Events.Sender.Smtp("mail_status", "mail_notifications"),
                NetLock_RMM_Server.Events.Sender.Smtp("ms_teams_status", "microsoft_teams_notifications"),
                NetLock_RMM_Server.Events.Sender.Smtp("telegram_status", "telegram_notifications"),
                NetLock_RMM_Server.Events.Sender.Smtp("ntfy_sh_status", "ntfy_sh_notifications")
            };

            await Task.WhenAll(tasks);*/

            await NetLock_RMM_Server.Events.Sender.Smtp("mail_status", "mail_notifications");
            await NetLock_RMM_Server.Events.Sender.Smtp("ms_teams_status", "microsoft_teams_notifications");
            await NetLock_RMM_Server.Events.Sender.Smtp("telegram_status", "telegram_notifications");
            await NetLock_RMM_Server.Events.Sender.Smtp("ntfy_sh_status", "ntfy_sh_notifications");

            string finishedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Logging.Handler.Debug("Events_Notification_Service.ProcessEventsTask", "Periodic task finished at: ", finishedTime);

            // Markiere alte Events als gelesen
            await NetLock_RMM_Server.Events.Sender.Mark_Old_Read(startedTime, finishedTime);
        }
        catch (Exception ex)
        {
            Logging.Handler.Error("EventsNotificationService", "ProcessEventsTask", ex.ToString());
        }
    }
}