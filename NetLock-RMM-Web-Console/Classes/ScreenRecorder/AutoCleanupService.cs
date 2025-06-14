using Base64;
using BlazorMonaco.Editor;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace NetLock_RMM_Web_Console.Classes.ScreenRecorder
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
                    Logging.Handler.Debug("Classes.ScreenRecorder.AutoCleanupService.ExecuteAsync", "Task canceled: ", ex.ToString());
                    // Task was terminated, terminate cleanly
                    break;
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Classes.ScreenRecorder.AutoCleanupService.ExecuteAsync", "General error", ex.ToString());
                }
            }
        }

        private async Task Clean()
        {
            try
            {
                Logging.Handler.Debug("Classes.ScreenRecorder.AutoCleanupService", "Task started at:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                //Console.WriteLine("Classes.ScreenRecorder.AutoCleanupService -> Task started at: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                bool isEnabled = (await MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "remote_screen_session_recording_auto_clean_enabled")) == "1";
                int daysToKeep = Convert.ToInt32(await MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "remote_screen_session_recording_forced_days"));

                Logging.Handler.Debug("Classes.ScreenRecorder.AutoCleanupService", "Auto cleanup enabled:", isEnabled.ToString());
                Logging.Handler.Debug("Classes.ScreenRecorder.AutoCleanupService", "Days to keep recordings:", daysToKeep.ToString());

                if (!isEnabled)
                {
                    Logging.Handler.Debug("Classes.ScreenRecorder.AutoCleanupService", "Auto cleanup is disabled, exiting.", "");
                    Console.WriteLine("Classes.ScreenRecorder.AutoCleanupService -> Auto cleanup is disabled, exiting.");
                    return;
                }

                // Automatic cleanup
                // Remove all files from Application_Paths.internal_recordings_dir older 
                // than the specified number of days

                string recordingsDir = Application_Paths.internal_recordings_dir;

                if (Directory.Exists(recordingsDir))
                {
                    var files = Directory.GetFiles(recordingsDir, "*.png");
                    DateTime thresholdDate = DateTime.Now.AddDays(-daysToKeep);
                    foreach (var file in files)
                    {
                        DateTime fileCreationTime = File.GetCreationTime(file);
                        if (fileCreationTime < thresholdDate)
                        {
                            File.Delete(file);
                            Logging.Handler.Debug("Classes.ScreenRecorder.AutoCleanupService", "Deleted file:", file);
                        }
                    }
                }
                else
                {
                    Logging.Handler.Error("Classes.ScreenRecorder.AutoCleanupService", "Recordings directory does not exist:", recordingsDir);
                }

                Logging.Handler.Debug("Classes.ScreenRecorder.AutoCleanupService", "Cleanup completed successfully.", "");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.ScreenRecorder.AutoCleanupService", "Error during cleanup", ex.ToString());
                Console.WriteLine("Classes.ScreenRecorder.AutoCleanupService -> Error during cleanup: " + ex.ToString());
            }
        }
    }
}
