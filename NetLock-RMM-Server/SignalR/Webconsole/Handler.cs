using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;

namespace NetLock_RMM_Server.SignalR.Webconsole
{
    public class Handler
    {
        public class Remote_Identity
        {
            public string? api_key { get; set; }
            public string? command { get; set; }
        }

        public class Root_Entity
        {
            public Remote_Identity? remote_identity { get; set; }
        }

        /*public static async Task<bool> Verify_Api_Key(string json)
        {
            try
            {
                // Extract JSON
                Logging.Handler.Debug("SignalR.Webconsole.Verify_Api_Key", "json", json);
                Root_Entity rootData = JsonSerializer.Deserialize<Root_Entity>(json);
                Remote_Identity remote_identity = rootData.remote_identity;

                // Read the appsettings.json file
                string appsettings_json = File.ReadAllText(Environment.CurrentDirectory + @"\appsettings.json");
                Logging.Handler.Debug("SignalR.Webconsole.Verify_Api_Key", "appsettings_json", appsettings_json);

                string webconsole_api_key = string.Empty;

                // Deserialisierung des gesamten JSON-Strings
                using (JsonDocument document = JsonDocument.Parse(appsettings_json))
                {
                    JsonElement root = document.RootElement;

                    // Zugriff auf das "Version_Information"-Objekt
                    JsonElement Api_KeyElement = root.GetProperty("Api_Key");

                    // Zugriff auf das "Windows_Agent"-Attribut innerhalb von "Version_Information"
                    JsonElement webconsoleElement = Api_KeyElement.GetProperty("webconsole");

                    // Konvertierung des Werts von "Windows_Agent" in einen String
                    webconsole_api_key = webconsoleElement.GetString();
                }

                Logging.Handler.Debug("SignalR.Webconsole.Verify_Api_Key", "webconsole_api_key", webconsole_api_key);

                if (remote_identity.api_key == "1234567890")
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR.Webconsole.Verify_Api_Key", "general_error", ex.ToString());
                return false;
            }
        }*/

        public static async Task<string> Get_Command(string json)
        {
            try
            {
                // Extract JSON
                Logging.Handler.Debug("SignalR.Webconsole.Verify_Api_Key", "json", json);
                Root_Entity rootData = JsonSerializer.Deserialize<Root_Entity>(json);
                Remote_Identity remote_identity = rootData.remote_identity;

                // Return command
                string command = remote_identity.command;
                Logging.Handler.Debug("SignalR.Webconsole.Get_Command", "command", command);

                return command;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("SignalR.Webconsole.Get_Command", "general_error", ex.ToString());
                return String.Empty;
            }
        }
    }
}
