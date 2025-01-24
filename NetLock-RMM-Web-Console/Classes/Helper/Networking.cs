using MudBlazor;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;

namespace NetLock_RMM_Web_Console.Classes.Helper
{
    public class Networking
    {
        // Test connection to url
        public static async Task<bool> Test_Connection(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(5); // Set timeout

                    // Send a HEAD request to the server
                    var response = await client.GetAsync(url);

                    // Check if the status code is OK
                    if (response.IsSuccessStatusCode)
                        return true;
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    // Log error (make sure to implement your logging handler)
                    Logging.Handler.Error("Networking.Test_Connection", url, ex.ToString());
                    return false;
                }
            }
        }

        public static async Task<string> HTTP_Post_Request_Json(string url, string json, bool admin_api)
        {
            try
            {
                Logging.Handler.Debug("Classes.Networking.HTTP_Post_Request_Json", "begin post request: url", url);

                var jsonContent = new StringContent(json, Encoding.UTF8, "application/json");

                string api_key = await Classes.MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "files_api_key");

                using (var httpClient = new HttpClient())
                {
                    // Set the API key in the header
                    if (admin_api)
                        httpClient.DefaultRequestHeaders.Add("x-api-key", api_key);

                    // Debug output for the upload URL
                    Logging.Handler.Debug("Classes.Networking.HTTP_Post_Request_Json -> OK", "url", url);

                    // POST request with JSON data
                    var response = await httpClient.PostAsync(url, jsonContent);

                    // Process the answer
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        Logging.Handler.Debug("/manage_files -> OK", "result", result);

                        if (result == "Unauthorized.")
                        {
                            return "unauthorized";
                        }
                        else
                            return result;
                    }
                    else
                    {
                        Logging.Handler.Error("/manage_files -> OK", "response", response.StatusCode.ToString());
                        return "error";
                    }
                }

            }
            catch (Exception ex)
            {
                Logging.Handler.Error("/manage_files -> OK", "", ex.ToString());
                return "error";
            }
        }
    }
}
