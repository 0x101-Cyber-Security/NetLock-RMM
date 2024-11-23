using MySqlConnector;
using System.Data.Common;
using System.Text.Json;
using static NetLock_RMM_Server.Agent.Windows.Device_Handler;

namespace NetLock_RMM_Server.Agent.Windows
{
    public class Event_Handler
    {
        public class Device_Identity_Entity
        {
            public string? agent_version { get; set; }
            public string? device_name { get; set; }
            public string? location_guid { get; set; }
            public string? tenant_guid { get; set; }
            public string? access_key { get; set; }
            public string? hwid { get; set; }
            public string? ip_address_internal { get; set; }
            public string? operating_system { get; set; }
            public string? domain { get; set; }
            public string? antivirus_solution { get; set; }
            public string? architecture { get; set; }
            public string? last_boot { get; set; }
            public string? timezone { get; set; }
            public string? cpu { get; set; }
            public string? cpu_usage { get; set; }
            public string? mainboard { get; set; }
            public string? gpu { get; set; }
            public string? ram { get; set; }
            public string? ram_usage { get; set; }
            public string? tpm { get; set; }
            //public string? environment_variables { get; set; }
        }

        public class Event_Entity
        {
            public string? severity { get; set; }
            public string? reported_by { get; set; }
            public string? _event { get; set; }
            public string? description { get; set; }
            public string? notification_json { get; set; }
            public string? type { get; set; }
            public string? language { get; set; }
        }

        public class Root_Entity
        {
            public Device_Identity_Entity? device_identity { get; set; }
            public Event_Entity? _event { get; set; }
        }

        public static async Task<string> Consume(string json)
        {
            Logging.Handler.Debug("Agent.Windows.Event_Handler.Consume", "json", json);

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                //Extract JSON
                Root_Entity rootData = JsonSerializer.Deserialize<Root_Entity>(json);
                Device_Identity_Entity device_identity = rootData.device_identity;
                Event_Entity _event = rootData._event;

                //Event data
                Logging.Handler.Debug("Agent.Windows.Event_Handler.Consume", "severity", _event.severity);
                Logging.Handler.Debug("Agent.Windows.Event_Handler.Consume", "reported_by", _event.reported_by);
                Logging.Handler.Debug("Agent.Windows.Event_Handler.Consume", "_event", _event._event);
                Logging.Handler.Debug("Agent.Windows.Event_Handler.Consume", "description", _event.description);
                Logging.Handler.Debug("Agent.Windows.Event_Handler.Consume", "notification_json", _event.notification_json);
                Logging.Handler.Debug("Agent.Windows.Event_Handler.Consume", "type", _event.type);
                Logging.Handler.Debug("Agent.Windows.Event_Handler.Consume", "language", _event.language);

                // Get tenant_id & location_id
                (int tenant_id, int location_id) = await Helper.Get_Tenant_Location_Id(device_identity.tenant_guid, device_identity.location_guid);

                // Get device_id
                int device_id = await Helper.Get_Device_Id(device_identity.device_name, tenant_id, location_id);

                // Get tenant_name & location_name
                (string tenant_name, string location_name) = await Helper.Get_Tenant_Location_Name(tenant_id, location_id);

                //Insert into database
                string execute_query = "INSERT INTO `events` ( `device_id`, `tenant_name_snapshot`, `location_name_snapshot`, `device_name`, `date`, `severity`, `reported_by`, `_event`, `description`, `notification_json`, `type`, `language`) VALUES (@device_id, @tenant_name, @location_name, @device_name, @date, @severity, @reported_by, @event, @description, @notification_json, @type, @language)";

                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(execute_query, conn);

                cmd.Parameters.AddWithValue("@device_id", device_id);
                cmd.Parameters.AddWithValue("@tenant_name", tenant_name);
                cmd.Parameters.AddWithValue("@location_name", location_name);
                cmd.Parameters.AddWithValue("@device_name", device_identity.device_name);
                cmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@severity", _event.severity);
                cmd.Parameters.AddWithValue("@reported_by", _event.reported_by);
                cmd.Parameters.AddWithValue("@event", _event._event);
                cmd.Parameters.AddWithValue("@description", _event.description);
                cmd.Parameters.AddWithValue("@notification_json", _event.notification_json);
                cmd.Parameters.AddWithValue("@type", _event.type);
                cmd.Parameters.AddWithValue("@language", _event.language);

                cmd.ExecuteNonQuery();

                await conn.CloseAsync();

                return "success";
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Agent.Windows.Event_Handler.Consume", "General error", ex.ToString());
                return "invalid";
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
