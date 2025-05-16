using static NetLock_RMM_Web_Console.Components.Pages.Collections.Scripts.Manage_Scripts;
using System.Net.Http.Headers;
using System.Text.Json;

namespace NetLock_RMM_Web_Console.Classes.Helper
{
    public class Http
    {
        public static async Task<string> Get_Request_With_Api_Key(string url)
        {
            try
            {
                string api_key = await Classes.MySQL.Handler.Get_Api_Key();

                using (var httpClient = new HttpClient())
                {
                    // Set Header
                    httpClient.DefaultRequestHeaders.Add("X-Api-Key", api_key);

                    // GET Request absenden
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        // Read response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Handler.Debug("Online_Mode.Handler.Get_Request_With_Api_Key", "Result", result);

                        return result;
                    }
                    else
                    {
                        // Error handling
                        Logging.Handler.Debug("Online_Mode.Handler.Get_Request_With_Api_Key", "Request failed", response.ReasonPhrase);
                        return String.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Online_Mode.Handler.Get_Request_With_Api_Key", "General error", ex.ToString());
                return String.Empty;
            }
        }

        public static async Task <string> POST_Request_Json_With_Api_Key(string url, string json)
        {
            try
            {
                string api_key = await Classes.MySQL.Handler.Get_Api_Key();

                using (var httpClient = new HttpClient())
                {
                    // Set Header
                    httpClient.DefaultRequestHeaders.Add("X-Api-Key", api_key);

                    // POST Send request
                    var response = await httpClient.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                    if (response.IsSuccessStatusCode)
                    {
                        // Read response
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Handler.Debug("Online_Mode.Handler.POST_Request_Json_With_Api_Key", "Result", result);
                        return result;
                    }
                    else
                    {
                        // Error handling
                        Logging.Handler.Debug("Online_Mode.Handler.POST_Request_Json_With_Api_Key", "Request failed", response.ReasonPhrase);
                        return String.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Online_Mode.Handler.POST_Request_Json_With_Api_Key", "General error", ex.ToString());
                return String.Empty;
            }
        }
    }
}
