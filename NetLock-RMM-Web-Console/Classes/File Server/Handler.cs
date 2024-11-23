using MySqlConnector;
using System.Data.Common;

namespace NetLock_RMM_Web_Console.Classes.File_Server
{
    public class Handler
    {
        // Get password of file by guid from database
        public static async Task<string> Get_File_Password_By_Guid(string guid)
        {
            try
            {
                string password = "";
                using (var conn = new MySqlConnection(Configuration.MySQL.Connection_String))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT password FROM files WHERE guid = @guid", conn))
                    {
                        cmd.Parameters.AddWithValue("@guid", guid);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                password = reader.GetString(0);
                            }
                        }
                    }
                }
                return password;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("File_Server.Handler.Get_File_Password_By_Guid", guid, ex.ToString());
                return String.Empty;
            }
        }
    }
}
