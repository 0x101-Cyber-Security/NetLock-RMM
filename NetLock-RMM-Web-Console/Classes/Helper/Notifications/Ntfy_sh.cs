using MySqlConnector;
using System.Data.Common;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Text;

namespace NetLock_RMM_Web_Console.Classes.Helper.Notifications
{
    public class Ntfy_sh
    {
        public static async Task<string> Send_Message(string id, string message)
        {
            Logging.Handler.Debug("Classes.Helper.Notifications.Ntfy_sh", id, message);

            string topic_url = String.Empty;
            string access_token = String.Empty;

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand("SELECT * FROM ntfy_sh_notifications WHERE id = @id;", conn);
                command.Parameters.AddWithValue("@id", id);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            topic_url = reader["topic_url"].ToString() ?? String.Empty;
                            access_token = reader["access_token"].ToString() ?? String.Empty;

                            Logging.Handler.Debug("Classes.Helper.Notifications.Ntfy_sh", "Send_Message.Query_Connector_Info", "Found topic_url for id: " + id + ": " + topic_url);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Helper.Notifications.Ntfy_sh", "Send_Message.Query_Connector_Info", ex.ToString());
            }
            finally
            {
                conn.Close();
            }

            //Send message
            try
            {
                if (access_token.Length < 3)
                {
                    using (var httpClient = new HttpClient())
                    {
                        var content = new StringContent(message, Encoding.UTF8, "text/plain");
                        var response = await httpClient.PostAsync(topic_url, content);

                        if (response.IsSuccessStatusCode) //Message sent successfully 
                        {
                            return "success";
                        }
                        else //Sending message failed
                        {
                            Logging.Handler.Error("Classes.Helper.Notifications.Microsoft_Teams", "Send_Message.Send(no auth)", "status_code: " + response.StatusCode.ToString() + " Url: " + topic_url);

                            return response.StatusCode.ToString();
                        }
                    }
                }
                else
                {
                    using (var httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {access_token}");

                        var content = new StringContent(message, Encoding.UTF8, "text/plain");
                        var response = await httpClient.PostAsync(topic_url, content);

                        if (response.IsSuccessStatusCode) //Message sent successfully 
                        {
                            return "success";
                        }
                        else //Sending message failed
                        {
                            Logging.Handler.Error("Classes.Helper.Notifications.Microsoft_Teams", "Send_Message.Send(with auth)", "status_code: " + response.StatusCode.ToString() + " Url: " + topic_url);

                            return response.StatusCode.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Helper.Notifications.Ntfy_sh", "Send_Message.Send", "status_code: " + ex.ToString() + " Url: " + topic_url);
                return ex.Message;
            }
        }
    }
}
