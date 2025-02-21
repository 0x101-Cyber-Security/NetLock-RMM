using MudBlazor;
using MySqlConnector;
using System.Text.Json;

namespace NetLock_RMM_Web_Console.Classes.Members_Portal
{
    public class Handler
    {
        public static async Task<bool> Request_Membership_License_Information(string members_portal_api_key)
        {
            try
            {
                bool unauthorized = false;

                string members_portal_status = String.Empty;
                string members_portal_license_name = String.Empty;
                int members_portal_licenses_used = 0;
                int members_portal_licenses_max = 0;
                bool members_portal_licenses_hard_limit = false;
                string package_url = String.Empty;
                 
                // Contact the members portal API to verify the API key
                using (var httpClient = new HttpClient())
                {
                    // Set the content type header
                    httpClient.DefaultRequestHeaders.Add("X-API-Key", members_portal_api_key);

                    // Send the JSON data to the server
                    var response = await httpClient.GetAsync(Application_Settings.IsLiveEnvironment ? Application_Settings.Members_Portal_Api_Url_Live + "/api/membership/information" : Application_Settings.Members_Portal_Api_Url_Test + "/api/membership/information");

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, handle the response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "result", result);

                        // Parse the JSON response
                        if (result == "unauthorized")
                        {
                            Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "result", "unauthorized");
                            unauthorized = true;
                        }
                        else
                        {
                            Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "result", "authorized");

                            // Extract response
                            using (JsonDocument document = JsonDocument.Parse(result))
                            {
                                JsonElement status_element = document.RootElement.GetProperty("status");
                                members_portal_status = status_element.ToString();

                                JsonElement name_element = document.RootElement.GetProperty("name");
                                members_portal_license_name = name_element.ToString();

                                JsonElement licenses_used_element = document.RootElement.GetProperty("licenses_used");
                                members_portal_licenses_used = licenses_used_element.GetInt32();

                                JsonElement licenses_max_element = document.RootElement.GetProperty("licenses_max");
                                members_portal_licenses_max = licenses_max_element.GetInt32();

                                JsonElement licenses_hard_limit_element = document.RootElement.GetProperty("licenses_hard_limit");
                                members_portal_licenses_hard_limit = licenses_hard_limit_element.GetBoolean();

                                JsonElement package_url_element = document.RootElement.GetProperty("package_url");
                                package_url = package_url_element.ToString();
                            }

                            // Save to settings table
                            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                            try
                            {
                                await conn.OpenAsync();

                                MySqlCommand command = new MySqlCommand("UPDATE settings SET members_portal_api_key = @members_portal_api_key, members_portal_license_name = @members_portal_license_name, members_portal_license_status = @members_portal_license_status, members_portal_licenses_used = @members_portal_licenses_used, members_portal_licenses_max = @members_portal_licenses_max, members_portal_licenses_hard_limit = @members_portal_licenses_hard_limit, package_provider_url = @package_provider_url;", conn);
                                command.Parameters.AddWithValue("@members_portal_api_key", members_portal_api_key);
                                command.Parameters.AddWithValue("@members_portal_license_name", members_portal_license_name);
                                command.Parameters.AddWithValue("@members_portal_license_status", members_portal_status);
                                command.Parameters.AddWithValue("@members_portal_licenses_used", members_portal_licenses_used);
                                command.Parameters.AddWithValue("@members_portal_licenses_max", members_portal_licenses_max);
                                command.Parameters.AddWithValue("@members_portal_licenses_hard_limit", members_portal_licenses_hard_limit);
                                command.Parameters.AddWithValue("@package_provider_url", package_url);

                                await command.ExecuteNonQueryAsync();

                                return true;
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Error("/system -> Save_Package_Provider_URL", "general_error", ex.ToString());
                                return false;
                            }
                            finally
                            {
                                conn.Close();
                            }
                        }
                    }
                    else
                    {
                        Logging.Handler.Debug("Online_Mode.Handler.Authenticate", "response", "unauthorized");
                        unauthorized = true;
                    }

                    // Check if the API key is unauthorized, if the case, reset settings
                    if (unauthorized)
                    {
                        // Save to settings table
                        MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                        try
                        {
                            await conn.OpenAsync();

                            MySqlCommand command = new MySqlCommand("UPDATE settings SET members_portal_api_key = @members_portal_api_key, members_portal_license_name = @members_portal_license_name, members_portal_license_status = @members_portal_license_status, members_portal_licenses_used = @members_portal_licenses_used, members_portal_licenses_max = @members_portal_licenses_max, members_portal_licenses_hard_limit = @members_portal_licenses_hard_limit;", conn);
                            command.Parameters.AddWithValue("@members_portal_api_key", members_portal_api_key);
                            command.Parameters.AddWithValue("@members_portal_license_name", "-");
                            command.Parameters.AddWithValue("@members_portal_license_status", "Expired");
                            command.Parameters.AddWithValue("@members_portal_licenses_used", "0");
                            command.Parameters.AddWithValue("@members_portal_licenses_max", "0");
                            command.Parameters.AddWithValue("@members_portal_licenses_hard_limit", "0");

                            await command.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Error("/system -> Save_Package_Provider_URL", "general_error", ex.ToString());
                        }
                        finally
                        {
                            conn.Close();
                        }

                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/system ->   ", "general_error", ex.ToString());
                return false;
            }
        }
    }
}
