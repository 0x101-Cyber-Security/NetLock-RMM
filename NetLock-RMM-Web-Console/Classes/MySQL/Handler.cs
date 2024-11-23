using MySqlConnector;
using System.Data.Common;
using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using MudBlazor;
using System.ComponentModel;

namespace NetLock_RMM_Web_Console.Classes.MySQL
{
    public class Handler
    {
        public static async Task <bool> Test_Connection()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                conn.Open();

                string sql = "SELECT * FROM clients;";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();

                conn.Close();
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> Check_Duplicate(string query)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();

                Logging.Handler.Debug("Classes.MySQL.Handler.Execute_Command", "Query", query);

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Execute_Command", "Query: " + query, ex.Message);
                conn.Close();
                return false;
            }
            finally
            {
                conn.Close();
            }
        }


        public static async Task<bool> Execute_Command(string query)
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.ExecuteNonQuery();

                Logging.Handler.Debug("Classes.MySQL.Handler.Execute_Command", "Query", query);

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Execute_Command", "Query: " + query,  ex.Message);
                conn.Close();
                return false;
            }
            finally
            {
                conn.Close();
            }
        }

        public static async Task<string> Quick_Reader(string query, string item)
        {
            Logging.Handler.Debug("Classes.MySQL.Handler.Quick_Reader", "query", query);
            Logging.Handler.Debug("Classes.MySQL.Handler.Quick_Reader", "item", query);

            string result = String.Empty;

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                Logging.Handler.Debug("Classes.MySQL.Handler.Quick_Reader", "MySQL_Prepared_Query", query); //Output prepared query

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            result = reader[item].ToString() ?? String.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Handler.Quick_Reader", "query: " + query + " item: " + item, ex.Message);
                conn.Close();
            }
            finally
            {
                conn.Close();
            }

            return result;
        }
    }
}
