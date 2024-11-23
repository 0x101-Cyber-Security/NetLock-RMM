using MySqlConnector;
using System.Data.Common;
using System.Text.Json;

namespace NetLock_RMM_Web_Console.Classes.MySQL
{
    public class Tenants
    {
        public class Tenants_Activation_State
        {
            public string id { get; set; } = String.Empty;
            public string guid { get; set; } = String.Empty;
        }

        // Get tenants json from account table
        public static async Task Assign_Tenant_To_User(string username, string tenant_id, string tenant_guid)
        {
            string tenants_json = string.Empty;

            using (MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String))
            {
                try
                {
                    await conn.OpenAsync();

                    // Fetch tenants JSON for the user
                    string query = "SELECT tenants FROM accounts WHERE username = @username;";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);

                    Logging.Handler.Debug("Assign_Tenant_To_User", "MySQL_Prepared_Query", query);

                    tenants_json = (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Assign_Tenant_To_User", "MySQL_Query", ex.ToString());
                    return;  // Exit early in case of failure
                }
            }

            // Deserialize, modify tenants list, and serialize back
            var tenants = JsonSerializer.Deserialize<List<Tenants_Activation_State>>(tenants_json) ?? new();

            if (tenants.All(t => t.id != tenant_id || t.guid != tenant_guid))
            {
                tenants.Add(new Tenants_Activation_State { id = tenant_id, guid = tenant_guid });
            }

            string new_tenants_json = JsonSerializer.Serialize(tenants, new JsonSerializerOptions { WriteIndented = true });

            using (MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String))
            {
                try
                {
                    await conn.OpenAsync();

                    // Update tenants for the user
                    string query = "UPDATE accounts SET tenants = @tenants WHERE username = @username;";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@tenants", new_tenants_json);
                    cmd.Parameters.AddWithValue("@username", username);

                    Logging.Handler.Debug("Assign_Tenant_To_User", "MySQL_Prepared_Query", query);

                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Assign_Tenant_To_User", "MySQL_Query", ex.ToString());
                }
            }
        }
    }
}
