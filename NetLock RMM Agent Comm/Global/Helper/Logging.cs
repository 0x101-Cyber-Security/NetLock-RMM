using System.Text.Json;
using System;
using System.IO;
using System.Linq;
using NetLock_RMM_Agent_Comm;

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
            if (Directory.Exists(Application_Paths.program_data_logs) == false)
                Directory.CreateDirectory(Application_Paths.program_data_logs);
        }

        public static void Debug(string reported_by, string _event, string content)
        {
            try
            {
                if (!Configuration.Agent.debug_mode)
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Debug";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);

                File.AppendAllText(Path.Combine(Application_Paths.program_data_logs, "Debug.txt"), log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Error(string reported_by, string _event, string content)
        {
            try
            {
                if (!Configuration.Agent.debug_mode)
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Error";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                string log_json = JsonSerializer.Serialize(json_object);

                File.AppendAllText(Path.Combine(Application_Paths.program_data_logs, "Error.txt"), log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Microsoft_Defender_Firewall(string reported_by, string _event, string content)
        {
            try
            {
                if (!Configuration.Agent.debug_mode)
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Microsoft_Defender_Firewall";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);

                File.AppendAllText(Path.Combine(Application_Paths.program_data_logs, "Microsoft_Defender_Firewall.txt"), log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Device_Information(string reported_by, string _event, string content)
        {
            try
            {
                if (!Configuration.Agent.debug_mode)
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Client_Information";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);

                File.AppendAllText(Path.Combine(Application_Paths.program_data_logs, "Device_Information.txt"), log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Microsoft_Defender_Antivirus(string reported_by, string _event, string content)
        {
            try
            {
                if (!Configuration.Agent.debug_mode)
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Microsoft_Defender_Antivirus";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);

                File.AppendAllText(Path.Combine(Application_Paths.program_data_logs, "Microsoft_Defender_Antivirus.txt"), log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void PowerShell(string reported_by, string _event, string content)
        {
            try
            {
                if (!Configuration.Agent.debug_mode)
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "PowerShell";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);

                File.AppendAllText(Path.Combine(Application_Paths.program_data_logs, "PowerShell.txt"), log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Registry(string reported_by, string _event, string content)
        {
            try
            {
                if (!Configuration.Agent.debug_mode)
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Registry";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);

                File.AppendAllText(Path.Combine(Application_Paths.program_data_logs, "Registry.txt"), log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Jobs(string reported_by, string _event, string content)
        {
            try
            {
                if (!Configuration.Agent.debug_mode)
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Jobs";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);

                File.AppendAllText(Path.Combine(Application_Paths.program_data_logs, "Jobs.txt"), log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Sensors(string reported_by, string _event, string content)
        {
            try
            {
                if (!Configuration.Agent.debug_mode)
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Sensors";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);

                File.AppendAllText(Path.Combine(Application_Paths.program_data_logs, "Sensors.txt"), log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Local_Server(string reported_by, string _event, string content)
        {
            try
            {
                if (!Configuration.Agent.debug_mode)
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Local_Server";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);

                File.AppendAllText(Path.Combine(Application_Paths.program_data_logs, "Local_Server.txt"), log_json + Environment.NewLine);
            }
            catch
            { }
        }



    }
}
