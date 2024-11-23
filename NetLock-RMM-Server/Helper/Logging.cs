using NetLock_RMM_Server;
using System.Text.Json;

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

        public static bool Check_Debug_Mode()
        {
            try
            {
                if (File.Exists(Application_Paths.debug_txt_path))
                    return true;
                else
                    return false;
            }
            catch
            {
                return true;
            }
        }

        private static void chck_dir()
        {
            if (Directory.Exists(Application_Paths.logs_dir) == false)
                Directory.CreateDirectory(Application_Paths.logs_dir);
        }

        public static string Generate_ID(int length)
        {
            string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static void Debug(string reported_by, string _event, string content)
        {
            try
            {
                if (!Check_Debug_Mode())
                    return;

                chck_dir();

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

                File.AppendAllText(Application_Paths.logs_dir + @"\debug.txt", log_json + Environment.NewLine);
            }
            catch
            { }
        }

        public static void Error(string reported_by, string _event, string content)
        {
            try
            {
                if (!Check_Debug_Mode())
                    return;

                chck_dir();

                Log_Data json_object = new Log_Data();
                json_object.type = "Error";
                json_object.date = DateTime.Now.ToString();
                json_object.reported_by = reported_by;
                json_object._event = _event;
                json_object.content = content;

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                };

                string log_json = JsonSerializer.Serialize(json_object, options);
                File.AppendAllText(Application_Paths.logs_dir + @"\error.txt", log_json + Environment.NewLine);
            }
            catch
            { }
        }
    }
}
