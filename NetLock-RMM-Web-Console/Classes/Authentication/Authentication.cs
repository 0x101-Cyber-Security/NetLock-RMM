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

                MySqlDataReader reader = await cmd.ExecuteReaderAsync();

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
                await conn.CloseAsync();
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
        
        /// <summary>
        /// Checks if a user is authorized for SSO login.
        /// Returns true only if the user exists in the database and has role "SSO".
        /// Does NOT create new users automatically.
        /// </summary>
        /// <param name="email">User email from SSO provider</param>
        /// <returns>True if user is authorized for SSO, false otherwise</returns>
        public static async Task<bool> CheckSsoUserAuthorization(string email)
        {
            try
            {
                using var conn = new MySqlConnector.MySqlConnection(Configuration.MySQL.Connection_String);
                
                await conn.OpenAsync();

                // Check if user exists AND has SSO role
                var checkCommand = new MySqlConnector.MySqlCommand("SELECT COUNT(*) FROM accounts WHERE username = @email AND role = 'SSO';", conn);
                checkCommand.Parameters.AddWithValue("@email", email);
                
                var count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                
                if (count > 0)
                {
                    // User exists with SSO role, update last login
                    var updateCommand = new MySqlConnector.MySqlCommand(
                        "UPDATE accounts SET last_login = @now, ip_address = 'SSO' WHERE username = @email", conn);
                    updateCommand.Parameters.AddWithValue("@email", email);
                    updateCommand.Parameters.AddWithValue("@now", DateTime.Now);
                    await updateCommand.ExecuteNonQueryAsync();
                    
                    Console.WriteLine($"SSO: User authorized and login updated: {email}");
                    Logging.Handler.Debug("SSO", "User Authorized", $"User {email} has SSO role and is authorized");
                    return true;
                }
                else
                {
                    Console.WriteLine($"SSO: User not authorized or does not have SSO role: {email}");
                    Logging.Handler.Debug("SSO", "User Not Authorized", $"User {email} not found or does not have SSO role");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SSO: Error checking user authorization: {ex.Message}");
                Logging.Handler.Error("SSO", "Authorization Check Error", ex.ToString());
                return false;
            }
        }
    }
}
