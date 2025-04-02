using System.Runtime.Intrinsics.X86;
using System;
using System.Net;

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
                    client.Timeout = TimeSpan.FromMinutes(60); // Long timeout for large files

                    // With ResponseHeadersRead we start the streaming as soon as the headers have been received.
                    using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        long? contentLength = response.Content.Headers.ContentLength;
                        if (contentLength.HasValue)
                        {
                            Logging.Handler.Debug("Helper.Http.Download_File", "Content-Length", contentLength.Value.ToString());
                        }
                        else
                        {
                            Logging.Handler.Error("Helper.Http.Download_File", "Content-Length", "No value specified.");
                        }

                        using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                        {
                            byte[] buffer = new byte[81920]; // 80 KB Buffer
                            long totalBytesRead = 0;
                            int bytesRead;
                            int progressBarWidth = 50; // Width of the loading beam

                            // Download loop
                            while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalBytesRead += bytesRead;

                                if (contentLength.HasValue)
                                {
                                    double progress = (double)totalBytesRead / contentLength.Value;
                                    int progressBlocks = (int)(progress * progressBarWidth);
                                    string progressBar = "[" + new string('#', progressBlocks) + new string('-', progressBarWidth - progressBlocks) + "]";

                                    // Conversion from bytes to MB
                                    double totalMB = contentLength.Value / (1024.0 * 1024.0);
                                    double downloadedMB = totalBytesRead / (1024.0 * 1024.0);

                                    Console.Write($"\r{progressBar} {progress * 100:0.00}% - {downloadedMB:0.00} MB / {totalMB:0.00} MB");
                                }
                                else
                                {
                                    double downloadedMB = totalBytesRead / (1024.0 * 1024.0);
                                    Console.Write($"\rDownloaded {downloadedMB:0.00} MB");
                                }
                            }

                            Console.WriteLine(); // Line break after download
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.Http.Download_File", "General error", ex.ToString());
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
