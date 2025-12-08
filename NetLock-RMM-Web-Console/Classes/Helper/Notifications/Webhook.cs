using System.Text;
using System.Text.Json;

namespace NetLock_RMM_Web_Console.Classes.Helper.Notifications;

public class Webhook
{
    /// <summary>
    /// Sends an HTTP request to a webhook URL
    /// </summary>
    /// <param name="url">The webhook URL</param>
    /// <param name="method">HTTP method (GET, POST, PUT, DELETE, PATCH)</param>
    /// <param name="requestBody">The request body as JSON string (optional)</param>
    /// <param name="requestHeaders">The request headers as JSON string (optional, format: {"Header-Name": "Value"})</param>
    /// <returns>Tuple with success status, HTTP status code and response body</returns>
    /// <example>
    /// var (success, statusCode, response) = await Webhook.SendAsync(
    ///     url: "https://example.com/webhook",
    ///     method: "POST",
    ///     requestBody: "{\"message\": \"Test\"}",
    ///     requestHeaders: "{\"Authorization\": \"Bearer token123\"}"
    /// );
    /// </example>
    
    public static async Task<(bool success, int statusCode, string response)> SendAsync(string url, string method = "POST", string? requestBody = null, string? requestHeaders = null)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Add request headers
            if (!string.IsNullOrWhiteSpace(requestHeaders))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(requestHeaders);
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            // Avoid duplicates and invalid headers
                            if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
                            {
                                // Content-Type is handled separately
                                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                                    continue;
                                
                                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignore malformed header JSON
                }
            }

            HttpResponseMessage response;
            HttpContent? content = null;

            // Prepare request body
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            }

            // Execute HTTP method
            switch (method.ToUpperInvariant())
            {
                case "GET":
                    response = await httpClient.GetAsync(url);
                    break;

                case "POST":
                    response = await httpClient.PostAsync(url, content);
                    break;

                case "PUT":
                    response = await httpClient.PutAsync(url, content);
                    break;

                case "DELETE":
                    if (content != null)
                    {
                        var request = new HttpRequestMessage(HttpMethod.Delete, url) { Content = content };
                        response = await httpClient.SendAsync(request);
                    }
                    else
                    {
                        response = await httpClient.DeleteAsync(url);
                    }
                    break;

                case "PATCH":
                    var patchRequest = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
                    response = await httpClient.SendAsync(patchRequest);
                    break;

                default:
                    return (false, 0, $"Invalid HTTP method: {method}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            return (response.IsSuccessStatusCode, (int)response.StatusCode, responseBody);
        }
        catch (HttpRequestException ex)
        {
            return (false, 0, $"HTTP request error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            return (false, 0, $"Request timeout: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, 0, $"Error sending webhook: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends an HTTP request to a webhook URL (synchronous version)
    /// </summary>
    public static (bool success, int statusCode, string response) Send(
        string url, 
        string method = "POST", 
        string? requestBody = null, 
        string? requestHeaders = null)
    {
        return SendAsync(url, method, requestBody, requestHeaders).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Validates if a URL is valid
    /// </summary>
    public static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validates if a JSON string is valid
    /// </summary>
    public static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return true;

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}