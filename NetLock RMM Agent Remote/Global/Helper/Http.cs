using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Global.Helper
{
    internal class Http
    {
        public static async Task<string> DownloadFileAsync(bool ssl, string url, string destinationFilePath, string package_guid)
        {
            string result = string.Empty;
            try
            {
                Logging.Debug("Helper.Http.DownloadFileAsync", "Trying to download file", "URL: " + url + " Destination: " + destinationFilePath);

                using (HttpClientHandler handler = new HttpClientHandler { AllowAutoRedirect = true })
                using (HttpClient client = new HttpClient(handler, disposeHandler: true))
                {
                    client.Timeout = TimeSpan.FromHours(24); // Set timeout to 24 hours
                    client.DefaultRequestHeaders.Add("Package-Guid", package_guid);

                    HttpResponseMessage response = null;

                    if (ssl)
                        response = await client.GetAsync("https://" + url, HttpCompletionOption.ResponseHeadersRead); // Streaming mode
                    else
                        response = await client.GetAsync("http://" + url, HttpCompletionOption.ResponseHeadersRead); // Streaming mode

                    response.EnsureSuccessStatusCode();

                    // Stream the content directly to the file
                    using (var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true))
                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                    {
                        await httpStream.CopyToAsync(fileStream);  // Efficiently copies the stream
                    }

                    Logging.Debug("Helper.Http.DownloadFileAsync", "Download successful", "URL: " + url + " Destination: " + destinationFilePath);

                    // Read and return the server response (in case it's part of the response body)
                    result = await response.Content.ReadAsStringAsync();
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Http.DownloadFileAsync", "General error", ex.ToString());
                return ex.Message;
            }
        }

        public static async Task<string> UploadFileAsync(bool ssl, string url, string filePath, string package_guid)
        {
            string result = string.Empty;

            try
            {
                Logging.Debug("Helper.Http.UploadFileAsync", "Trying to upload file", "URL: " + url + " Source: " + filePath);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Package-Guid", package_guid);

                    using (MultipartFormDataContent content = new MultipartFormDataContent())
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

                        HttpResponseMessage response = null;

                        if (ssl)
                            response = await client.PostAsync("https://" + url, content);
                        else
                            response = await client.PostAsync("http://" + url, content);

                        // Ensure the request was successful
                        response.EnsureSuccessStatusCode();

                        // Read the server response content as a string
                        result = await response.Content.ReadAsStringAsync();
                    }

                    Logging.Debug("Helper.Http.UploadFileAsync", "Upload successful", "URL: " + url + " Source: " + filePath);

                    // Return the server response
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Http.UploadFileAsync", "General error", ex.ToString());
                return ex.Message;
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
                Logging.Error("Helper.Http.GetHashAsync", "General error", ex.ToString());
                return string.Empty;
            }
        }
    }
}
