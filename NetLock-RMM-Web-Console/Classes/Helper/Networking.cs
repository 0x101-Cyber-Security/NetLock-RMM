using System.Net;

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
    }
}
