using Microsoft.AspNetCore.Http;
using MySqlConnector;
using System.Data.Common;
using System.Net;
using Telegram.Bot;
using NetLock_RMM_Server.Configuration;

namespace Helper.Notifications
{
    public class Telegram
    {
        public static async Task<bool> Send_Message(string id, string message)
        {
            string bot_token = String.Empty;
            string chat_id = String.Empty;

            MySqlConnection conn = new MySqlConnection(MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand("SELECT * FROM telegram_notifications WHERE id = @id;", conn);
                command.Parameters.AddWithValue("@id", id);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            bot_token = reader["bot_token"].ToString() ?? String.Empty;
                            chat_id = reader["chat_id"].ToString() ?? String.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Helper.Notifications.Telegram", "Send_Message.Query_Connector_Info", ex.ToString());
            }
            finally
            {
                conn.Close();
            }

            //Send message
            try
            {
                var botClient = new TelegramBotClient(bot_token);
                var messageText = message;

                await botClient.SendTextMessageAsync(chat_id, messageText);

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.Helper.Notifications.Telegram", "Send_Message.Send", "status_code: " + ex.ToString());
                return false;
            }       

        }
    }
}
