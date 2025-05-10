using MySqlConnector;
using System.Data.Common;

namespace NetLock_RMM_Server.Sensors
{
    public class Handler
    {
        public static async Task <bool> Run_Sensors()
        {
            try
            {
                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

                try
                {
                    await conn.OpenAsync();

                    string query = "SELECT * FROM sensors;";

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    
                    Logging.Handler.Debug("Sensors.Handler.Run_Sensors", "MySQL_Prepared_Query", query);

                    using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                // Device offline
                                if (reader["category"].ToString() == "7")
                                {

                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Sensors.Handler.Run_Sensors", "MySQL_Query", ex.ToString());
                }
                finally
                {
                    conn.Close();
                }

            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Sensors.Handler.Run_Sensors", "Error in Run_Sensors", ex.ToString());
            }

            return true;
        }
    }
}
