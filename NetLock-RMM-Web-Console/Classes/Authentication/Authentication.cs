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

                MySqlCommand cmd = new MySqlCommand("SELECT * FROM accounts WHERE username = @username;", conn);
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
    }
}
