using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.JSInterop;
using MySqlConnector;
using System.Data.Common;
using System.Text.Json;

namespace NetLock_RMM_Web_Console.Classes.Authentication
{
    public class Permissions
    {
        public class Permissions_Tenants_Activation_State
        {
            public string id { get; set; } = String.Empty;
            public string guid { get; set; } = String.Empty;

        }

        public static async Task<bool> Verify_Tenants(string username, string tenant_guid)
        {
            string permissions_tenants_json = String.Empty;
            List<string> permissions_tenants_list = new List<string> { };

            //Get permissions
            string query = "SELECT * FROM `accounts` WHERE username = @username;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@username", username);

                Logging.Handler.Debug("Classes.Authentication.Verify_Tenants", "query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            permissions_tenants_json = reader["tenants"].ToString() ?? String.Empty;
                        }
                    }
                }

                Logging.Handler.Debug("Classes.Authentication.Verify_Tenants", "permissions_tenants_json", permissions_tenants_json);

                //Extract tenants from json
                if (!String.IsNullOrEmpty(permissions_tenants_json))
                {
                    //Set the activation state for the tenants
                    try
                    {
                        List<Permissions_Tenants_Activation_State> tenants_activation_state_list = JsonSerializer.Deserialize<List<Permissions_Tenants_Activation_State>>(permissions_tenants_json);

                        foreach (var tenant in tenants_activation_state_list)
                        {
                            Logging.Handler.Debug("Classes.Authentication.Verify_Tenants", "foreach tenant", tenant.id);

                            permissions_tenants_list.Add(tenant.id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Classes.Authentication.Verify_Tenants (permissions_tenants_json deserialize)", "Result", ex.ToString());
                        return false;
                    }
                }
                else
                {
                    Logging.Handler.Debug("Classes.Authentication.Verify_Tenants (permissions_tenants_json deserialize)", "Result", "Empty");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Authentication.Verify_Tenants", "general_error (force logout)", ex.ToString());
                return false;
            }
            finally
            {
                await conn.CloseAsync();
            }

            //Check if the tenant is in the list
            if (permissions_tenants_list.Contains(tenant_guid))
            {
                Logging.Handler.Debug("Classes.Authentication.Verify_Tenants", "tenant_guid", tenant_guid);
                return true;
            }
            else
            {
                Logging.Handler.Debug("Classes.Authentication.Verify_Tenants", "tenant_guid", "Not found");
                return false;
            }
        }

        public static async Task<List<string>> Get_Tenants(string username, bool guid)
        {
            string permissions_tenants_json = String.Empty;
            List<string> permissions_tenants_list = new List<string> { };

            //Get permissions
            string query = "SELECT * FROM `accounts` WHERE username = @username;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@username", username);

                Logging.Handler.Debug("Classes.Authentication.Get_Tenants", "query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            permissions_tenants_json = reader["tenants"].ToString() ?? String.Empty;
                        }
                    }
                }

                Logging.Handler.Debug("Classes.Authentication.Get_Tenants", "permissions_tenants_json", permissions_tenants_json);

                //Extract tenants from json
                if (!String.IsNullOrEmpty(permissions_tenants_json))
                {
                    //Set the activation state for the tenants
                    try
                    {
                        List<Permissions_Tenants_Activation_State> tenants_activation_state_list = JsonSerializer.Deserialize<List<Permissions_Tenants_Activation_State>>(permissions_tenants_json);

                        foreach (var tenant in tenants_activation_state_list)
                        {
                            Logging.Handler.Debug("Classes.Authentication.Get_Tenants", "foreach tenant", tenant.id);

                            if (guid)
                                permissions_tenants_list.Add(tenant.guid);
                            else
                                permissions_tenants_list.Add(tenant.id);
                        }

                        // Return the list of tenants
                        return permissions_tenants_list;
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Classes.Authentication.Get_Tenants (permissions_tenants_json deserialize)", "Result", ex.ToString());
                        return new List<string> { };
                    }
                }
                else
                {
                    Logging.Handler.Debug("Classes.Authentication.Get_Tenants (permissions_tenants_json deserialize)", "Result", "Empty");
                    return new List<string> { };
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Authentication.Get_Tenants", "general_error (force logout)", ex.ToString());
                return new List<string> { };
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Check if admin has access to all tenants
        public static async Task<bool> Verify_Tenants_Full_Access(string username)
        {
            try
            {
                if (String.IsNullOrEmpty(username))
                {
                    Logging.Handler.Debug("Classes.Authentication.Verify_Tenants_Full_Access", "username", "Empty, logout user");
                    return false;
                }

                List<string> permissions_tenants_list = await Get_Tenants(username, true);
                string results = await MySQL.Handler.Quick_Reader("SELECT * FROM tenants", "guid");
                List<string> all_tenants_list = results.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

                Logging.Handler.Debug("Classes.Authentication.Verify_Tenants_Full_Access", "permissions_tenants_list_raw", string.Join(", ", permissions_tenants_list));

                // Filter out tenant GUIDs that no longer exist
                permissions_tenants_list = permissions_tenants_list.Where(guid => all_tenants_list.Contains(guid)).ToList();

                Logging.Handler.Debug("Classes.Authentication.Verify_Tenants_Full_Access", "permissions_tenants_list_filtered", string.Join(", ", permissions_tenants_list));
                Logging.Handler.Debug("Classes.Authentication.Verify_Tenants_Full_Access", "all_tenants_list", string.Join(", ", all_tenants_list));

                // Wenn die gefilterte Liste leer ist, kein Zugriff
                if (permissions_tenants_list.Count == 0)
                    return false;

                // Prüfe, ob der User auf alle noch existierenden Tenants Zugriff hat
                if (!all_tenants_list.All(permissions_tenants_list.Contains))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Authentication.Verify_Tenants_Full_Access", "general_error (force logout)", ex.ToString());
                return false;
            }
        }

        public static async Task<bool> Verify_Permission(string username, string permission_key)
        {
            if (String.IsNullOrEmpty(username))
            {
                Logging.Handler.Debug("Classes.Authentication.Verify_Tenants", "username", "Empty, logout user");
                return false;
            }

            string permissions_json = String.Empty;

            //Get permissions
            string query = "SELECT * FROM `accounts` WHERE username = @username;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                command.Parameters.AddWithValue("@username", username);

                Logging.Handler.Debug("Classes.Authentication.Verify_Tenants", "query", query);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            permissions_json = reader["permissions"].ToString() ?? String.Empty;
                        }
                    }
                }

                Logging.Handler.Debug("Classes.Authentication.Verify_Tenants", "permissions_json", permissions_json);

                //Extract permissions
                if (!String.IsNullOrEmpty(permissions_json))
                {
                    JsonDocument document = JsonDocument.Parse(permissions_json);

                    // Return the permission result
                    try
                    {
                        JsonElement permission_element = document.RootElement.GetProperty(permission_key);
                        return permission_element.GetBoolean();
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Error("Classes.Authentication.Verify_Tenants", "permissions_json (permissions_dashboard_enabled)", ex.ToString());
                        return false;
                    }
                }
                else if (permissions_json == "[]")
                {
                    Logging.Handler.Debug("Classes.Authentication.Verify_Tenants", "permissions_json", "Empty, logout user");
                    return false;
                }
                else
                {
                    Logging.Handler.Debug("Classes.Authentication.Verify_Tenants", "permissions_json", "Empty, logout user");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Authentication.Verify_Tenants", "general_error (force logout)", ex.ToString());
                return false;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
