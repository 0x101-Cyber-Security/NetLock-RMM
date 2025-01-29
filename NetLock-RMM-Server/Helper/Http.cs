using System.Runtime.Intrinsics.X86;
using System;

namespace Helper
{
    public class Http
    {
        public static async Task<bool> Download_File(string url, string path)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "NetLock RMM Server Backend");
                    client.Timeout = TimeSpan.FromMinutes(60); 

                    HttpResponseMessage response = null;

                    response = await client.GetAsync(url);

                    response.EnsureSuccessStatusCode();

                    using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.Http.Download_File", "General error", ex.ToString());
                return false;
            }
        }
    }
}
