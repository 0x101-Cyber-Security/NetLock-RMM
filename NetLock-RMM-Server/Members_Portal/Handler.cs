using crypto;
using MySqlConnector;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net;
using System.Net.Http;
using System.Text;

namespace NetLock_RMM_Server.Members_Portal
{
    public class Handler
    {
        public static async Task <bool> Update_License_Information()
        {
            try
            {
                // Get Api Key
                string api_key = await MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "members_portal_api_key");

                // Check if the API key is valid
                if (String.IsNullOrEmpty(api_key))
                {
                    Logging.Handler.Debug("Members_Portal.Update_Authorized_Devices_License_Information", "api key is empty", "aborting");
                    return false;
                }

                // Contact the members portal API to verify the API key
                using (var httpClient = new HttpClient())
                {
                    // Set the content type header
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Add("X-API-Key", api_key);

                    // Create the JSON data
                    var json = JsonSerializer.Serialize(new
                    {
                        licenses_used = await MySQL.Handler.Get_Authorized_Devices_Count_30_Days(),
                        hwid = await _x101.HWID_System.ENGINE.Get_Hwid(),
                    });

                    // Send the JSON data to the server
                    var response = await httpClient.PostAsync(Application_Settings.IsLiveEnvironment ? Application_Settings.Members_Portal_Api_Url_Live + "/api/membership/information/history" : Application_Settings.Members_Portal_Api_Url_Test + "/api/membership/information/history", new StringContent(json, Encoding.UTF8, "application/json"));

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, handle the response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Handler.Debug("Members_Portal.Update_Authorized_Devices_License_Information", "result", result);

                        // Parse the JSON response
                        if (result == "unauthorized")
                        {
                            Logging.Handler.Debug("Members_Portal.Update_Authorized_Devices_License_Information", "result", "unauthorized");
                            return false;
                        }
                        else
                        {
                            Logging.Handler.Debug("Members_Portal.Update_Authorized_Devices_License_Information", "result", "authorized");
                            return true;
                        }
                    }
                    else
                    {
                        // Request was not successful, log the error
                        Logging.Handler.Debug("Members_Portal.Update_Authorized_Devices_License_Information", "error", response.StatusCode.ToString());
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Debug("Members_Portal.Update_Authorized_Devices_License_Information", "error", ex.ToString());
                return false;
            }
        }
    }
}
