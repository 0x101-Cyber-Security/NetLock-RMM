using MySqlConnector;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace NetLock_RMM_Server.Logging
{
    public class Handler
    {
        private static readonly object lockObj = new object();

        public static void Debug(string module, string method, string message, [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                // Don't log if logging is disabled
                if (!Configuration.Server.loggingEnabled)
                    return;

                // Don't log if the message is null or empty
                if (String.IsNullOrEmpty(message))
                    return;

                string text = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] [debug][{module}][{method}][L:{lineNumber}] {message}";

                // Lock to prevent multiple threads from writing to the file at the same time
                lock (lockObj)
                {
                    // Create the directory if it doesn't exist
                    if (!Directory.Exists(Application_Paths.logs_dir))
                        Directory.CreateDirectory(Application_Paths.logs_dir);

                    // Write to the debug log file
                    using (var writer = File.AppendText(Application_Paths.log_debug_path))
                    {
                        writer.WriteLine(text);
                    }

                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(text);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to debug log: " + ex.Message);
            }
        }

        public static void Info(string module, string method, string message, [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                // Don't log if logging is disabled
                if (!Configuration.Server.loggingEnabled)
                    return;

                // Don't log if the message is null or empty
                if (String.IsNullOrEmpty(message))
                    return;

                string text = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] [info][{module}][{method}][L:{lineNumber}] {message}";

                // Lock to prevent multiple threads from writing to the file at the same time
                lock (lockObj)
                {
                    // Create the directory if it doesn't exist
                    if (!Directory.Exists(Application_Paths.logs_dir))
                        Directory.CreateDirectory(Application_Paths.logs_dir);

                    // Write to the info log file
                    using (var writer = File.AppendText(Application_Paths.log_info_path))
                    {
                        writer.WriteLine(text);
                    }

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(text);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to info log: " + ex.Message);
            }
        }

        public static void Warning(string module, string method, string message, [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                // Don't log if logging is disabled
                if (!Configuration.Server.loggingEnabled)
                    return;

                // Don't log if the message is null or empty
                if (String.IsNullOrEmpty(message))
                    return;

                string text = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] [warning][{module}][{method}][L:{lineNumber}] {message}";

                // Lock to prevent multiple threads from writing to the file at the same time
                lock (lockObj)
                {
                    // Create the directory if it doesn't exist
                    if (!Directory.Exists(Application_Paths.logs_dir))
                        Directory.CreateDirectory(Application_Paths.logs_dir);

                    // Write to the warning log file
                    using (var writer = File.AppendText(Application_Paths.log_warning_path))
                    {
                        writer.WriteLine(text);
                    }

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(text);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to warning log: " + ex.Message);
            }
        }

        public static void Error(string module, string method, string message, [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                // Don't log if logging is disabled
                if (!Configuration.Server.loggingEnabled)
                    return;

                // Don't log if the message is null or empty
                if (String.IsNullOrEmpty(message))
                    return;

                string text = $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] [error][{module}][{method}][L:{lineNumber}] {message}";

                // Lock to prevent multiple threads from writing to the file at the same time
                lock (lockObj)
                {
                    // Create the directory if it doesn't exist
                    if (!Directory.Exists(Application_Paths.logs_dir))
                        Directory.CreateDirectory(Application_Paths.logs_dir);

                    // Write to the error log file
                    using (var writer = File.AppendText(Application_Paths.log_error_path))
                    {
                        writer.WriteLine(text);
                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(text);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to error log: " + ex.Message);
            }
        }

        public static void Database(string module, string method, string message)
        {
            try
            {
                // Don't log if logging is disabled
                if (!Configuration.Server.loggingEnabled)
                    return;

                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    conn.Open();

                    string execute_query = "INSERT INTO `logs` (`module`, `method`, `message`, `date`) VALUES (@module, @method, @message, @date);";

                    MySqlCommand cmd = new MySqlCommand(execute_query, conn);
                    cmd.Parameters.AddWithValue("@module", module);
                    cmd.Parameters.AddWithValue("@method", method);
                    cmd.Parameters.AddWithValue("@message", message);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error writing to database log: " + ex.Message);
                }
                finally
                {
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to database log: " + ex.Message);
            }
        }

        // Neue Methode für die optimierte Protokollierung
        public static bool IsDebugVerboseEnabled()
        {
            try 
            {
                // Diese Methode kann erweitert werden, um die Protokollierungsebene 
                // aus der Konfiguration zu lesen oder Umgebungsvariablen zu prüfen
                
                // Aktuell gibt es einen konfigurierbaren Verbosity-Level in appsettings.json:
                var verbosityLevel = GetVerbosityLevel();
                
                // Ein Wert von 2 oder höher bedeutet, dass detaillierte Debug-Ausgaben aktiviert sind
                return Configuration.Server.loggingEnabled && verbosityLevel >= 2;
            }
            catch
            {
                // Im Fehlerfall gehen wir davon aus, dass detaillierte Logs nicht gewünscht sind
                return false;
            }
        }
        
        private static int GetVerbosityLevel()
        {
            try
            {
                var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();
                
                return config.GetValue<int>("Logging:Custom:VerbosityLevel", 1);
            }
            catch
            {
                return 1; // Standard-Verbosity bei Fehlern
            }
        }
    }
}