using System.Text.Json;
using System;
using System.IO;
using System.Linq;
using NetLock_RMM_Agent_Remote;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Global.Helper
{
    public class Logging
    {
        public class Log_Data
        {
            public string type { get; set; } = string.Empty;
            public string date { get; set; } = string.Empty;
            public string reported_by { get; set; } = string.Empty;
            public string _event { get; set; } = string.Empty;
            public string content { get; set; } = string.Empty;
        }

        // Asynchronous logging queue
        private static readonly Channel<LogEntry> _logChannel = Channel.CreateUnbounded<LogEntry>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        private static readonly Task _logTask;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private class LogEntry
        {
            public string Type { get; set; }
            public string ReportedBy { get; set; }
            public string Event { get; set; }
            public Func<string> ContentProvider { get; set; }
            public string FileName { get; set; }
        }

        static Logging()
        {
            // Start background log writer
            _logTask = Task.Run(ProcessLogQueue);
        }

        public static bool Check_Debug_Mode()
        {
            try
            {
                if (File.Exists(Application_Paths.program_data_debug_txt))
                    return true;
                else
                    return false;
            }
            catch
            {
                return true;
            }
        }

        private static void Check_Dir()
        {
            if (!Directory.Exists(Application_Paths.program_data_logs))
                Directory.CreateDirectory(Application_Paths.program_data_logs);
        }

        private static async Task ProcessLogQueue()
        {
            var reader = _logChannel.Reader;
            
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var entry))
                {
                    try
                    {
                        Check_Dir();

                        var log_data = new Log_Data
                        {
                            type = entry.Type,
                            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), // Faster format
                            reported_by = entry.ReportedBy,
                            _event = entry.Event,
                            content = entry.ContentProvider() // Lazy evaluation
                        };

                        string log_json = JsonSerializer.Serialize(log_data, _jsonOptions);
                        string filePath = Path.Combine(Application_Paths.program_data_logs, entry.FileName);

                        await File.AppendAllTextAsync(filePath, log_json + Environment.NewLine);
                    }
                    catch
                    {
                        // Silent fail - logging errors should not crash the app
                    }
                }
            }
        }

        // Optimized Debug with Lazy Evaluation
        public static void Debug(string reported_by, string _event, string content)
        {
            if (!Configuration.Agent.debug_mode)
                return;

            DebugLazy(reported_by, _event, () => content);
        }

        // New: Lazy evaluation version
        public static void DebugLazy(string reported_by, string _event, Func<string> contentProvider)
        {
            if (!Configuration.Agent.debug_mode)
                return;

            _logChannel.Writer.TryWrite(new LogEntry
            {
                Type = "Debug",
                ReportedBy = reported_by,
                Event = _event,
                ContentProvider = contentProvider,
                FileName = "Debug.txt"
            });
        }

        public static void Error(string reported_by, string _event, string content)
        {
            if (!Configuration.Agent.debug_mode)
                return;

            ErrorLazy(reported_by, _event, () => content);
        }

        // New: Lazy evaluation version
        public static void ErrorLazy(string reported_by, string _event, Func<string> contentProvider)
        {
            if (!Configuration.Agent.debug_mode)
                return;

            _logChannel.Writer.TryWrite(new LogEntry
            {
                Type = "Error",
                ReportedBy = reported_by,
                Event = _event,
                ContentProvider = contentProvider,
                FileName = "Error.txt"
            });
        }

        public static void Microsoft_Defender_Firewall(string reported_by, string _event, string content)
        {
            if (!Configuration.Agent.debug_mode)
                return;

            _logChannel.Writer.TryWrite(new LogEntry
            {
                Type = "Microsoft_Defender_Firewall",
                ReportedBy = reported_by,
                Event = _event,
                ContentProvider = () => content,
                FileName = "Microsoft_Defender_Firewall.txt"
            });
        }

        public static void Device_Information(string reported_by, string _event, string content)
        {
            if (!Configuration.Agent.debug_mode)
                return;

            _logChannel.Writer.TryWrite(new LogEntry
            {
                Type = "Client_Information",
                ReportedBy = reported_by,
                Event = _event,
                ContentProvider = () => content,
                FileName = "Device_Information.txt"
            });
        }

        public static void Remote_Control(string reported_by, string _event, string content)
        {
            if (!Configuration.Agent.debug_mode)
                return;

            _logChannel.Writer.TryWrite(new LogEntry
            {
                Type = "Remote_Control",
                ReportedBy = reported_by,
                Event = _event,
                ContentProvider = () => content,
                FileName = "Remote_Control.txt"
            });
        }

        public static void PowerShell(string reported_by, string _event, string content)
        {
            if (!Configuration.Agent.debug_mode)
                return;

            _logChannel.Writer.TryWrite(new LogEntry
            {
                Type = "PowerShell",
                ReportedBy = reported_by,
                Event = _event,
                ContentProvider = () => content,
                FileName = "PowerShell.txt"
            });
        }

        public static void Registry(string reported_by, string _event, string content)
        {
            if (!Configuration.Agent.debug_mode)
                return;

            _logChannel.Writer.TryWrite(new LogEntry
            {
                Type = "Registry",
                ReportedBy = reported_by,
                Event = _event,
                ContentProvider = () => content,
                FileName = "Registry.txt"
            });
        }
    }
}
