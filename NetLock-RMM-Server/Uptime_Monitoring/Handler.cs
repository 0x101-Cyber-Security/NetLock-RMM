using MySqlConnector;
using NetLock_RMM_Server.Background_Services;
using NetLock_RMM_Server.SignalR;
using System.Data.Common;
using System.Text.Json;

namespace NetLock_RMM_Server.Uptime_Monitoring
{
    public class Handler
    {
        public static async Task Do(string identityJson, bool connected)
        {
            // Check if server uptime is LESS than 30 minutes
            if (Configuration.Server.serverStartTime > DateTime.Now.AddMinutes(-30))
            {
                Logging.Handler.Debug("Uptime_Monitoring.Handler.Do", "Server uptime is less than 30 minutes, skipping event creation.", "");
                return;
            }

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            string notification_json = @"{""mail"":true,""microsoft_teams"":true,""telegram"":true,""ntfy_sh"":true}";

            try
            {
                // Extract device_name, location_name & tenant_name from JSON
                Logging.Handler.Debug("Uptime_Monitoring.Handler.Do", "identityJson", identityJson);

                string device_name = string.Empty;
                string location_guid = string.Empty;
                string tenant_guid = string.Empty;

                // Deserialize the JSON string into a dynamic object
                using (JsonDocument document = JsonDocument.Parse(identityJson))
                {
                    JsonElement root = document.RootElement;

                    // Check whether "device_identity" exists
                    if (root.TryGetProperty("device_identity", out JsonElement deviceIdentity))
                    {
                        // Jetzt innerhalb von "device_identity" navigieren und auslesen
                        device_name = deviceIdentity.GetProperty("device_name").GetString();
                        location_guid = deviceIdentity.GetProperty("location_guid").GetString();
                        tenant_guid = deviceIdentity.GetProperty("tenant_guid").GetString();
                    }
                    else
                        Logging.Handler.Error("Uptime_Monitoring.Handler.Do", "device_identity not found in JSON", identityJson);
                }

                // Log the extracted values
                Logging.Handler.Debug("Uptime_Monitoring.Handler.Do", "device_name", device_name);
                Logging.Handler.Debug("Uptime_Monitoring.Handler.Do", "location_guid", location_guid);
                Logging.Handler.Debug("Uptime_Monitoring.Handler.Do", "tenant_guid", tenant_guid);

                // Get tenant_id & location_id
                (int tenant_id, int location_id) = await Agent.Windows.Helper.Get_Tenant_Location_Id(tenant_guid, location_guid);

                // Get device_id
                int device_id = await Agent.Windows.Helper.Get_Device_Id(device_name, tenant_id, location_id);

                // Check if uptime_monitoring_enabled is enabled
                bool uptime_monitoring_enabled = await Agent.Windows.Helper.Get_Uptime_Monitoring_Status(device_id);

                if (!uptime_monitoring_enabled)
                {
                    Logging.Handler.Debug("Uptime_Monitoring.Handler.Do", "uptime_monitoring_enabled", "false");
                    return;
                }

                // Check if device is in update pending state
                int updatePending = Convert.ToInt32(await MySQL.Handler.Quick_Reader($"SELECT COUNT(*) AS count FROM devices WHERE id = {device_id} AND update_pending = 1;", "count"));

                if (updatePending == 1)
                {
                    Logging.Handler.Debug("Uptime_Monitoring.Handler.Do", "Device is in update pending state, skipping event creation.", "");
                    return;
                }

                // Get tenant_name & location_name
                (string tenant_name, string location_name) = await Agent.Windows.Helper.Get_Tenant_Location_Name(tenant_id, location_id);

                string _event = connected ? "Device connected." : "Device disconnected.";
                string description = connected ? "Connection to NetLock RMM server restored." : "The device lost connection to the NetLock RMM server.";

                string severity = "3";

                if (connected)
                    severity = "0";
                else
                    severity = "3";

                // Insert event into the database
                string execute_query = "INSERT INTO `events` ( `device_id`, `tenant_name_snapshot`, `location_name_snapshot`, `device_name`, `date`, `severity`, `reported_by`, `_event`, `description`, `notification_json`, `type`, `language`) VALUES (@device_id, @tenant_name, @location_name, @device_name, @date, @severity, @reported_by, @event, @description, @notification_json, @type, @language)";

                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(execute_query, conn);

                cmd.Parameters.AddWithValue("@device_id", device_id);
                cmd.Parameters.AddWithValue("@tenant_name", tenant_name);
                cmd.Parameters.AddWithValue("@location_name", location_name);
                cmd.Parameters.AddWithValue("@device_name", device_name);
                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@severity", severity);
                cmd.Parameters.AddWithValue("@reported_by", "Uptime monitoring");
                cmd.Parameters.AddWithValue("@event", _event);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@notification_json", notification_json);
                cmd.Parameters.AddWithValue("@type", "4"); // Uptime monitoring
                cmd.Parameters.AddWithValue("@language", "0");

                cmd.ExecuteNonQuery();

                await conn.CloseAsync();
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Uptime_Monitoring.Handler.Do", "General error", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
