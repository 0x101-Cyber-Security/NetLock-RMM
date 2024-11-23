using MySqlConnector;
using System.Text.Json;

namespace NetLock_RMM_Server.Agent.Windows
{
    public class Helper
    {
        // Get the tenant id & location id with tenant_guid & location_guid
        public static async Task<(int, int)> Get_Tenant_Location_Id(string tenant_guid, string location_guid)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                // Abfrage für tenant_id
                string tenantIdQuery = "SELECT id FROM tenants WHERE guid = @tenant_guid;";
                MySqlCommand tenantCmd = new MySqlCommand(tenantIdQuery, conn);
                tenantCmd.Parameters.AddWithValue("@tenant_guid", tenant_guid);

                int tenant_id = 0;
                using (MySqlDataReader tenantReader = await tenantCmd.ExecuteReaderAsync())
                {
                    if (tenantReader.HasRows && await tenantReader.ReadAsync())
                    {
                        tenant_id = tenantReader.GetInt32("id");
                    }
                }

                // Abfrage für location_id
                string locationIdQuery = "SELECT id FROM locations WHERE guid = @location_guid;";
                MySqlCommand locationCmd = new MySqlCommand(locationIdQuery, conn);
                locationCmd.Parameters.AddWithValue("@location_guid", location_guid);

                int location_id = 0;
                using (MySqlDataReader locationReader = await locationCmd.ExecuteReaderAsync())
                {
                    if (locationReader.HasRows && await locationReader.ReadAsync())
                    {
                        location_id = locationReader.GetInt32("id");
                    }
                }

                return (tenant_id, location_id);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("NetLock_RMM_Server.Modules.Helper.Get_Tenant_Location_Id", "General error", ex.ToString());
                return (0, 0);
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get tenant and location name with tenant id & location id
        public static async Task<(string, string)> Get_Tenant_Location_Name(int tenant_id, int location_id)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                // Abfrage für tenant_name
                string tenantNameQuery = "SELECT name FROM tenants WHERE id = @tenant_id;";
                MySqlCommand tenantCmd = new MySqlCommand(tenantNameQuery, conn);
                tenantCmd.Parameters.AddWithValue("@tenant_id", tenant_id);

                string tenant_name = String.Empty;
                using (MySqlDataReader tenantReader = await tenantCmd.ExecuteReaderAsync())
                {
                    if (tenantReader.HasRows && await tenantReader.ReadAsync())
                    {
                        tenant_name = tenantReader["name"].ToString();
                    }
                }

                // Abfrage für location_name
                string locationNameQuery = "SELECT name FROM locations WHERE id = @location_id;";
                MySqlCommand locationCmd = new MySqlCommand(locationNameQuery, conn);
                locationCmd.Parameters.AddWithValue("@location_id", location_id);

                string location_name = String.Empty;
                using (MySqlDataReader locationReader = await locationCmd.ExecuteReaderAsync())
                {
                    if (locationReader.HasRows && await locationReader.ReadAsync())
                    {
                        location_name = locationReader["name"].ToString();
                    }
                }

                return (tenant_name, location_name);
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("NetLock_RMM_Server.Modules.Helper.Get_Tenant_Location_Name", "General error", ex.ToString());
                return (String.Empty, String.Empty);
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get device id with device name, tenant id & location id
        public static async Task<int> Get_Device_Id(string device_name, int tenant_id, int location_id)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                // Abfrage für device_id
                string deviceIdQuery = "SELECT id FROM devices WHERE device_name = @device_name AND tenant_id = @tenant_id AND location_id = @location_id;";
                MySqlCommand deviceCmd = new MySqlCommand(deviceIdQuery, conn);
                deviceCmd.Parameters.AddWithValue("@device_name", device_name);
                deviceCmd.Parameters.AddWithValue("@tenant_id", tenant_id);
                deviceCmd.Parameters.AddWithValue("@location_id", location_id);

                int device_id = 0;
                using (MySqlDataReader deviceReader = await deviceCmd.ExecuteReaderAsync())
                {
                    if (deviceReader.HasRows && await deviceReader.ReadAsync())
                    {
                        device_id = deviceReader.GetInt32("id");
                    }
                }

                return device_id;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("NetLock_RMM_Server.Modules.Helper.Get_Device_Id", "General error", ex.ToString());
                return 0;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get role_remote from appsettings.json file
        public static async Task<bool> Get_Role_Status(string role)
        {
            try
            {
                Logging.Handler.Debug("NetLock_RMM_Server.Modules.Helper.Get_Role_Status", "role", role);
                
                string json = File.ReadAllText("appsettings.json");
                Logging.Handler.Debug("NetLock_RMM_Server.Modules.Helper.Get_Role_Status", "appsettings.json", json);

                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement kestrelElement = document.RootElement.GetProperty("Kestrel");
                    JsonElement rolesElement = kestrelElement.GetProperty("Roles");
                    bool role_status = rolesElement.GetProperty(role).GetBoolean();
                    Logging.Handler.Debug("NetLock_RMM_Server.Modules.Helper.Get_Role_Status", "role_status", role_status.ToString());
                    return role_status;
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("NetLock_RMM_Server.Modules.Helper.Get_Role_Status", "General error", ex.ToString());
                return false;
            }
        }

    }
}
