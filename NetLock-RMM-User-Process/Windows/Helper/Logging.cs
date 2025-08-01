﻿using System.Text.Json;
using System;
using System.IO;
using System.Linq;
using NetLock_RMM_User_Process;

namespace NetLock_RMM_User_Process.Windows.Helper
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

        private static bool Check_Debug_Mode()
        {
            try
            {
                if (File.Exists(Application_Paths.application_data_debug_txt))
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
            if (!Directory.Exists(Application_Paths.application_data_logs))
                Directory.CreateDirectory(Application_Paths.application_data_logs);
        }

        public static void Debug(string reported_by, string _event, string content)
        {
            try
            {
                if (Check_Debug_Mode())
                    return;

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

                File.AppendAllText(Application_Paths.application_data_logs + @"\Debug.txt", log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Error(string reported_by, string _event, string content)
        {
            try
            {
                if (Check_Debug_Mode())
                    return;

                Check_Dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Error";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                string log_json = JsonSerializer.Serialize(json_object);

                File.AppendAllText(Application_Paths.application_data_logs + @"\Error.txt", log_json + Environment.NewLine);
            }
            catch
            { }
        }
    }
}