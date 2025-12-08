using System.Configuration;
using MySqlConnector;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using NetLock_RMM_Server;
using NetLock_RMM_Server.Configuration;

namespace Helper.Notifications
{
    public class Webhook
    {
        public static async Task<bool> Send_Message(string id, string netlock_tenant_name, string netlock_location_name, string netlock_device_name, string netlock_date, string netlock_reported_by, string netlock_event, string netlock_description)
        {
            string url = string.Empty;
            string method = "POST";
            string? requestBody = null;
            string? requestHeaders = null;

            MySqlConnection conn = new MySqlConnection(MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand("SELECT json FROM webhook_notifications WHERE id = @id;", conn);
                command.Parameters.AddWithValue("@id", id);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            string jsonData = reader["json"].ToString() ?? string.Empty;
                            
                            if (!string.IsNullOrWhiteSpace(jsonData))
                            {
                                try
                                {
                                    var config = JsonSerializer.Deserialize<WebhookConfig>(jsonData);
                                    if (config != null)
                                    {
                                        url = config.url ?? string.Empty;
                                        method = config.method ?? "POST";
                                        requestBody = config.requestBody;
                                        requestHeaders = config.requestHeaders;
                                    }
                                }
                                catch (JsonException ex)
                                {
                                    Logging.Handler.Error("Helper.Notifications.Webhook", "Send_Message.Parse_JSON", ex.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.Notifications.Webhook", "Send_Message.Query_Webhook_Info", ex.ToString());
                return false;
            }
            finally
            {
                await conn.CloseAsync();
            }

            // Replace variables in request body
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                requestBody = requestBody.Replace("$netlock_tenant_name", netlock_tenant_name);
                requestBody = requestBody.Replace("$netlock_location_name", netlock_location_name);
                requestBody = requestBody.Replace("$netlock_device_name", netlock_device_name);
                requestBody = requestBody.Replace("$netlock_date", netlock_date);
                requestBody = requestBody.Replace("$netlock_reported_by", netlock_reported_by);
                requestBody = requestBody.Replace("$netlock_event", netlock_event);
                requestBody = requestBody.Replace("$netlock_description", netlock_description);
            }
            
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);

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
                                if (!string.IsNullOrWhiteSpace(header.Key) && !string.IsNullOrWhiteSpace(header.Value))
                                {
                                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                                }
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        Logging.Handler.Error("Helper.Notifications.Webhook", "Send_Message.Parse_Headers", ex.ToString());
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
                        return false;
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Logging.Handler.Error("Helper.Notifications.Webhook", "Send_Message.Send", $"status_code: {response.StatusCode}");
                }

                if (response.IsSuccessStatusCode)
                    return true;
                else
                    return false;
            }
            catch (HttpRequestException ex)
            {
                Logging.Handler.Error("Helper.Notifications.Webhook", "Send_Message.Send", ex.ToString());
                return false;
            }
            catch (TaskCanceledException ex)
            {
                Logging.Handler.Error("Helper.Notifications.Webhook", "Send_Message.Send", ex.ToString());
                return false;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.Notifications.Webhook", "Send_Message.Send", ex.ToString());
                return false;
            }
        }

        private class WebhookConfig
        {
            public string? url { get; set; }
            public string? method { get; set; }
            public string? requestBody { get; set; }
            public string? requestHeaders { get; set; }
        }
    }
}
