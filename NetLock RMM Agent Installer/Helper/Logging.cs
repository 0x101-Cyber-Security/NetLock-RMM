using System.Text.Json;
using System;
using System.IO;
using System.Linq;
using NetLock_RMM_Agent_Installer;

namespace Logging
{
    public class Handler
    {
        public class Log_Data
        {
            public string type { get; set; } = string.Empty;
            public string date { get; set; } = string.Empty;
            public string reported_by { get; set; } = string.Empty;
            public string _event { get; set; } = string.Empty;
            public string content { get; set; } = string.Empty;
        }

        private static void Check_Dir()
        {
            if (!Directory.Exists(Application_Paths.c_temp_logs_dir))
                Directory.CreateDirectory(Application_Paths.c_temp_logs_dir);
        }

        public static void Debug(string reported_by, string _event, string content)
        {
            try
            {
                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Debug";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by= reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);

                File.AppendAllText(Application_Paths.c_temp_logs_dir + @"\Debug.txt", log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Error(string reported_by, string _event, string content)
        {
            try
            {
                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Error";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                string log_json = JsonSerializer.Serialize(json_object);

                File.AppendAllText(Application_Paths.c_temp_logs_dir + @"\Error.txt", log_json + Environment.NewLine);
            }
            catch
            { }
        }
    }
}
