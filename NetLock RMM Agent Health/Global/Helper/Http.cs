using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Global.Helper
{
    internal class Http
    {
        public static async Task<bool> DownloadFileAsync(bool ssl, string url, string destinationFilePath, string guid)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Package-Guid", guid);

                    HttpResponseMessage response = null;

                    if (ssl)
                        response = await client.GetAsync("https://" + url);
                    else
                        response = await client.GetAsync("http://" + url);

                    response.EnsureSuccessStatusCode();

                    using (var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Error("DownloadFileAsync", "General error", ex.ToString());
                Console.WriteLine($"DownloadFileAsync: An error occurred: {ex.Message}");
                return false;
            }
        }

        // Get hash
        public static async Task<string> GetHashAsync(bool ssl, string url, string guid)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Package-Guid", guid);

                    HttpResponseMessage response = null;

                    if (ssl)
                        response = await client.GetAsync("https://" + url);
                    else
                        response = await client.GetAsync("http://" + url);

                    response.EnsureSuccessStatusCode();

                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Logging.Error("GetHashAsync", "General error", ex.ToString());
                Console.WriteLine($"GetHashAsync: An error occurred: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
