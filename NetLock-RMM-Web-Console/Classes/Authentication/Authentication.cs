using Microsoft.IdentityModel.Tokens;
using MySqlConnector;

namespace NetLock_RMM_Web_Console.Classes.Authentication
{
    public class User
    {
        public static async Task<bool> Verify_User(string username, string password)
        {
            bool isPasswordCorrect = false;

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand("SELECT * FROM accounts WHERE username = @username LIMIT 1;", conn);
                cmd.Parameters.AddWithValue("@username", username);

                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                        isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, reader["password"].ToString());
                }
                await reader.CloseAsync();

                return isPasswordCorrect;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("class", "Verify_User", ex.ToString());
                return false;
            }
            finally
            { 
                conn.Close();
            }
        }

        public static async Task Update_Remote_Session_Token(string username)
        {
            // Hash username to ensure it's not easily guessable
            string token_username = BCrypt.Net.BCrypt.HashPassword(username);

            // Generate a random token
            string token = Randomizer.Handler.Token(true, 32);

            // Add token with username
            token = token + token_username;

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
         
            try
            {
                await conn.OpenAsync();
                
                MySqlCommand cmd = new MySqlCommand("UPDATE accounts SET remote_session_token = @token WHERE username = @username;", conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@token", token);
                
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("class", "Update_Remote_Session_Token", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Get the remote session token for a user
        public static async Task<string> Get_Remote_Session_Token(string username)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            
            string token = string.Empty;
            
            try
            {
                await conn.OpenAsync();
            
                MySqlCommand cmd = new MySqlCommand("SELECT remote_session_token FROM accounts WHERE username = @username LIMIT 1;", conn);
                cmd.Parameters.AddWithValue("@username", username);
                
                MySqlDataReader reader = await cmd.ExecuteReaderAsync();
                
                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                        token = reader["remote_session_token"].ToString();
                }
               
                return token;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("class", "Get_Remote_Session_Token", ex.ToString());
                return string.Empty;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
