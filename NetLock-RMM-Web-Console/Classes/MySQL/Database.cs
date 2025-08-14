using MySqlConnector;
using System.Configuration;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace NetLock_RMM_Web_Console.Classes.MySQL
{
    public class Database
    {
        // Check connection
        public static async Task<bool> Check_Connection()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Check_Connection", "Result", ex.Message);
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        public static async Task<string> Get_Version()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SELECT VERSION();";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        return reader.GetString(0);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Version", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return String.Empty;
        }

        // Check if MySQL is used (not MariaDB) and if the version is supported by NetLock
        public static async Task<bool> Verify_Supported_SQL_Server()
        {
            try
            {
                await using var conn = new MySqlConnection(Configuration.MySQL.Connection_String);
                await conn.OpenAsync();

                const string query = "SELECT @@version, @@version_comment;";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    string version = reader.GetString(0);
                    string versionComment = reader.GetString(1);

                    Logging.Handler.Debug("Classes.MySQL.Database.Verify_Supported_SQL_Server", "version", version);
                    Logging.Handler.Debug("Classes.MySQL.Database.Verify_Supported_SQL_Server", "versionComment", versionComment);

                    // Check whether it is MariaDB
                    if (versionComment.IndexOf("mariadb", StringComparison.OrdinalIgnoreCase) >= 0)
                        return false;
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Verify_Supported_SQL_Server", "Fehler beim Überprüfen des SQL-Servers", ex.ToString());
                return false;
            }

            return true;
        }

        public static async Task<string> Get_Uptime()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SHOW GLOBAL STATUS LIKE 'Uptime';";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        return reader.GetString(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Uptime", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return String.Empty;
        }

        public static async Task<string> Get_Connected_Users()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            var result = new List<object>();

            try
            {
                await conn.OpenAsync();

                string query = "SELECT COUNT(*) AS ConnectedUserCount FROM information_schema.processlist WHERE user != 'system user';";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        int connectedUserCount = reader.GetInt32(reader.GetOrdinal("ConnectedUserCount"));
                        return connectedUserCount.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Connected_Users", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return "0";
        }

        public static async Task<string> Get_Active_Queries()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            var result = new List<object>();

            try
            {
                await conn.OpenAsync();

                string query = "SHOW FULL PROCESSLIST;";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        { 
                            // Adjust the correct index to obtain the required data.
                            var queryInfo = new
                            {
                                Id = reader.GetInt32(0),
                                User = reader.GetString(1),
                                Host = reader.GetString(2),
                                Database = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Command = reader.GetString(4),
                                Time = reader.GetInt32(5),
                                State = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Info = reader.IsDBNull(7) ? null : reader.GetString(7)
                            };

                            result.Add(queryInfo);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Active_Queries", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            string json = JsonSerializer.Serialize(result);

            Logging.Handler.Debug("Classes.MySQL.Database.Get_Active_Queries", "json", json);

            return json;
        }

        public static async Task<string> Get_Database_Size()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SELECT table_schema 'Database', SUM(data_length + index_length) / 1024 / 1024 'Size (MB)' " +
                               "FROM information_schema.tables WHERE table_schema = @DatabaseName GROUP BY table_schema;";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DatabaseName", Configuration.MySQL.Database);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        // Hole die Größe als Dezimalwert
                        var sizeInMB = reader.GetDecimal(1);

                        // Runden auf zwei Nachkommastellen
                        var roundedSize = Math.Round(sizeInMB, 2);

                        // Rückgabe als String
                        return roundedSize.ToString() + " MB";
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Database_Size", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return String.Empty;
        }


        public static async Task<string> Get_Failed_Logins()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
            var result = new List<object>();

            try
            {
                await conn.OpenAsync();

                string query = "SELECT COUNT(*) AS FailedLoginCount FROM accounts WHERE reset_password = 1;";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        int failedLoginCount = reader.GetInt32(reader.GetOrdinal("FailedLoginCount"));
                        return failedLoginCount.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Failed_Logins", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return "0";
        }

        public static async Task<string> Get_Max_Connections()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SHOW VARIABLES LIKE 'max_connections';";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        return reader.GetString(1);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database.Get_Max_Connections", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }

            return String.Empty;
        }

        // Check table existing
        public static async Task<bool> Check_Table_Existing()
        {


            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                string query = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '" + Configuration.MySQL.Database + "' AND table_name = 'settings';";

                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    if (reader.HasRows && await reader.ReadAsync())
                    {
                        if (reader.GetInt32(0) > 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Check_Table_Existing", "Result", ex.Message);
            }
            finally
            {
                await conn.CloseAsync();
            }

            return false;
        }

        // Check database existing (gives back the amount of tables)
        public static async Task<bool> Check_Database_Existing()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String))
                {
                    await conn.OpenAsync();

                    string query = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = @database;";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@database", Configuration.MySQL.Database);

                        object result = await cmd.ExecuteScalarAsync();
                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Check_Database_Existing", "Result", ex.Message);
                return false;
            }
        }

        public static async Task<string> Check_NetLock_Database_Version()
        {
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                // Get assembly version
                string version = String.Empty;

                conn.Open();

                string query = "SELECT db_version FROM settings;";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows && reader.Read())
                    {
                        return reader["db_version"].ToString();
                    }
                }

                return version;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Check_Database_Version", "Result", ex.ToString());
                return String.Empty;
            }
            finally
            {
                conn.Close();
            }
        }

        // Installations scripts
        private static string installation_script_2_0_0_0 = @"LyohNDAxMDEgU0VUIEBPTERfQ0hBUkFDVEVSX1NFVF9DTElFTlQ9QEBDSEFSQUNURVJfU0VUX0NMSUVOVCAqLzsNCi8qITQwMTAxIFNFVCBOQU1FUyB1dGY4ICovOw0KLyohNTA1MDMgU0VUIE5BTUVTIHV0ZjhtYjQgKi87DQovKiE0MDEwMyBTRVQgQE9MRF9USU1FX1pPTkU9QEBUSU1FX1pPTkUgKi87DQovKiE0MDEwMyBTRVQgVElNRV9aT05FPScrMDA6MDAnICovOw0KLyohNDAwMTQgU0VUIEBPTERfRk9SRUlHTl9LRVlfQ0hFQ0tTPUBARk9SRUlHTl9LRVlfQ0hFQ0tTLCBGT1JFSUdOX0tFWV9DSEVDS1M9MCAqLzsNCi8qITQwMTAxIFNFVCBAT0xEX1NRTF9NT0RFPUBAU1FMX01PREUsIFNRTF9NT0RFPSdOT19BVVRPX1ZBTFVFX09OX1pFUk8nICovOw0KLyohNDAxMTEgU0VUIEBPTERfU1FMX05PVEVTPUBAU1FMX05PVEVTLCBTUUxfTk9URVM9MCAqLzsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGFjY291bnRzYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgdXNlcm5hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgcGFzc3dvcmRgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgcmVzZXRfcGFzc3dvcmRgIGludCBERUZBVUxUICcwJywNCiAgYHJvbGVgIGVudW0oJ0FkbWluaXN0cmF0b3InLCdNb2RlcmF0b3InLCdDdXN0b21lcicpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBtYWlsYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHBob25lYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGxhc3RfbG9naW5gIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgaXBfYWRkcmVzc2AgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGB0d29fZmFjdG9yX2VuYWJsZWRgIGludCBERUZBVUxUICcwJywNCiAgYHR3b19mYWN0b3JfYWNjb3VudF9zZWNyZXRfa2V5YCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHBlcm1pc3Npb25zYCBtZWRpdW10ZXh0LA0KICBgdGVuYW50c2AgbWVkaXVtdGV4dCwNCiAgYHNlc3Npb25fZ3VpZGAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPWxhdGluMTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGFnZW50X3BhY2thZ2VfY29uZmlndXJhdGlvbnNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBuYW1lYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgc3NsYCBpbnQgREVGQVVMVCAnMScsDQogIGBjb21tdW5pY2F0aW9uX3NlcnZlcnNgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgcmVtb3RlX3NlcnZlcnNgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGB1cGRhdGVfc2VydmVyc2AgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGB0cnVzdF9zZXJ2ZXJzYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGZpbGVfc2VydmVyc2AgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHRlbmFudF9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGxvY2F0aW9uX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgbGFuZ3VhZ2VgIGVudW0oJ2RlLURFJywnZW4tVVMnKSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUICdlbi1VUycsDQogIGBndWlkYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGFudGl2aXJ1c19jb250cm9sbGVkX2ZvbGRlcl9hY2Nlc3NfcnVsZXNldHNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBuYW1lYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGVzY3JpcHRpb25gIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBhdXRob3JgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgYXBwbGljYXRpb25zX2RyaXZlcnNfaGlzdG9yeWAgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGRldmljZV9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBganNvbmAgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBhcHBsaWNhdGlvbnNfaW5zdGFsbGVkX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgYXBwbGljYXRpb25zX2xvZ29uX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgYXBwbGljYXRpb25zX3NjaGVkdWxlZF90YXNrc19oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBqc29uYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGFwcGxpY2F0aW9uc19zZXJ2aWNlc19oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBqc29uYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGF1dG9tYXRpb25zYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgbmFtZWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRlc2NyaXB0aW9uYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBjYXRlZ29yeWAgaW50IERFRkFVTFQgTlVMTCwNCiAgYHN1Yl9jYXRlZ29yeWAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGNvbmRpdGlvbmAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGV4cGVjdGVkX3Jlc3VsdGAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHRyaWdnZXJgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBqc29uYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGRldmljZXNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBhZ2VudF92ZXJzaW9uYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCAnMC4wLjAuMCcsDQogIGB0ZW5hbnRfbmFtZWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHRlbmFudF9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGxvY2F0aW9uX25hbWVgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUICctJywNCiAgYGxvY2F0aW9uX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZ3JvdXBfbmFtZWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgJy0nLA0KICBgZ3JvdXBfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkZXZpY2VfbmFtZWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGFjY2Vzc19rZXlgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBod2lkYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgYmxhY2tsaXN0ZWRgIGludCBERUZBVUxUICcwJywNCiAgYGF1dGhvcml6ZWRgIGludCBERUZBVUxUICcwJywNCiAgYGxhc3RfYWNjZXNzYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYHN5bmNlZGAgaW50IERFRkFVTFQgJzAnLA0KICBgaXBfYWRkcmVzc19pbnRlcm5hbGAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGlwX2FkZHJlc3NfZXh0ZXJuYWxgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBvcGVyYXRpbmdfc3lzdGVtYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZG9tYWluYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgYW50aXZpcnVzX3NvbHV0aW9uYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZmlyZXdhbGxfc3RhdHVzYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgYXJjaGl0ZWN0dXJlYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgbGFzdF9ib290YCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgdGltZXpvbmVgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBjcHVgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBjcHVfdXNhZ2VgIGludCBERUZBVUxUICcwJywNCiAgYGNwdV9pbmZvcm1hdGlvbmAgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYG1haW5ib2FyZGAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGdwdWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHJhbWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHJhbV91c2FnZWAgaW50IERFRkFVTFQgJzAnLA0KICBgcmFtX2luZm9ybWF0aW9uYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgdHBtYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZW52aXJvbm1lbnRfdmFyaWFibGVzYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgbmV0d29ya19hZGFwdGVyc2AgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGRpc2tzYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgYXBwbGljYXRpb25zX2luc3RhbGxlZGAgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGFwcGxpY2F0aW9uc19sb2dvbmAgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGFwcGxpY2F0aW9uc19zY2hlZHVsZWRfdGFza3NgIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBhcHBsaWNhdGlvbnNfc2VydmljZXNgIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBhcHBsaWNhdGlvbnNfZHJpdmVyc2AgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYHByb2Nlc3Nlc2AgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYG5vdGVzYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgYW50aXZpcnVzX3Byb2R1Y3RzYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgYW50aXZpcnVzX2luZm9ybWF0aW9uYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGRldmljZV9pbmZvcm1hdGlvbl9hbnRpdmlydXNfcHJvZHVjdHNfaGlzdG9yeWAgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGRldmljZV9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBganNvbmAgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBkZXZpY2VfaW5mb3JtYXRpb25fY3B1X2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZGV2aWNlX2luZm9ybWF0aW9uX2Rpc2tzX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZGV2aWNlX2luZm9ybWF0aW9uX2dlbmVyYWxfaGlzdG9yeWAgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGRldmljZV9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgcG9saWN5X25hbWVgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBpcF9hZGRyZXNzX2ludGVybmFsYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgaXBfYWRkcmVzc19leHRlcm5hbGAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYG5ldHdvcmtfYWRhcHRlcnNgIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBqc29uYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGRldmljZV9pbmZvcm1hdGlvbl9oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBqc29uYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGRldmljZV9pbmZvcm1hdGlvbl9uZXR3b3JrX2FkYXB0ZXJzX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZGV2aWNlX2luZm9ybWF0aW9uX25vdGVzX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYG5vdGVgIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZGV2aWNlX2luZm9ybWF0aW9uX3JhbV9oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBqc29uYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGRldmljZV9pbmZvcm1hdGlvbl9yZW1vdGVfc2hlbGxfaGlzdG9yeWAgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGRldmljZV9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgY29tbWFuZGAgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYHJlc3VsdGAgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBkZXZpY2VfaW5mb3JtYXRpb25fdGFza19tYW5hZ2VyX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZXZlbnRzYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgdGVuYW50X25hbWVfc25hcHNob3RgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBsb2NhdGlvbl9uYW1lX3NuYXBzaG90YCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGV2aWNlX25hbWVgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUIE5VTEwsDQogIGBzZXZlcml0eWAgaW50IERFRkFVTFQgTlVMTCwNCiAgYHJlcG9ydGVkX2J5YCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgX2V2ZW50YCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgZGVzY3JpcHRpb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBub3RpZmljYXRpb25fanNvbmAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHJlYWRgIGludCBERUZBVUxUICcwJywNCiAgYHR5cGVgIGludCBERUZBVUxUIE5VTEwsDQogIGBsYW5ndWFnZWAgaW50IERFRkFVTFQgTlVMTCwNCiAgYG1haWxfc3RhdHVzYCBpbnQgREVGQVVMVCAnMCcsDQogIGBtc190ZWFtc19zdGF0dXNgIGludCBERUZBVUxUICcwJywNCiAgYHRlbGVncmFtX3N0YXR1c2AgaW50IERFRkFVTFQgJzAnLA0KICBgbnRmeV9zaF9zdGF0dXNgIGludCBERUZBVUxUICcwJywNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBmaWxlc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGRldmljZV9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYG5hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0XzA5MDBfYWlfY2kgREVGQVVMVCBOVUxMLA0KICBgcGF0aGAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfMDkwMF9haV9jaSBERUZBVUxUIE5VTEwsDQogIGBzaGE1MTJgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0XzA5MDBfYWlfY2kgREVGQVVMVCBOVUxMLA0KICBgZ3VpZGAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfMDkwMF9haV9jaSBERUZBVUxUIE5VTEwsDQogIGBwYXNzd29yZGAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYGFjY2Vzc2AgZW51bSgnUHJpdmF0ZScsJ1B1YmxpYycpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfMDkwMF9haV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0XzA5MDBfYWlfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBncm91cHNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGB0ZW5hbnRfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBsb2NhdGlvbl9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYG5hbWVgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRlc2NyaXB0aW9uYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGluZnJhc3RydWN0dXJlX2V2ZW50c2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYHRlbmFudF9uYW1lYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGV2aWNlX25hbWVgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUIE5VTEwsDQogIGByZXBvcnRlZF9ieWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGV2ZW50YCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgZGVzY3JpcHRpb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGByZWFkYCBpbnQgREVGQVVMVCAnMCcsDQogIGBsb2dfaWRgIHZhcmNoYXIoMTApIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHR5cGVgIGludCBERUZBVUxUICcwJywNCiAgYGxhbmdgIGludCBERUZBVUxUICcwJywNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBqb2JzYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgbmFtZWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRlc2NyaXB0aW9uYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBwbGF0Zm9ybWAgZW51bSgnV2luZG93cycsJ1N5c3RlbScpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHR5cGVgIGVudW0oJ1Bvd2VyU2hlbGwnLCdNeVNRTCcpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHNjcmlwdF9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGpzb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgbG9jYXRpb25zYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgdGVuYW50X2lkYCBpbnQgTk9UIE5VTEwgREVGQVVMVCAnMCcsDQogIGBndWlkYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgbmFtZWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGVzY3JpcHRpb25gIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgbWFpbF9ub3RpZmljYXRpb25zYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgbWFpbF9hZGRyZXNzYCBtZWRpdW10ZXh0LA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBhdXRob3JgIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBkZXNjcmlwdGlvbmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgbGF0aW4xIENPTExBVEUgbGF0aW4xX3N3ZWRpc2hfY2kgREVGQVVMVCBOVUxMLA0KICBgc2V2ZXJpdHlgIGludCBERUZBVUxUICcwJywNCiAgYHRlbmFudHNgIG1lZGl1bXRleHQsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD1sYXRpbjE7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBtaWNyb3NvZnRfdGVhbXNfbm90aWZpY2F0aW9uc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGNvbm5lY3Rvcl9uYW1lYCBtZWRpdW10ZXh0LA0KICBgY29ubmVjdG9yX3VybGAgbWVkaXVtdGV4dCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgZGVzY3JpcHRpb25gIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBzZXZlcml0eWAgaW50IERFRkFVTFQgJzAnLA0KICBgdGVuYW50c2AgbWVkaXVtdGV4dCwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPWxhdGluMTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYG50Znlfc2hfbm90aWZpY2F0aW9uc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYHRvcGljX25hbWVgIG1lZGl1bXRleHQsDQogIGB0b3BpY191cmxgIG1lZGl1bXRleHQsDQogIGBhY2Nlc3NfdG9rZW5gIG1lZGl1bXRleHQsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYGRlc2NyaXB0aW9uYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgc2V2ZXJpdHlgIGludCBERUZBVUxUICcwJywNCiAgYHRlbmFudHNgIG1lZGl1bXRleHQsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD1sYXRpbjE7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBwZXJmb3JtYW5jZV9tb25pdG9yaW5nX3Jlc3NvdXJjZXNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGB0ZW5hbnRfbmFtZWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGxvY2F0aW9uX25hbWVgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkZXZpY2VfbmFtZWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgTlVMTCwNCiAgYHR5cGVgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBwZXJmb3JtYW5jZV9kYXRhYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYHBvbGljaWVzYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgbmFtZWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGVzY3JpcHRpb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBhbnRpdmlydXNfc2V0dGluZ3NgIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBhbnRpdmlydXNfZXhjbHVzaW9uc2AgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGFudGl2aXJ1c19zY2FuX2pvYnNgIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBhbnRpdmlydXNfY29udHJvbGxlZF9mb2xkZXJfYWNjZXNzX2ZvbGRlcnNgIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBzZW5zb3JzYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgam9ic2AgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYG9wZXJhdGluZ19zeXN0ZW1gIGVudW0oJ1dpbmRvd3MnLCdMaW51eCcsJ21hY09TJykgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYHNjcmlwdHNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBuYW1lYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGVzY3JpcHRpb25gIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBhdXRob3JgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYHBsYXRmb3JtYCBlbnVtKCdXaW5kb3dzJywnU3lzdGVtJykgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgc2hlbGxgIGVudW0oJ1Bvd2VyU2hlbGwnLCdNeVNRTCcpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHNjcmlwdGAgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGpzb25gIG1lZGl1bXRleHQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgc2Vuc29yc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYG5hbWVgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkZXNjcmlwdGlvbmAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgY2F0ZWdvcnlgIGludCBERUZBVUxUIE5VTEwsDQogIGBzdWJfY2F0ZWdvcnlgIGludCBERUZBVUxUIE5VTEwsDQogIGBzZXZlcml0eWAgaW50IERFRkFVTFQgTlVMTCwNCiAgYHNjcmlwdF9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYHNjcmlwdF9hY3Rpb25faWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBqc29uYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYHNlcnZlcnNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBuYW1lYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgaXBfYWRkcmVzc2AgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYGRvbWFpbmAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYG9zYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgaGVhcnRoYmVhdGAgZGF0ZXRpbWUgREVGQVVMVCBOVUxMLA0KICBgYXBwc2V0dGluZ3NgIG1lZGl1bXRleHQsDQogIGBjcHVfdXNhZ2VgIGludCBERUZBVUxUIE5VTEwsDQogIGByYW1fdXNhZ2VgIGludCBERUZBVUxUIE5VTEwsDQogIGBkaXNrX3VzYWdlYCBpbnQgREVGQVVMVCBOVUxMLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfMDkwMF9haV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYHNldHRpbmdzYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGJfdmVyc2lvbmAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGZpbGVzX2FwaV9rZXlgIHZhcmNoYXIoMjU1KSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBzbXRwYCBtZWRpdW10ZXh0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgcGFja2FnZV9wcm92aWRlcl91cmxgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYHN1cHBvcnRfaGlzdG9yeWAgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGRldmljZV9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYHVzZXJuYW1lYCB2YXJjaGFyKDI1NSkgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBkZXNjcmlwdGlvbmAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGB0ZWxlZ3JhbV9ub3RpZmljYXRpb25zYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgYm90X25hbWVgIG1lZGl1bXRleHQsDQogIGBib3RfdG9rZW5gIG1lZGl1bXRleHQsDQogIGBjaGF0X2lkYCBtZWRpdW10ZXh0LA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBhdXRob3JgIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBkZXNjcmlwdGlvbmAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYHNldmVyaXR5YCBpbnQgREVGQVVMVCAnMCcsDQogIGB0ZW5hbnRzYCBtZWRpdW10ZXh0LA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9bGF0aW4xOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgdGVuYW50c2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGd1aWRgIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBuYW1lYCB2YXJjaGFyKDI1NSkgREVGQVVMVCAnVTNSaGJtUmhjbVE9JywNCiAgYGRlc2NyaXB0aW9uYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBjb21wYW55YCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgY29udGFjdF9wZXJzb25fb25lYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgY29udGFjdF9wZXJzb25fdHdvYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgY29udGFjdF9wZXJzb25fdGhyZWVgIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBjb250YWN0X3BlcnNvbl9mb3VyYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgY29udGFjdF9wZXJzb25fZml2ZWAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPWxhdGluMTsNCg0KLyohNDAxMDMgU0VUIFRJTUVfWk9ORT1JRk5VTEwoQE9MRF9USU1FX1pPTkUsICdzeXN0ZW0nKSAqLzsNCi8qITQwMTAxIFNFVCBTUUxfTU9ERT1JRk5VTEwoQE9MRF9TUUxfTU9ERSwgJycpICovOw0KLyohNDAwMTQgU0VUIEZPUkVJR05fS0VZX0NIRUNLUz1JRk5VTEwoQE9MRF9GT1JFSUdOX0tFWV9DSEVDS1MsIDEpICovOw0KLyohNDAxMDEgU0VUIENIQVJBQ1RFUl9TRVRfQ0xJRU5UPUBPTERfQ0hBUkFDVEVSX1NFVF9DTElFTlQgKi87DQovKiE0MDExMSBTRVQgU1FMX05PVEVTPUlGTlVMTChAT0xEX1NRTF9OT1RFUywgMSkgKi87";
        private static string installation_script_2_5_0_0 = @"LyohNDAxMDEgU0VUIEBPTERfQ0hBUkFDVEVSX1NFVF9DTElFTlQ9QEBDSEFSQUNURVJfU0VUX0NMSUVOVCAqLzsNCi8qITQwMTAxIFNFVCBOQU1FUyB1dGY4ICovOw0KLyohNTA1MDMgU0VUIE5BTUVTIHV0ZjhtYjQgKi87DQovKiE0MDEwMyBTRVQgQE9MRF9USU1FX1pPTkU9QEBUSU1FX1pPTkUgKi87DQovKiE0MDEwMyBTRVQgVElNRV9aT05FPScrMDA6MDAnICovOw0KLyohNDAwMTQgU0VUIEBPTERfRk9SRUlHTl9LRVlfQ0hFQ0tTPUBARk9SRUlHTl9LRVlfQ0hFQ0tTLCBGT1JFSUdOX0tFWV9DSEVDS1M9MCAqLzsNCi8qITQwMTAxIFNFVCBAT0xEX1NRTF9NT0RFPUBAU1FMX01PREUsIFNRTF9NT0RFPSdOT19BVVRPX1ZBTFVFX09OX1pFUk8nICovOw0KLyohNDAxMTEgU0VUIEBPTERfU1FMX05PVEVTPUBAU1FMX05PVEVTLCBTUUxfTk9URVM9MCAqLzsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGFjY291bnRzYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgdXNlcm5hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgcGFzc3dvcmRgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgcmVzZXRfcGFzc3dvcmRgIGludCBERUZBVUxUICcwJywNCiAgYHJvbGVgIGVudW0oJ0FkbWluaXN0cmF0b3InLCdNb2RlcmF0b3InLCdDdXN0b21lcicpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBtYWlsYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHBob25lYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGxhc3RfbG9naW5gIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgaXBfYWRkcmVzc2AgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGB0d29fZmFjdG9yX2VuYWJsZWRgIGludCBERUZBVUxUICcwJywNCiAgYHR3b19mYWN0b3JfYWNjb3VudF9zZWNyZXRfa2V5YCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHBlcm1pc3Npb25zYCBtZWRpdW10ZXh0LA0KICBgdGVuYW50c2AgbWVkaXVtdGV4dCwNCiAgYHNlc3Npb25fZ3VpZGAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgQVVUT19JTkNSRU1FTlQ9MiBERUZBVUxUIENIQVJTRVQ9bGF0aW4xOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgYWdlbnRfcGFja2FnZV9jb25maWd1cmF0aW9uc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYG5hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgc3NsYCBpbnQgREVGQVVMVCAnMScsDQogIGBjb21tdW5pY2F0aW9uX3NlcnZlcnNgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgcmVtb3RlX3NlcnZlcnNgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgdXBkYXRlX3NlcnZlcnNgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgdHJ1c3Rfc2VydmVyc2AgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBmaWxlX3NlcnZlcnNgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgdGVuYW50X2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgbG9jYXRpb25faWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBsYW5ndWFnZWAgZW51bSgnZGUtREUnLCdlbi1VUycpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUICdlbi1VUycsDQogIGBndWlkYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgQVVUT19JTkNSRU1FTlQ9NCBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGFudGl2aXJ1c19jb250cm9sbGVkX2ZvbGRlcl9hY2Nlc3NfcnVsZXNldHNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBuYW1lYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRlc2NyaXB0aW9uYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGFwcGxpY2F0aW9uc19kcml2ZXJzX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBBVVRPX0lOQ1JFTUVOVD0xMTMxIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgYXBwbGljYXRpb25zX2luc3RhbGxlZF9oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBqc29uYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgQVVUT19JTkNSRU1FTlQ9MTEzMSBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGFwcGxpY2F0aW9uc19sb2dvbl9oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBqc29uYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgQVVUT19JTkNSRU1FTlQ9MTEzMSBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGFwcGxpY2F0aW9uc19zY2hlZHVsZWRfdGFza3NfaGlzdG9yeWAgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGRldmljZV9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBganNvbmAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIEFVVE9fSU5DUkVNRU5UPTExMzEgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBhcHBsaWNhdGlvbnNfc2VydmljZXNfaGlzdG9yeWAgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGRldmljZV9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBganNvbmAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIEFVVE9fSU5DUkVNRU5UPTExMzEgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBhdXRvbWF0aW9uc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYG5hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGVzY3JpcHRpb25gIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgY2F0ZWdvcnlgIGludCBERUZBVUxUIE5VTEwsDQogIGBzdWJfY2F0ZWdvcnlgIGludCBERUZBVUxUIE5VTEwsDQogIGBjb25kaXRpb25gIGludCBERUZBVUxUIE5VTEwsDQogIGBleHBlY3RlZF9yZXN1bHRgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgdHJpZ2dlcmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBqc29uYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgQVVUT19JTkNSRU1FTlQ9NCBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGRldmljZXNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBhZ2VudF92ZXJzaW9uYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgJzAuMC4wLjAnLA0KICBgdGVuYW50X25hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgdGVuYW50X2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgbG9jYXRpb25fbmFtZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUICctJywNCiAgYGxvY2F0aW9uX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZ3JvdXBfbmFtZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUICctJywNCiAgYGdyb3VwX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGV2aWNlX25hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgYWNjZXNzX2tleWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBod2lkYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHBsYXRmb3JtYCBlbnVtKCdXaW5kb3dzJywnTGludXgnLCdNYWNPUycpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGJsYWNrbGlzdGVkYCBpbnQgREVGQVVMVCAnMCcsDQogIGBhdXRob3JpemVkYCBpbnQgREVGQVVMVCAnMCcsDQogIGBsYXN0X2FjY2Vzc2AgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBzeW5jZWRgIGludCBERUZBVUxUICcwJywNCiAgYGlwX2FkZHJlc3NfaW50ZXJuYWxgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgaXBfYWRkcmVzc19leHRlcm5hbGAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBvcGVyYXRpbmdfc3lzdGVtYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRvbWFpbmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBhbnRpdmlydXNfc29sdXRpb25gIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZmlyZXdhbGxfc3RhdHVzYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGFyY2hpdGVjdHVyZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBsYXN0X2Jvb3RgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgdGltZXpvbmVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgY3B1YCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGNwdV91c2FnZWAgaW50IERFRkFVTFQgJzAnLA0KICBgY3B1X2luZm9ybWF0aW9uYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYG1haW5ib2FyZGAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBncHVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgcmFtYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHJhbV91c2FnZWAgaW50IERFRkFVTFQgJzAnLA0KICBgcmFtX2luZm9ybWF0aW9uYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYHRwbWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBlbnZpcm9ubWVudF92YXJpYWJsZXNgIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgbmV0d29ya19hZGFwdGVyc2AgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBkaXNrc2AgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBhcHBsaWNhdGlvbnNfaW5zdGFsbGVkYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGFwcGxpY2F0aW9uc19sb2dvbmAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBhcHBsaWNhdGlvbnNfc2NoZWR1bGVkX3Rhc2tzYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGFwcGxpY2F0aW9uc19zZXJ2aWNlc2AgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBhcHBsaWNhdGlvbnNfZHJpdmVyc2AgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBwcm9jZXNzZXNgIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgbm90ZXNgIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgYW50aXZpcnVzX3Byb2R1Y3RzYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGFudGl2aXJ1c19pbmZvcm1hdGlvbmAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBjcm9uam9ic2AgbWVkaXVtdGV4dCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgQVVUT19JTkNSRU1FTlQ9MjYgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBkZXZpY2VfaW5mb3JtYXRpb25fYW50aXZpcnVzX3Byb2R1Y3RzX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBBVVRPX0lOQ1JFTUVOVD0xMTMxIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZGV2aWNlX2luZm9ybWF0aW9uX2NwdV9oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBqc29uYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgQVVUT19JTkNSRU1FTlQ9MTEzMSBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGRldmljZV9pbmZvcm1hdGlvbl9jcm9uam9ic19oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBqc29uYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgQVVUT19JTkNSRU1FTlQ9MTEyOCBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGRldmljZV9pbmZvcm1hdGlvbl9kaXNrc19oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBqc29uYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgQVVUT19JTkNSRU1FTlQ9MTEzMSBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGRldmljZV9pbmZvcm1hdGlvbl9nZW5lcmFsX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYHBvbGljeV9uYW1lYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGlwX2FkZHJlc3NfaW50ZXJuYWxgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgaXBfYWRkcmVzc19leHRlcm5hbGAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBuZXR3b3JrX2FkYXB0ZXJzYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGpzb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBBVVRPX0lOQ1JFTUVOVD0xMTMxIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZGV2aWNlX2luZm9ybWF0aW9uX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGRldmljZV9pbmZvcm1hdGlvbl9uZXR3b3JrX2FkYXB0ZXJzX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBBVVRPX0lOQ1JFTUVOVD0xMTMxIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZGV2aWNlX2luZm9ybWF0aW9uX25vdGVzX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBub3RlYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBkZXZpY2VfaW5mb3JtYXRpb25fcmFtX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBBVVRPX0lOQ1JFTUVOVD0xMTMxIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZGV2aWNlX2luZm9ybWF0aW9uX3JlbW90ZV9zaGVsbF9oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBhdXRob3JgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgY29tbWFuZGAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGByZXN1bHRgIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBBVVRPX0lOQ1JFTUVOVD0xMjggREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBkZXZpY2VfaW5mb3JtYXRpb25fdGFza19tYW5hZ2VyX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBBVVRPX0lOQ1JFTUVOVD0xMTMxIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZXZlbnRzYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgdGVuYW50X25hbWVfc25hcHNob3RgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgbG9jYXRpb25fbmFtZV9zbmFwc2hvdGAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkZXZpY2VfbmFtZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUIE5VTEwsDQogIGBzZXZlcml0eWAgaW50IERFRkFVTFQgTlVMTCwNCiAgYHJlcG9ydGVkX2J5YCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYF9ldmVudGAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBkZXNjcmlwdGlvbmAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBub3RpZmljYXRpb25fanNvbmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGByZWFkYCBpbnQgREVGQVVMVCAnMCcsDQogIGB0eXBlYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgbGFuZ3VhZ2VgIGludCBERUZBVUxUIE5VTEwsDQogIGBtYWlsX3N0YXR1c2AgaW50IERFRkFVTFQgJzAnLA0KICBgbXNfdGVhbXNfc3RhdHVzYCBpbnQgREVGQVVMVCAnMCcsDQogIGB0ZWxlZ3JhbV9zdGF0dXNgIGludCBERUZBVUxUICcwJywNCiAgYG50Znlfc2hfc3RhdHVzYCBpbnQgREVGQVVMVCAnMCcsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZmlsZXNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBuYW1lYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF8wOTAwX2FpX2NpIERFRkFVTFQgTlVMTCwNCiAgYHBhdGhgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0XzA5MDBfYWlfY2kgREVGQVVMVCBOVUxMLA0KICBgc2hhNTEyYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF8wOTAwX2FpX2NpIERFRkFVTFQgTlVMTCwNCiAgYGd1aWRgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0XzA5MDBfYWlfY2kgREVGQVVMVCBOVUxMLA0KICBgcGFzc3dvcmRgIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBhY2Nlc3NgIGVudW0oJ1ByaXZhdGUnLCdQdWJsaWMnKSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0XzA5MDBfYWlfY2kgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIEFVVE9fSU5DUkVNRU5UPTEzMSBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfMDkwMF9haV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYGdyb3Vwc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYHRlbmFudF9pZGAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGxvY2F0aW9uX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgbmFtZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkZXNjcmlwdGlvbmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIEFVVE9fSU5DUkVNRU5UPTIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBpbmZyYXN0cnVjdHVyZV9ldmVudHNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGB0ZW5hbnRfbmFtZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkZXZpY2VfbmFtZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUIE5VTEwsDQogIGByZXBvcnRlZF9ieWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBldmVudGAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBkZXNjcmlwdGlvbmAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGByZWFkYCBpbnQgREVGQVVMVCAnMCcsDQogIGBsb2dfaWRgIHZhcmNoYXIoMTApIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGB0eXBlYCBpbnQgREVGQVVMVCAnMCcsDQogIGBsYW5nYCBpbnQgREVGQVVMVCAnMCcsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgam9ic2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYG5hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGVzY3JpcHRpb25gIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgcGxhdGZvcm1gIGVudW0oJ1dpbmRvd3MnLCdMaW51eCcsJ01hY09TJywnU3lzdGVtJykgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYHR5cGVgIGVudW0oJ1Bvd2VyU2hlbGwnLCdCYXNoJywnTXlTUUwnLCdac2gnKSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgc2NyaXB0X2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBganNvbmAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIEFVVE9fSU5DUkVNRU5UPTYgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBsb2NhdGlvbnNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGB0ZW5hbnRfaWRgIGludCBOT1QgTlVMTCBERUZBVUxUICcwJywNCiAgYGd1aWRgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgbmFtZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkZXNjcmlwdGlvbmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIEFVVE9fSU5DUkVNRU5UPTMgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBtYWlsX25vdGlmaWNhdGlvbnNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBtYWlsX2FkZHJlc3NgIG1lZGl1bXRleHQsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYGRlc2NyaXB0aW9uYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCBsYXRpbjEgQ09MTEFURSBsYXRpbjFfc3dlZGlzaF9jaSBERUZBVUxUIE5VTEwsDQogIGBzZXZlcml0eWAgaW50IERFRkFVTFQgJzAnLA0KICBgdGVuYW50c2AgbWVkaXVtdGV4dCwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPWxhdGluMTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYG1pY3Jvc29mdF90ZWFtc19ub3RpZmljYXRpb25zYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgY29ubmVjdG9yX25hbWVgIG1lZGl1bXRleHQsDQogIGBjb25uZWN0b3JfdXJsYCBtZWRpdW10ZXh0LA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBhdXRob3JgIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBkZXNjcmlwdGlvbmAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYHNldmVyaXR5YCBpbnQgREVGQVVMVCAnMCcsDQogIGB0ZW5hbnRzYCBtZWRpdW10ZXh0LA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBERUZBVUxUIENIQVJTRVQ9bGF0aW4xOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgbnRmeV9zaF9ub3RpZmljYXRpb25zYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgdG9waWNfbmFtZWAgbWVkaXVtdGV4dCwNCiAgYHRvcGljX3VybGAgbWVkaXVtdGV4dCwNCiAgYGFjY2Vzc190b2tlbmAgbWVkaXVtdGV4dCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgZGVzY3JpcHRpb25gIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBzZXZlcml0eWAgaW50IERFRkFVTFQgJzAnLA0KICBgdGVuYW50c2AgbWVkaXVtdGV4dCwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPWxhdGluMTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYHBlcmZvcm1hbmNlX21vbml0b3JpbmdfcmVzc291cmNlc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYHRlbmFudF9uYW1lYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGxvY2F0aW9uX25hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGV2aWNlX25hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCBOVUxMLA0KICBgdHlwZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBwZXJmb3JtYW5jZV9kYXRhYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBwb2xpY2llc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYG5hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBhdXRob3JgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGVzY3JpcHRpb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgYW50aXZpcnVzX3NldHRpbmdzYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGFudGl2aXJ1c19leGNsdXNpb25zYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGFudGl2aXJ1c19zY2FuX2pvYnNgIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgYW50aXZpcnVzX2NvbnRyb2xsZWRfZm9sZGVyX2FjY2Vzc19mb2xkZXJzYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYHNlbnNvcnNgIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBgam9ic2AgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIEFVVE9fSU5DUkVNRU5UPTggREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0X3VuaWNvZGVfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBzY3JpcHRzYCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgbmFtZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkZXNjcmlwdGlvbmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBhdXRob3JgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBwbGF0Zm9ybWAgZW51bSgnV2luZG93cycsJ0xpbnV4JywnTWFjT1MnLCdTeXN0ZW0nKSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgc2hlbGxgIGVudW0oJ1Bvd2VyU2hlbGwnLCdCYXNoJywnTXlTUUwnLCdac2gnKSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgc2NyaXB0YCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYGpzb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBBVVRPX0lOQ1JFTUVOVD0xMSBERUZBVUxUIENIQVJTRVQ9dXRmOG1iNCBDT0xMQVRFPXV0ZjhtYjRfdW5pY29kZV9jaTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYHNlbnNvcnNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBuYW1lYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGRlc2NyaXB0aW9uYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGNhdGVnb3J5YCBpbnQgREVGQVVMVCBOVUxMLA0KICBgc3ViX2NhdGVnb3J5YCBpbnQgREVGQVVMVCBOVUxMLA0KICBgc2V2ZXJpdHlgIGludCBERUZBVUxUIE5VTEwsDQogIGBzY3JpcHRfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBzY3JpcHRfYWN0aW9uX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBganNvbmAgbWVkaXVtdGV4dCBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2ksDQogIGBwbGF0Zm9ybWAgZW51bSgnV2luZG93cycsJ0xpbnV4JywnTWFjT1MnKSBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIEFVVE9fSU5DUkVNRU5UPTEzIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgc2VydmVyc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYG5hbWVgIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBpcF9hZGRyZXNzYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgZG9tYWluYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgb3NgIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBoZWFydGhiZWF0YCBkYXRldGltZSBERUZBVUxUIE5VTEwsDQogIGBhcHBzZXR0aW5nc2AgbWVkaXVtdGV4dCwNCiAgYGNwdV91c2FnZWAgaW50IERFRkFVTFQgTlVMTCwNCiAgYHJhbV91c2FnZWAgaW50IERFRkFVTFQgTlVMTCwNCiAgYGRpc2tfdXNhZ2VgIGludCBERUZBVUxUIE5VTEwsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIEFVVE9fSU5DUkVNRU5UPTIgREVGQVVMVCBDSEFSU0VUPXV0ZjhtYjQgQ09MTEFURT11dGY4bWI0XzA5MDBfYWlfY2k7DQoNCkNSRUFURSBUQUJMRSBJRiBOT1QgRVhJU1RTIGBzZXR0aW5nc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGRiX3ZlcnNpb25gIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZmlsZXNfYXBpX2tleWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBzbXRwYCBtZWRpdW10ZXh0IENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSwNCiAgYHBhY2thZ2VfcHJvdmlkZXJfdXJsYCB2YXJjaGFyKDI1NSkgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYGFnZW50X3VwZGF0ZXNfZW5hYmxlZGAgaW50IERFRkFVTFQgJzAnLA0KICBgbWVtYmVyc19wb3J0YWxfYXBpX2tleWAgdmFyY2hhcigyNTUpIENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpIERFRkFVTFQgTlVMTCwNCiAgYG1lbWJlcnNfcG9ydGFsX2xpY2Vuc2VfbmFtZWAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIGBtZW1iZXJzX3BvcnRhbF9saWNlbnNlX3N0YXR1c2AgZW51bSgnQWN0aXZlJywnRXhwaXJlZCcpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUICdFeHBpcmVkJywNCiAgYG1lbWJlcnNfcG9ydGFsX2xpY2Vuc2VzX3VzZWRgIGludCBERUZBVUxUICcwJywNCiAgYG1lbWJlcnNfcG9ydGFsX2xpY2Vuc2VzX21heGAgaW50IERFRkFVTFQgJzAnLA0KICBgbWVtYmVyc19wb3J0YWxfbGljZW5zZXNfaGFyZF9saW1pdGAgaW50IERFRkFVTFQgJzAnLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBBVVRPX0lOQ1JFTUVOVD0yIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgc3VwcG9ydF9oaXN0b3J5YCAoDQogIGBpZGAgaW50IE5PVCBOVUxMIEFVVE9fSU5DUkVNRU5ULA0KICBgZGV2aWNlX2lkYCBpbnQgREVGQVVMVCBOVUxMLA0KICBgdXNlcm5hbWVgIHZhcmNoYXIoMjU1KSBDSEFSQUNURVIgU0VUIHV0ZjhtYjQgQ09MTEFURSB1dGY4bWI0X3VuaWNvZGVfY2kgREVGQVVMVCBOVUxMLA0KICBgZGF0ZWAgZGF0ZXRpbWUgREVGQVVMVCAnMjAwMC0wMS0wMSAwMDowMDowMCcsDQogIGBkZXNjcmlwdGlvbmAgdmFyY2hhcigyNTUpIENIQVJBQ1RFUiBTRVQgdXRmOG1iNCBDT0xMQVRFIHV0ZjhtYjRfdW5pY29kZV9jaSBERUZBVUxUIE5VTEwsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgdGVsZWdyYW1fbm90aWZpY2F0aW9uc2AgKA0KICBgaWRgIGludCBOT1QgTlVMTCBBVVRPX0lOQ1JFTUVOVCwNCiAgYGJvdF9uYW1lYCBtZWRpdW10ZXh0LA0KICBgYm90X3Rva2VuYCBtZWRpdW10ZXh0LA0KICBgY2hhdF9pZGAgbWVkaXVtdGV4dCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgYXV0aG9yYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgZGVzY3JpcHRpb25gIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIGBzZXZlcml0eWAgaW50IERFRkFVTFQgJzAnLA0KICBgdGVuYW50c2AgbWVkaXVtdGV4dCwNCiAgUFJJTUFSWSBLRVkgKGBpZGApDQopIEVOR0lORT1Jbm5vREIgREVGQVVMVCBDSEFSU0VUPWxhdGluMTsNCg0KQ1JFQVRFIFRBQkxFIElGIE5PVCBFWElTVFMgYHRlbmFudHNgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBndWlkYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgbmFtZWAgdmFyY2hhcigyNTUpIERFRkFVTFQgJ1UzUmhibVJoY21RPScsDQogIGBkZXNjcmlwdGlvbmAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYGF1dGhvcmAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYGRhdGVgIGRhdGV0aW1lIERFRkFVTFQgJzIwMDAtMDEtMDEgMDA6MDA6MDAnLA0KICBgY29tcGFueWAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYGNvbnRhY3RfcGVyc29uX29uZWAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYGNvbnRhY3RfcGVyc29uX3R3b2AgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYGNvbnRhY3RfcGVyc29uX3RocmVlYCB2YXJjaGFyKDI1NSkgREVGQVVMVCBOVUxMLA0KICBgY29udGFjdF9wZXJzb25fZm91cmAgdmFyY2hhcigyNTUpIERFRkFVTFQgTlVMTCwNCiAgYGNvbnRhY3RfcGVyc29uX2ZpdmVgIHZhcmNoYXIoMjU1KSBERUZBVUxUIE5VTEwsDQogIFBSSU1BUlkgS0VZIChgaWRgKQ0KKSBFTkdJTkU9SW5ub0RCIEFVVE9fSU5DUkVNRU5UPTMgREVGQVVMVCBDSEFSU0VUPWxhdGluMTsNCg0KLyohNDAxMDMgU0VUIFRJTUVfWk9ORT1JRk5VTEwoQE9MRF9USU1FX1pPTkUsICdzeXN0ZW0nKSAqLzsNCi8qITQwMTAxIFNFVCBTUUxfTU9ERT1JRk5VTEwoQE9MRF9TUUxfTU9ERSwgJycpICovOw0KLyohNDAwMTQgU0VUIEZPUkVJR05fS0VZX0NIRUNLUz1JRk5VTEwoQE9MRF9GT1JFSUdOX0tFWV9DSEVDS1MsIDEpICovOw0KLyohNDAxMDEgU0VUIENIQVJBQ1RFUl9TRVRfQ0xJRU5UPUBPTERfQ0hBUkFDVEVSX1NFVF9DTElFTlQgKi87DQovKiE0MDExMSBTRVQgU1FMX05PVEVTPUlGTlVMTChAT0xEX1NRTF9OT1RFUywgMSkgKi87DQo=";
        private static string installation_script_defaults = @"LS0gLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0NCi0tIEhvc3Q6ICAgICAgICAgICAgICAgICAgICAgICAgIDEyNy4wLjAuMQ0KLS0gU2VydmVyLVZlcnNpb246ICAgICAgICAgICAgICAgOC4wLjQyIC0gTXlTUUwgQ29tbXVuaXR5IFNlcnZlciAtIEdQTA0KLS0gU2VydmVyLUJldHJpZWJzc3lzdGVtOiAgICAgICAgTGludXgNCi0tIEhlaWRpU1FMIFZlcnNpb246ICAgICAgICAgICAgIDEyLjEwLjAuNzAwMA0KLS0gLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0tLS0NCg0KLyohNDAxMDEgU0VUIEBPTERfQ0hBUkFDVEVSX1NFVF9DTElFTlQ9QEBDSEFSQUNURVJfU0VUX0NMSUVOVCAqLzsNCi8qITQwMTAxIFNFVCBOQU1FUyB1dGY4ICovOw0KLyohNTA1MDMgU0VUIE5BTUVTIHV0ZjhtYjQgKi87DQovKiE0MDEwMyBTRVQgQE9MRF9USU1FX1pPTkU9QEBUSU1FX1pPTkUgKi87DQovKiE0MDEwMyBTRVQgVElNRV9aT05FPScrMDA6MDAnICovOw0KLyohNDAwMTQgU0VUIEBPTERfRk9SRUlHTl9LRVlfQ0hFQ0tTPUBARk9SRUlHTl9LRVlfQ0hFQ0tTLCBGT1JFSUdOX0tFWV9DSEVDS1M9MCAqLzsNCi8qITQwMTAxIFNFVCBAT0xEX1NRTF9NT0RFPUBAU1FMX01PREUsIFNRTF9NT0RFPSdOT19BVVRPX1ZBTFVFX09OX1pFUk8nICovOw0KLyohNDAxMTEgU0VUIEBPTERfU1FMX05PVEVTPUBAU1FMX05PVEVTLCBTUUxfTk9URVM9MCAqLzsNCg0KLS0gRXhwb3J0aWVyZSBEYXRlbiBhdXMgVGFiZWxsZSBuZXRsb2NrZGVtby5hdXRvbWF0aW9uczogfjEgcm93cyAodW5nZWbDpGhyKQ0KSU5TRVJUIElOVE8gYGF1dG9tYXRpb25zYCAoYGlkYCwgYG5hbWVgLCBgZGVzY3JpcHRpb25gLCBgYXV0aG9yYCwgYGRhdGVgLCBgY2F0ZWdvcnlgLCBgc3ViX2NhdGVnb3J5YCwgYGNvbmRpdGlvbmAsIGBleHBlY3RlZF9yZXN1bHRgLCBgdHJpZ2dlcmAsIGBqc29uYCkgVkFMVUVTDQoJKDQsICdEZWZhdWx0JywgJycsICdhZG1pbicsICcyMDI1LTA2LTMwIDEzOjUxOjQ1JywgMCwgMCwgMSwgJ0RlZmF1bHQnLCAnRGVmYXVsdCcsICd7XHJcbiAgIm5hbWUiOiAiRGVmYXVsdCIsXHJcbiAgImRhdGUiOiAiMjAyNS0wNi0zMCAxMzo1MTo0NCIsXHJcbiAgImF1dGhvciI6ICJhZG1pbiIsXHJcbiAgImRlc2NyaXB0aW9uIjogIiIsXHJcbiAgImNhdGVnb3J5IjogMCxcclxuICAic3ViX2NhdGVnb3J5IjogMCxcclxuICAiY29uZGl0aW9uIjogMSxcclxuICAiZXhwZWN0ZWRfcmVzdWx0IjogIkRlZmF1bHQiLFxyXG4gICJ0cmlnZ2VyIjogIkRlZmF1bHQiXHJcbn0nKTsNCg0KLS0gRXhwb3J0aWVyZSBEYXRlbiBhdXMgVGFiZWxsZSBuZXRsb2NrZGVtby5ncm91cHM6IH4yIHJvd3MgKHVuZ2Vmw6RocikNCklOU0VSVCBJTlRPIGBncm91cHNgIChgaWRgLCBgdGVuYW50X2lkYCwgYGxvY2F0aW9uX2lkYCwgYG5hbWVgLCBgZGF0ZWAsIGBhdXRob3JgLCBgZGVzY3JpcHRpb25gKSBWQUxVRVMNCgkoMiwgMywgMywgJ1dvcmtzdGF0aW9ucycsICcyMDI1LTA2LTMwIDEzOjQ3OjAzJywgJ2FkbWluJywgJycpLA0KCSgzLCAzLCAzLCAnU2VydmVyJywgJzIwMjUtMDYtMzAgMTM6NDc6MDgnLCAnYWRtaW4nLCAnJyk7DQoNCi0tIEV4cG9ydGllcmUgRGF0ZW4gYXVzIFRhYmVsbGUgbmV0bG9ja2RlbW8ubG9jYXRpb25zOiB+MSByb3dzICh1bmdlZsOkaHIpDQpJTlNFUlQgSU5UTyBgbG9jYXRpb25zYCAoYGlkYCwgYHRlbmFudF9pZGAsIGBndWlkYCwgYG5hbWVgLCBgZGF0ZWAsIGBhdXRob3JgLCBgZGVzY3JpcHRpb25gKSBWQUxVRVMNCgkoMywgMywgJ2ZhNjk4Y2E2LWMxOTEtNGEwOS1hMTZmLWQ2NzkwNjFkNzQ0OCcsICdEZWZhdWx0JywgJzIwMjUtMDYtMzAgMTM6NDY6NTEnLCAnYWRtaW4nLCAnJyk7DQoNCi0tIEV4cG9ydGllcmUgRGF0ZW4gYXVzIFRhYmVsbGUgbmV0bG9ja2RlbW8ucG9saWNpZXM6IH4xIHJvd3MgKHVuZ2Vmw6RocikNCklOU0VSVCBJTlRPIGBwb2xpY2llc2AgKGBpZGAsIGBuYW1lYCwgYGRhdGVgLCBgYXV0aG9yYCwgYGRlc2NyaXB0aW9uYCwgYGFudGl2aXJ1c19zZXR0aW5nc2AsIGBhbnRpdmlydXNfZXhjbHVzaW9uc2AsIGBhbnRpdmlydXNfc2Nhbl9qb2JzYCwgYGFudGl2aXJ1c19jb250cm9sbGVkX2ZvbGRlcl9hY2Nlc3NfZm9sZGVyc2AsIGBzZW5zb3JzYCwgYGpvYnNgKSBWQUxVRVMNCgkoOCwgJ0RlZmF1bHQnLCAnMjAyNS0wNi0zMCAxMzo1MToyNicsICdhZG1pbicsICcnLCAneyJlbmFibGVkIjpmYWxzZSwic2VjdXJpdHlfY2VudGVyIjpmYWxzZSwic2VjdXJpdHlfY2VudGVyX3RyYXkiOmZhbHNlLCJjaGVja19ob3VybHlfc2lnbmF0dXJlcyI6ZmFsc2UsImFsbG93X21ldGVyZWRfdXBkYXRlcyI6ZmFsc2UsImRlbGV0ZV9xdWFyYW50aW5lX3NpeF9tb250aHMiOmZhbHNlLCJzY2FuX2RpcmVjdGlvbiI6MCwiZmlsZV9oYXNoX2NvbXB1dGluZyI6ZmFsc2UsImJsb2NrX2F0X2ZpcnN0X3NlZW4iOmZhbHNlLCJzY2FuX2FyY2hpdmVzIjpmYWxzZSwic2Nhbl9tYWlscyI6ZmFsc2UsIm5ldF9zY2FuX25ldHdvcmtfZmlsZXMiOmZhbHNlLCJuZXRfZmlsdGVyX2luY29taW5nX2Nvbm5lY3Rpb25zIjpmYWxzZSwibmV0X2RhdGFncmFtX3Byb2Nlc3NpbmciOmZhbHNlLCJwYXJzZXJfdGxzIjpmYWxzZSwicGFyc2VyX3JkcCI6ZmFsc2UsInBhcnNlcl9zc2giOmZhbHNlLCJwYXJzZXJfaHR0cCI6ZmFsc2UsInBhcnNlcl9kbnMiOmZhbHNlLCJwYXJzZXJfZG5zb3ZlcnRjcCI6ZmFsc2UsImNvbnRyb2xsZWRfZm9sZGVyX2FjY2Vzc19lbmFibGVkIjpmYWxzZSwiY29udHJvbGxlZF9mb2xkZXJfYWNjZXNzX3J1bGVzZXQiOiItIiwiY29udHJvbGxlZF9mb2xkZXJfYWNjZXNzX2ZvbGRlcnNfd2hpdGVsaXN0X3VudGlsX2RhdGUiOiIyMDI1LTA3LTE0VDAwOjAwOjAwIiwibm90aWZpY2F0aW9uc19jb250cm9sbGVkX2ZvbGRlcl9hY3Rpb25zX2Jsb2NrZWRfYWN0aW9uIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX2FudGl2aXJ1c19kaXNhYmxlZCI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9hbnRpdmlydXNfZW5hYmxlZCI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9hbnRpc3B5d2FyZV9kaXNhYmxlZCI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9hbnRpc3B5d2FyZV9lbmFibGVkIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX2JlaGF2aW9yX2RldGVjdGVkIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX2NvbmZpZ19jaGFuZ2VkIjpmYWxzZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9kaXNhYmxlZF9leHBpcmVkX3N0YXRlIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX2VuZ2luZV9mYWlsdXJlIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX2VuZ2luZV91cGRhdGVfcGxhdGZvcm1vdXRvZmRhdGUiOnRydWUsIm5vdGlmaWNhdGlvbnNfbWFsd2FyZXByb3RlY3Rpb25fZXhwaXJhdGlvbl93YXJuaW5nX3N0YXRlIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX2ZvbGRlcl9ndWFyZF9zZWN0b3JfYmxvY2siOnRydWUsIm5vdGlmaWNhdGlvbnNfbWFsd2FyZXByb3RlY3Rpb25fbWFsd2FyZV9hY3Rpb25fZmFpbGVkXzAwX21hbHdhcmVwcm90ZWN0aW9uX3N0YXRlX21hbHdhcmVfYWN0aW9uX2ZhaWxlZF8wMF9tYWx3YXJlcHJvdGVjdGlvbl9zdGF0ZV9tYWx3YXJlX2FjdGlvbl9jcml0aWNhbGx5X2ZhaWxlZCI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9tYWx3YXJlX2FjdGlvbl90YWtlbiI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9tYWx3YXJlX2RldGVjdGVkIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX29zX2VvbCI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9vc19leHBpcmluZyI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9wbGF0Zm9ybV9hbG1vc3RvdXRvZmRhdGUiOnRydWUsIm5vdGlmaWNhdGlvbnNfbWFsd2FyZXByb3RlY3Rpb25fcGxhdGZvcm1fdXBkYXRlX2ZhaWxlZCI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9wcm90ZWN0aW9uX2VvbCI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9xdWFyYW50aW5lX2RlbGV0ZSI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9xdWFyYW50aW5lX3Jlc3RvcmUiOnRydWUsIm5vdGlmaWNhdGlvbnNfbWFsd2FyZXByb3RlY3Rpb25fcnRwX2Rpc2FibGVkIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX3J0cF9lbmFibGVkIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX3J0cF9mZWF0dXJlX2NvbmZpZ3VyZWQiOmZhbHNlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX3J0cF9mZWF0dXJlX2ZhaWx1cmUiOnRydWUsIm5vdGlmaWNhdGlvbnNfbWFsd2FyZXByb3RlY3Rpb25fcnRwX2ZlYXR1cmVfcmVjb3ZlcmVkIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX3NjYW5fY2FuY2VsbGVkIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX3NjYW5fY29tcGxldGVkIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX3NjYW5fZmFpbGVkIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX3NjYW5fcGF1c2VkIjp0cnVlLCJub3RpZmljYXRpb25zX21hbHdhcmVwcm90ZWN0aW9uX3NpZ25hdHVyZV9yZXZlcnNpb24iOnRydWUsIm5vdGlmaWNhdGlvbnNfbWFsd2FyZXByb3RlY3Rpb25fc2lnbmF0dXJlX3VwZGF0ZV9mYWlsZWQiOnRydWUsIm5vdGlmaWNhdGlvbnNfbWFsd2FyZXByb3RlY3Rpb25fc2lnbmF0dXJlX3VwZGF0ZWQiOnRydWUsIm5vdGlmaWNhdGlvbnNfbWFsd2FyZXByb3RlY3Rpb25fc3RhdGVfbWFsd2FyZV9hY3Rpb25fY3JpdGljYWxseV9mYWlsZWQiOnRydWUsIm5vdGlmaWNhdGlvbnNfbWFsd2FyZXByb3RlY3Rpb25fc3RhdGVfbWFsd2FyZV9kZXRlY3RlZCI6dHJ1ZSwibm90aWZpY2F0aW9uc19tYWx3YXJlcHJvdGVjdGlvbl9zdGF0ZV9tYWx3YXJlX2FjdGlvbl90YWtlbiI6dHJ1ZSwibm90aWZpY2F0aW9uc190YW1wZXJfcHJvdGVjdGlvbl9ibG9ja2VkX2NoYW5nZXMiOnRydWUsIm5vdGlmaWNhdGlvbnNfbmV0bG9ja19tYWlsIjp0cnVlLCJub3RpZmljYXRpb25zX25ldGxvY2tfbWljcm9zb2Z0X3RlYW1zIjp0cnVlLCJub3RpZmljYXRpb25zX25ldGxvY2tfdGVsZWdyYW0iOnRydWUsIm5vdGlmaWNhdGlvbnNfbmV0bG9ja19udGZ5X3NoIjp0cnVlfScsICdbXScsICdbXScsICdbXScsICdbXHJcbiAge1xyXG4gICAgImlkIjogIjEzIlxyXG4gIH0sXHJcbiAge1xyXG4gICAgImlkIjogIjE0IlxyXG4gIH1cclxuXScsICdbXScpOw0KDQotLSBFeHBvcnRpZXJlIERhdGVuIGF1cyBUYWJlbGxlIG5ldGxvY2tkZW1vLnNlbnNvcnM6IH4yIHJvd3MgKHVuZ2Vmw6RocikNCklOU0VSVCBJTlRPIGBzZW5zb3JzYCAoYGlkYCwgYG5hbWVgLCBgZGVzY3JpcHRpb25gLCBgYXV0aG9yYCwgYGRhdGVgLCBgY2F0ZWdvcnlgLCBgc3ViX2NhdGVnb3J5YCwgYHNldmVyaXR5YCwgYHNjcmlwdF9pZGAsIGBzY3JpcHRfYWN0aW9uX2lkYCwgYGpzb25gLCBgcGxhdGZvcm1gKSBWQUxVRVMNCgkoMTMsICdNb3JlIHRoYW4gOTAlIGRyaXZlIHNwYWNlIHVzZWQnLCAnQ2hlY2tzIGFsbCBkcml2ZXMgKHJlbW92YWJsZXMgJiBuZXR3b3JrIGRyaXZlcyBleGNsdWRlZCkuIENoZWNrcyBldmVyeSA1IG1pbnV0ZXMuIE5vIHRocmVzaG9sZC4nLCBOVUxMLCAnMjAyNS0wNi0zMCAxMzo0OToyMCcsIDAsIDIsIDEsIDAsIDAsICd7XHJcbiAgImlkIjogIlRPQUNIOSIsXHJcbiAgIm5hbWUiOiAiTW9yZSB0aGFuIDkwJSBkcml2ZSBzcGFjZSB1c2VkIixcclxuICAiZGF0ZSI6ICIyMDI1LTA2LTMwIDEzOjUwOjI5IixcclxuICAibGFzdF9ydW4iOiBudWxsLFxyXG4gICJhdXRob3IiOiAiYWRtaW4iLFxyXG4gICJkZXNjcmlwdGlvbiI6ICJDaGVja3MgYWxsIGRyaXZlcyAocmVtb3ZhYmxlcyBcXHUwMDI2IG5ldHdvcmsgZHJpdmVzIGV4Y2x1ZGVkKS4gQ2hlY2tzIGV2ZXJ5IDUgbWludXRlcy4gTm8gdGhyZXNob2xkLiIsXHJcbiAgInBsYXRmb3JtIjogIldpbmRvd3MiLFxyXG4gICJzZXZlcml0eSI6IDEsXHJcbiAgImNhdGVnb3J5IjogMCxcclxuICAic3ViX2NhdGVnb3J5IjogMixcclxuICAidXRpbGl6YXRpb25fY2F0ZWdvcnkiOiAwLFxyXG4gICJub3RpZmljYXRpb25fdHJlc2hvbGRfY291bnQiOiAwLFxyXG4gICJub3RpZmljYXRpb25fdHJlc2hvbGRfbWF4IjogMCxcclxuICAibm90aWZpY2F0aW9uX2hpc3RvcnkiOiBudWxsLFxyXG4gICJhY3Rpb25fdHJlc2hvbGRfY291bnQiOiAwLFxyXG4gICJhY3Rpb25fdHJlc2hvbGRfbWF4IjogMCxcclxuICAiYWN0aW9uX2hpc3RvcnkiOiBudWxsLFxyXG4gICJhdXRvX3Jlc2V0IjogdHJ1ZSxcclxuICAic2NyaXB0X2lkIjogMCxcclxuICAic2NyaXB0IjogIiIsXHJcbiAgInNjcmlwdF9hY3Rpb25faWQiOiAwLFxyXG4gICJzY3JpcHRfYWN0aW9uIjogIiIsXHJcbiAgImNwdV91c2FnZSI6IDkwLFxyXG4gICJwcm9jZXNzX25hbWUiOiAiIixcclxuICAicmFtX3VzYWdlIjogOTAsXHJcbiAgImRpc2tfdXNhZ2UiOiA5MCxcclxuICAiZGlza19taW5pbXVtX2NhcGFjaXR5IjogMCxcclxuICAiZGlza19jYXRlZ29yeSI6IDIsXHJcbiAgImRpc2tfbGV0dGVycyI6ICJDIixcclxuICAiZGlza19pbmNsdWRlX25ldHdvcmtfZGlza3MiOiBmYWxzZSxcclxuICAiZGlza19pbmNsdWRlX3JlbW92YWJsZV9kaXNrcyI6IGZhbHNlLFxyXG4gICJldmVudGxvZyI6ICJBcHBsaWNhdGlvbiIsXHJcbiAgImV2ZW50bG9nX2N1c3RvbSI6IGZhbHNlLFxyXG4gICJldmVudGxvZ19ldmVudF9pZCI6ICIiLFxyXG4gICJleHBlY3RlZF9yZXN1bHQiOiAiIixcclxuICAic2VydmljZV9uYW1lIjogIiIsXHJcbiAgInNlcnZpY2VfY29uZGl0aW9uIjogMixcclxuICAic2VydmljZV9hY3Rpb24iOiAwLFxyXG4gICJwaW5nX2FkZHJlc3MiOiAiIixcclxuICAicGluZ190aW1lb3V0IjogNSxcclxuICAicGluZ19jb25kaXRpb24iOiAwLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl90eXBlIjogMyxcclxuICAidGltZV9zY2hlZHVsZXJfc2Vjb25kcyI6IDEwLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl9taW51dGVzIjogNSxcclxuICAidGltZV9zY2hlZHVsZXJfaG91cnMiOiAxLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl90aW1lIjogIjEyOjAwOjAwIixcclxuICAidGltZV9zY2hlZHVsZXJfZGF0ZSI6ICI2LzMwLzIwMjUgMTo0NzoxOSBQTSIsXHJcbiAgInRpbWVfc2NoZWR1bGVyX21vbmRheSI6IGZhbHNlLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl90dWVzZGF5IjogZmFsc2UsXHJcbiAgInRpbWVfc2NoZWR1bGVyX3dlZG5lc2RheSI6IGZhbHNlLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl90aHVyc2RheSI6IGZhbHNlLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl9mcmlkYXkiOiBmYWxzZSxcclxuICAidGltZV9zY2hlZHVsZXJfc2F0dXJkYXkiOiBmYWxzZSxcclxuICAidGltZV9zY2hlZHVsZXJfc3VuZGF5IjogZmFsc2UsXHJcbiAgIm5vdGlmaWNhdGlvbnNfbWFpbCI6IHRydWUsXHJcbiAgIm5vdGlmaWNhdGlvbnNfbWljcm9zb2Z0X3RlYW1zIjogdHJ1ZSxcclxuICAibm90aWZpY2F0aW9uc190ZWxlZ3JhbSI6IHRydWUsXHJcbiAgIm5vdGlmaWNhdGlvbnNfbnRmeV9zaCI6IHRydWVcclxufScsICdXaW5kb3dzJyksDQoJKDE0LCAnTW9yZW4gdGhhbiA5MCUgUkFNIHVzZWQnLCAnQ2hlY2tzIGV2ZXJ5IDUgbWludXRlcy4gTm8gdGhyZXNob2xkLicsIE5VTEwsICcyMDI1LTA2LTMwIDEzOjQ5OjU0JywgMCwgMSwgMSwgMCwgMCwgJ3tcclxuICAiaWQiOiAiNVVOMEhXIixcclxuICAibmFtZSI6ICJNb3JlbiB0aGFuIDkwJSBSQU0gdXNlZCIsXHJcbiAgImRhdGUiOiAiMjAyNS0wNi0zMCAxMzo1MDoyNCIsXHJcbiAgImxhc3RfcnVuIjogbnVsbCxcclxuICAiYXV0aG9yIjogImFkbWluIixcclxuICAiZGVzY3JpcHRpb24iOiAiQ2hlY2tzIGV2ZXJ5IDUgbWludXRlcy4gTm8gdGhyZXNob2xkLiIsXHJcbiAgInBsYXRmb3JtIjogIldpbmRvd3MiLFxyXG4gICJzZXZlcml0eSI6IDEsXHJcbiAgImNhdGVnb3J5IjogMCxcclxuICAic3ViX2NhdGVnb3J5IjogMSxcclxuICAidXRpbGl6YXRpb25fY2F0ZWdvcnkiOiAwLFxyXG4gICJub3RpZmljYXRpb25fdHJlc2hvbGRfY291bnQiOiAwLFxyXG4gICJub3RpZmljYXRpb25fdHJlc2hvbGRfbWF4IjogMCxcclxuICAibm90aWZpY2F0aW9uX2hpc3RvcnkiOiBudWxsLFxyXG4gICJhY3Rpb25fdHJlc2hvbGRfY291bnQiOiAwLFxyXG4gICJhY3Rpb25fdHJlc2hvbGRfbWF4IjogMCxcclxuICAiYWN0aW9uX2hpc3RvcnkiOiBudWxsLFxyXG4gICJhdXRvX3Jlc2V0IjogdHJ1ZSxcclxuICAic2NyaXB0X2lkIjogMCxcclxuICAic2NyaXB0IjogIiIsXHJcbiAgInNjcmlwdF9hY3Rpb25faWQiOiAwLFxyXG4gICJzY3JpcHRfYWN0aW9uIjogIiIsXHJcbiAgImNwdV91c2FnZSI6IDkwLFxyXG4gICJwcm9jZXNzX25hbWUiOiAiIixcclxuICAicmFtX3VzYWdlIjogOTAsXHJcbiAgImRpc2tfdXNhZ2UiOiA1MCxcclxuICAiZGlza19taW5pbXVtX2NhcGFjaXR5IjogMCxcclxuICAiZGlza19jYXRlZ29yeSI6IDAsXHJcbiAgImRpc2tfbGV0dGVycyI6ICJDIixcclxuICAiZGlza19pbmNsdWRlX25ldHdvcmtfZGlza3MiOiBmYWxzZSxcclxuICAiZGlza19pbmNsdWRlX3JlbW92YWJsZV9kaXNrcyI6IGZhbHNlLFxyXG4gICJldmVudGxvZyI6ICJBcHBsaWNhdGlvbiIsXHJcbiAgImV2ZW50bG9nX2N1c3RvbSI6IGZhbHNlLFxyXG4gICJldmVudGxvZ19ldmVudF9pZCI6ICIiLFxyXG4gICJleHBlY3RlZF9yZXN1bHQiOiAiIixcclxuICAic2VydmljZV9uYW1lIjogIiIsXHJcbiAgInNlcnZpY2VfY29uZGl0aW9uIjogMixcclxuICAic2VydmljZV9hY3Rpb24iOiAwLFxyXG4gICJwaW5nX2FkZHJlc3MiOiAiIixcclxuICAicGluZ190aW1lb3V0IjogNSxcclxuICAicGluZ19jb25kaXRpb24iOiAwLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl90eXBlIjogMyxcclxuICAidGltZV9zY2hlZHVsZXJfc2Vjb25kcyI6IDEwLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl9taW51dGVzIjogNSxcclxuICAidGltZV9zY2hlZHVsZXJfaG91cnMiOiAxLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl90aW1lIjogIjEyOjAwOjAwIixcclxuICAidGltZV9zY2hlZHVsZXJfZGF0ZSI6ICI2LzMwLzIwMjUgMTo0OTozMyBQTSIsXHJcbiAgInRpbWVfc2NoZWR1bGVyX21vbmRheSI6IGZhbHNlLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl90dWVzZGF5IjogZmFsc2UsXHJcbiAgInRpbWVfc2NoZWR1bGVyX3dlZG5lc2RheSI6IGZhbHNlLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl90aHVyc2RheSI6IGZhbHNlLFxyXG4gICJ0aW1lX3NjaGVkdWxlcl9mcmlkYXkiOiBmYWxzZSxcclxuICAidGltZV9zY2hlZHVsZXJfc2F0dXJkYXkiOiBmYWxzZSxcclxuICAidGltZV9zY2hlZHVsZXJfc3VuZGF5IjogZmFsc2UsXHJcbiAgIm5vdGlmaWNhdGlvbnNfbWFpbCI6IHRydWUsXHJcbiAgIm5vdGlmaWNhdGlvbnNfbWljcm9zb2Z0X3RlYW1zIjogdHJ1ZSxcclxuICAibm90aWZpY2F0aW9uc190ZWxlZ3JhbSI6IHRydWUsXHJcbiAgIm5vdGlmaWNhdGlvbnNfbnRmeV9zaCI6IHRydWVcclxufScsICdXaW5kb3dzJyk7DQoNCi0tIEV4cG9ydGllcmUgRGF0ZW4gYXVzIFRhYmVsbGUgbmV0bG9ja2RlbW8udGVuYW50czogfjEgcm93cyAodW5nZWbDpGhyKQ0KSU5TRVJUIElOVE8gYHRlbmFudHNgIChgaWRgLCBgZ3VpZGAsIGBuYW1lYCwgYGRlc2NyaXB0aW9uYCwgYGF1dGhvcmAsIGBkYXRlYCwgYGNvbXBhbnlgLCBgY29udGFjdF9wZXJzb25fb25lYCwgYGNvbnRhY3RfcGVyc29uX3R3b2AsIGBjb250YWN0X3BlcnNvbl90aHJlZWAsIGBjb250YWN0X3BlcnNvbl9mb3VyYCwgYGNvbnRhY3RfcGVyc29uX2ZpdmVgKSBWQUxVRVMNCgkoMywgJzk2YTA4Mzg5LTRlMzQtNGMwZi05MWE0LWQ1YjIyZTAzNmE4YycsICdEZWZhdWx0JywgJycsICdhZG1pbicsICcyMDI1LTA2LTMwIDEzOjQ2OjQ0JywgJycsICcnLCAnJywgJycsICcnLCAnJyk7DQoNCi8qITQwMTAzIFNFVCBUSU1FX1pPTkU9SUZOVUxMKEBPTERfVElNRV9aT05FLCAnc3lzdGVtJykgKi87DQovKiE0MDEwMSBTRVQgU1FMX01PREU9SUZOVUxMKEBPTERfU1FMX01PREUsICcnKSAqLzsNCi8qITQwMDE0IFNFVCBGT1JFSUdOX0tFWV9DSEVDS1M9SUZOVUxMKEBPTERfRk9SRUlHTl9LRVlfQ0hFQ0tTLCAxKSAqLzsNCi8qITQwMTAxIFNFVCBDSEFSQUNURVJfU0VUX0NMSUVOVD1AT0xEX0NIQVJBQ1RFUl9TRVRfQ0xJRU5UICovOw0KLyohNDAxMTEgU0VUIFNRTF9OT1RFUz1JRk5VTEwoQE9MRF9TUUxfTk9URVMsIDEpICovOw0K";

        // Upgrade scripts
        private static string upgrade_script_2_0_0_0_to_2_5_0_0 = "QUxURVIgVEFCTEUgYGRldmljZXNgIEFERCBDT0xVTU4gYHBsYXRmb3JtYCBFTlVNKCdXaW5kb3dzJywnTGludXgnLCdNYWNPUycpIE5VTEwgREVGQVVMVCBOVUxMIEFGVEVSIGBod2lkYDsNCkFMVEVSIFRBQkxFIGBkZXZpY2VzYCBBREQgQ09MVU1OIGBjcm9uam9ic2AgTUVESVVNVEVYVCBOVUxMIERFRkFVTFQgTlVMTCBBRlRFUiBgYW50aXZpcnVzX2luZm9ybWF0aW9uYDsNCg0KLS0gQWRkIGRldmljZV9pbmZvcm1hdGlvbl9jcm9uam9ic19oaXN0b3J5IHRhYmxlDQpDUkVBVEUgVEFCTEUgSUYgTk9UIEVYSVNUUyBgZGV2aWNlX2luZm9ybWF0aW9uX2Nyb25qb2JzX2hpc3RvcnlgICgNCiAgYGlkYCBpbnQgTk9UIE5VTEwgQVVUT19JTkNSRU1FTlQsDQogIGBkZXZpY2VfaWRgIGludCBERUZBVUxUIE5VTEwsDQogIGBkYXRlYCBkYXRldGltZSBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJywNCiAgYGpzb25gIG1lZGl1bXRleHQgQ0hBUkFDVEVSIFNFVCB1dGY4bWI0IENPTExBVEUgdXRmOG1iNF91bmljb2RlX2NpLA0KICBQUklNQVJZIEtFWSAoYGlkYCkNCikgRU5HSU5FPUlubm9EQiBBVVRPX0lOQ1JFTUVOVD0xMTI4IERFRkFVTFQgQ0hBUlNFVD11dGY4bWI0IENPTExBVEU9dXRmOG1iNF91bmljb2RlX2NpOw0KDQpBTFRFUiBUQUJMRSBgam9ic2AgQ0hBTkdFIENPTFVNTiBgcGxhdGZvcm1gIGBwbGF0Zm9ybWAgRU5VTSgnV2luZG93cycsJ0xpbnV4JywnTWFjT1MnLCdTeXN0ZW0nKSBOVUxMIERFRkFVTFQgTlVMTCBDT0xMQVRFICd1dGY4bWI0X3VuaWNvZGVfY2knIEFGVEVSIGBkYXRlYDsNCkFMVEVSIFRBQkxFIGBqb2JzYCBDSEFOR0UgQ09MVU1OIGB0eXBlYCBgdHlwZWAgRU5VTSgnUG93ZXJTaGVsbCcsJ0Jhc2gnLCdNeVNRTCcsJ1pzaCcpIE5VTEwgREVGQVVMVCBOVUxMIENPTExBVEUgJ3V0ZjhtYjRfdW5pY29kZV9jaScgQUZURVIgYHBsYXRmb3JtYDsNCg0KLS0gVXBkYXRlIGpvYnMgdGFibGUNCkFMVEVSIFRBQkxFIGBqb2JzYA0KICAgIENIQU5HRSBDT0xVTU4gYHBsYXRmb3JtYCBgcGxhdGZvcm1gIEVOVU0oJ1dpbmRvd3MnLCdMaW51eCcsJ01hY09TJywnU3lzdGVtJykgTlVMTCBERUZBVUxUIE5VTEwgQ09MTEFURSAndXRmOG1iNF91bmljb2RlX2NpJyBBRlRFUiBgZGF0ZWA7DQoNCkFMVEVSIFRBQkxFIGBqb2JzYA0KICAgIENIQU5HRSBDT0xVTU4gYHR5cGVgIGB0eXBlYCBFTlVNKCdQb3dlclNoZWxsJywnQmFzaCcsJ015U1FMJykgTlVMTCBERUZBVUxUIE5VTEwgQ09MTEFURSAndXRmOG1iNF91bmljb2RlX2NpJyBBRlRFUiBgcGxhdGZvcm1gOw0KDQotLSBVcGRhdGUgc2NyaXB0cyB0YWJsZQ0KQUxURVIgVEFCTEUgYHNjcmlwdHNgDQogICAgQ0hBTkdFIENPTFVNTiBgcGxhdGZvcm1gIGBwbGF0Zm9ybWAgRU5VTSgnV2luZG93cycsJ0xpbnV4JywnTWFjT1MnLCdTeXN0ZW0nKSBOVUxMIERFRkFVTFQgTlVMTCBDT0xMQVRFICd1dGY4bWI0X3VuaWNvZGVfY2knIEFGVEVSIGBkYXRlYDsNCg0KQUxURVIgVEFCTEUgYHNjcmlwdHNgDQogICAgQ0hBTkdFIENPTFVNTiBgc2hlbGxgIGBzaGVsbGAgRU5VTSgnUG93ZXJTaGVsbCcsJ0Jhc2gnLCdNeVNRTCcpIE5VTEwgREVGQVVMVCBOVUxMIENPTExBVEUgJ3V0ZjhtYjRfdW5pY29kZV9jaScgQUZURVIgYHBsYXRmb3JtYDsNCg0KLS0gVXBkYXRlIHNlbnNvcnMgdGFibGU6IGFkZCBwbGF0Zm9ybSBjb2x1bW4gYW5kIHNldCBleGlzdGluZyBzZW5zb3JzIHRvIFdpbmRvd3MNCkFMVEVSIFRBQkxFIGBzZW5zb3JzYA0KICAgIEFERCBDT0xVTU4gYHBsYXRmb3JtYCBFTlVNKCdXaW5kb3dzJywnTGludXgnLCdNYWNPUycpIE5VTEwgREVGQVVMVCBOVUxMIEFGVEVSIGBqc29uYDsNCg0KVVBEQVRFIGBzZW5zb3JzYCBTRVQgYHBsYXRmb3JtYCA9ICdXaW5kb3dzJzsNCg0KLS0gUmVtb3ZlIG9wZXJhdGluZ19zeXN0ZW0gY29sdW1uIGZyb20gcG9saWNpZXMgdGFibGUNCkFMVEVSIFRBQkxFIGBwb2xpY2llc2ANCiAgICBEUk9QIENPTFVNTiBgb3BlcmF0aW5nX3N5c3RlbWA7DQoNCi0tIFVwZGF0ZSBzY3JpcHRzIGFuZCBqb2JzIHRhYmxlcyB0byBpbmNsdWRlICdac2gnIGluIHNoZWxsL3R5cGUgY29sdW1ucw0KQUxURVIgVEFCTEUgYHNjcmlwdHNgDQogICAgQ0hBTkdFIENPTFVNTiBgc2hlbGxgIGBzaGVsbGAgRU5VTSgnUG93ZXJTaGVsbCcsJ0Jhc2gnLCdNeVNRTCcsJ1pzaCcpIE5VTEwgREVGQVVMVCBOVUxMIENPTExBVEUgJ3V0ZjhtYjRfdW5pY29kZV9jaScgQUZURVIgYHBsYXRmb3JtYDsNCg0KQUxURVIgVEFCTEUgYGpvYnNgDQogICAgQ0hBTkdFIENPTFVNTiBgdHlwZWAgYHR5cGVgIEVOVU0oJ1Bvd2VyU2hlbGwnLCdCYXNoJywnTXlTUUwnLCdac2gnKSBOVUxMIERFRkFVTFQgTlVMTCBDT0xMQVRFICd1dGY4bWI0X3VuaWNvZGVfY2knIEFGVEVSIGBwbGF0Zm9ybWA7DQoNCi0tIFVwZGF0ZSBzZXR0aW5ncyB0YWJsZTogYWRkIGFnZW50IHVwZGF0ZXMgYW5kIGxpY2Vuc2luZyBjb2x1bW5zDQpBTFRFUiBUQUJMRSBgc2V0dGluZ3NgDQogICAgQUREIENPTFVNTiBgYWdlbnRfdXBkYXRlc19lbmFibGVkYCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiBgcGFja2FnZV9wcm92aWRlcl91cmxgOw0KDQpBTFRFUiBUQUJMRSBgc2V0dGluZ3NgDQogICAgQUREIENPTFVNTiBgbGljZW5zZXNfdXNlZGAgSU5UIE5VTEwgREVGQVVMVCAnMCcgQUZURVIgYGFnZW50X3VwZGF0ZXNfZW5hYmxlZGAsDQogICAgQUREIENPTFVNTiBgbGljZW5zZXNfbWF4YCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiBgbGljZW5zZXNfdXNlZGAsDQogICAgQUREIENPTFVNTiBgbGljZW5zZXNfaGFyZF9saW1pdGAgSU5UIE5VTEwgREVGQVVMVCAnMCcgQUZURVIgYGxpY2Vuc2VzX21heGA7DQoNCkFMVEVSIFRBQkxFIGBzZXR0aW5nc2ANCiAgICBBREQgQ09MVU1OIGBsaWNlbnNlX3N0YXR1c2AgRU5VTSgnQWN0aXZlJywnRXhwaXJlZCcpIE5VTEwgREVGQVVMVCBOVUxMIEFGVEVSIGBhZ2VudF91cGRhdGVzX2VuYWJsZWRgOw0KDQpBTFRFUiBUQUJMRSBgc2V0dGluZ3NgDQogICAgQ0hBTkdFIENPTFVNTiBgbGljZW5zZV9zdGF0dXNgIGBsaWNlbnNlX3N0YXR1c2AgRU5VTSgnQWN0aXZlJywnRXhwaXJlZCcpIE5VTEwgREVGQVVMVCAnRXhwaXJlZCcgQ09MTEFURSAndXRmOG1iNF91bmljb2RlX2NpJyBBRlRFUiBgYWdlbnRfdXBkYXRlc19lbmFibGVkYDsNCg0KLS0gQWRkIG1lbWJlcnMgcG9ydGFsIHJlbGF0ZWQgY29sdW1ucw0KQUxURVIgVEFCTEUgYHNldHRpbmdzYA0KICAgIEFERCBDT0xVTU4gYG1lbWJlcnNfcG9ydGFsX2FwaV9rZXlgIFZBUkNIQVIoMjU1KSBOVUxMIERFRkFVTFQgTlVMTCBBRlRFUiBgYWdlbnRfdXBkYXRlc19lbmFibGVkYDsNCg0KQUxURVIgVEFCTEUgYHNldHRpbmdzYA0KICAgIENIQU5HRSBDT0xVTU4gYGxpY2Vuc2Vfc3RhdHVzYCBgbWVtYmVyc19wb3J0YWxfbGljZW5zZV9zdGF0dXNgIEVOVU0oJ0FjdGl2ZScsJ0V4cGlyZWQnKSBOVUxMIERFRkFVTFQgJ0V4cGlyZWQnIENPTExBVEUgJ3V0ZjhtYjRfdW5pY29kZV9jaScgQUZURVIgYG1lbWJlcnNfcG9ydGFsX2FwaV9rZXlgLA0KICAgIENIQU5HRSBDT0xVTU4gYGxpY2Vuc2VzX3VzZWRgIGBtZW1iZXJzX3BvcnRhbF9saWNlbnNlc191c2VkYCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiBgbWVtYmVyc19wb3J0YWxfbGljZW5zZV9zdGF0dXNgLA0KICAgIENIQU5HRSBDT0xVTU4gYGxpY2Vuc2VzX21heGAgYG1lbWJlcnNfcG9ydGFsX2xpY2Vuc2VzX21heGAgSU5UIE5VTEwgREVGQVVMVCAnMCcgQUZURVIgYG1lbWJlcnNfcG9ydGFsX2xpY2Vuc2VzX3VzZWRgLA0KICAgIENIQU5HRSBDT0xVTU4gYGxpY2Vuc2VzX2hhcmRfbGltaXRgIGBtZW1iZXJzX3BvcnRhbF9saWNlbnNlc19oYXJkX2xpbWl0YCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiBgbWVtYmVyc19wb3J0YWxfbGljZW5zZXNfbWF4YDsNCg0KQUxURVIgVEFCTEUgYHNldHRpbmdzYA0KICAgIEFERCBDT0xVTU4gYG1lbWJlcnNfcG9ydGFsX25hbWVgIFZBUkNIQVIoMjU1KSBOVUxMIERFRkFVTFQgTlVMTCBBRlRFUiBgbWVtYmVyc19wb3J0YWxfYXBpX2tleWA7DQoNCkFMVEVSIFRBQkxFIGBzZXR0aW5nc2ANCiAgICBDSEFOR0UgQ09MVU1OIGBtZW1iZXJzX3BvcnRhbF9uYW1lYCBgbWVtYmVyc19wb3J0YWxfbGljZW5zZV9uYW1lYCBWQVJDSEFSKDI1NSkgTlVMTCBERUZBVUxUIE5VTEwgQ09MTEFURSAndXRmOG1iNF91bmljb2RlX2NpJyBBRlRFUiBgbWVtYmVyc19wb3J0YWxfYXBpX2tleWA7DQoNClVQREFURSBgc2Vuc29yc2AgU0VUIGBwbGF0Zm9ybWA9J1dpbmRvd3MnOw0KDQpVUERBVEUgYHNldHRpbmdzYCBTRVQgYGRiX3ZlcnNpb25gPScyLjUuMC4wJzs=";
        private static string upgrade_script_2_5_0_0_to_2_5_0_2 = "QUxURVIgVEFCTEUgYHNlcnZlcnNgIEFERCBDT0xVTU4gYGRvY2tlcmAgSU5UIE5VTEwgREVGQVVMVCAnMCcgQUZURVIgYGRpc2tfdXNhZ2VgOw==";
        private static string upgrade_script_2_5_0_2_to_2_5_0_7 = "QUxURVIgVEFCTEUgYHNldHRpbmdzYCBEUk9QIENPTFVNTiBgbWVtYmVyc19wb3J0YWxfbGljZW5zZV9uYW1lYCwgRFJPUCBDT0xVTU4gYG1lbWJlcnNfcG9ydGFsX2xpY2Vuc2Vfc3RhdHVzYCwgRFJPUCBDT0xVTU4gYG1lbWJlcnNfcG9ydGFsX2xpY2Vuc2VzX3VzZWRgLCBEUk9QIENPTFVNTiBgbWVtYmVyc19wb3J0YWxfbGljZW5zZXNfbWF4YCwgRFJPUCBDT0xVTU4gYG1lbWJlcnNfcG9ydGFsX2xpY2Vuc2VzX2hhcmRfbGltaXRgOw==";
        private static string upgrade_script_2_5_0_7_to_2_5_0_9 = "QUxURVIgVEFCTEUgYGRldmljZXNgIEFERCBDT0xVTU4gYHVwdGltZV9tb25pdG9yaW5nX2VuYWJsZWRgIElOVCBOVUxMIERFRkFVTFQgJzAnIEFGVEVSIGBjcm9uam9ic2A7DQpBTFRFUiBUQUJMRSBgbWFpbF9ub3RpZmljYXRpb25zYCBBREQgQ09MVU1OIGB1cHRpbWVfbW9uaXRvcmluZ19lbmFibGVkYCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiBgdGVuYW50c2A7DQpBTFRFUiBUQUJMRSBgbWljcm9zb2Z0X3RlYW1zX25vdGlmaWNhdGlvbnNgIEFERCBDT0xVTU4gYHVwdGltZV9tb25pdG9yaW5nX2VuYWJsZWRgIElOVCBOVUxMIERFRkFVTFQgJzAnIEFGVEVSIGB0ZW5hbnRzYDsNCkFMVEVSIFRBQkxFIGBudGZ5X3NoX25vdGlmaWNhdGlvbnNgIEFERCBDT0xVTU4gYHVwdGltZV9tb25pdG9yaW5nX2VuYWJsZWRgIElOVCBOVUxMIERFRkFVTFQgJzAnIEFGVEVSIGB0ZW5hbnRzYDsNCkFMVEVSIFRBQkxFIGB0ZWxlZ3JhbV9ub3RpZmljYXRpb25zYCBBREQgQ09MVU1OIGB1cHRpbWVfbW9uaXRvcmluZ19lbmFibGVkYCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiBgdGVuYW50c2A7";
        private static string upgrade_script_2_5_1_0_to_2_5_1_1 = "QUxURVIgVEFCTEUgYHNldHRpbmdzYCBBREQgQ09MVU1OIGBjbGVhbnVwX2FwcGxpY2F0aW9uc19kcml2ZXJzX2hpc3RvcnlfZW5hYmxlZGAgSU5UIE5VTEwgREVGQVVMVCAnMScgQUZURVIgYG1lbWJlcnNfcG9ydGFsX2FwaV9rZXlgLCBBREQgQ09MVU1OIGBjbGVhbnVwX2FwcGxpY2F0aW9uc19kcml2ZXJzX2hpc3RvcnlfZGF5c2AgSU5UIE5VTEwgREVGQVVMVCAnMTUnIEFGVEVSIGBjbGVhbnVwX2FwcGxpY2F0aW9uc19kcml2ZXJzX2hpc3RvcnlfZW5hYmxlZGAsIEFERCBDT0xVTU4gYGNsZWFudXBfYXBwbGljYXRpb25zX2luc3RhbGxlZF9oaXN0b3J5X2VuYWJsZWRgIElOVCBOVUxMIERFRkFVTFQgJzEnIEFGVEVSIGBjbGVhbnVwX2FwcGxpY2F0aW9uc19kcml2ZXJzX2hpc3RvcnlfZGF5c2AsIEFERCBDT0xVTU4gYGNsZWFudXBfYXBwbGljYXRpb25zX2luc3RhbGxlZF9oaXN0b3J5X2RheXNgIElOVCBOVUxMIERFRkFVTFQgJzE1JyBBRlRFUiBgY2xlYW51cF9hcHBsaWNhdGlvbnNfaW5zdGFsbGVkX2hpc3RvcnlfZW5hYmxlZGAsIEFERCBDT0xVTU4gYGNsZWFudXBfYXBwbGljYXRpb25zX2xvZ29uX2hpc3RvcnlfZW5hYmxlZGAgSU5UIE5VTEwgREVGQVVMVCAnMScgQUZURVIgYGNsZWFudXBfYXBwbGljYXRpb25zX2luc3RhbGxlZF9oaXN0b3J5X2RheXNgLCBBREQgQ09MVU1OIGBjbGVhbnVwX2FwcGxpY2F0aW9uc19sb2dvbl9oaXN0b3J5X2RheXNgIElOVCBOVUxMIERFRkFVTFQgJzE1JyBBRlRFUiBgY2xlYW51cF9hcHBsaWNhdGlvbnNfbG9nb25faGlzdG9yeV9lbmFibGVkYCwgQUREIENPTFVNTiBgY2xlYW51cF9hcHBsaWNhdGlvbnNfc2NoZWR1bGVkX3Rhc2tzX2hpc3RvcnlfZW5hYmxlZGAgSU5UIE5VTEwgREVGQVVMVCAnMScgQUZURVIgYGNsZWFudXBfYXBwbGljYXRpb25zX2xvZ29uX2hpc3RvcnlfZGF5c2AsIEFERCBDT0xVTU4gYGNsZWFudXBfYXBwbGljYXRpb25zX3NjaGVkdWxlZF90YXNrc19oaXN0b3J5X2RheXNgIElOVCBOVUxMIERFRkFVTFQgJzE1JyBBRlRFUiBgY2xlYW51cF9hcHBsaWNhdGlvbnNfc2NoZWR1bGVkX3Rhc2tzX2hpc3RvcnlfZW5hYmxlZGAsIEFERCBDT0xVTU4gYGNsZWFudXBfYXBwbGljYXRpb25zX3NlcnZpY2VzX2hpc3RvcnlfZW5hYmxlZGAgSU5UIE5VTEwgREVGQVVMVCAnMScgQUZURVIgYGNsZWFudXBfYXBwbGljYXRpb25zX3NjaGVkdWxlZF90YXNrc19oaXN0b3J5X2RheXNgLCBBREQgQ09MVU1OIGBjbGVhbnVwX2FwcGxpY2F0aW9uc19zZXJ2aWNlc19oaXN0b3J5X2RheXNgIElOVCBOVUxMIERFRkFVTFQgJzE1JyBBRlRFUiBgY2xlYW51cF9hcHBsaWNhdGlvbnNfc2VydmljZXNfaGlzdG9yeV9lbmFibGVkYCwgQUREIENPTFVNTiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fYW50aXZpcnVzX3Byb2R1Y3RzX2hpc3RvcnlfZW5hYmxlZGAgSU5UIE5VTEwgREVGQVVMVCAnMScgQUZURVIgYGNsZWFudXBfYXBwbGljYXRpb25zX3NlcnZpY2VzX2hpc3RvcnlfZGF5c2AsIEFERCBDT0xVTU4gYGNsZWFudXBfZGV2aWNlX2luZm9ybWF0aW9uX2FudGl2aXJ1c19wcm9kdWN0c19oaXN0b3J5X2RheXNgIElOVCBOVUxMIERFRkFVTFQgJzE1JyBBRlRFUiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fYW50aXZpcnVzX3Byb2R1Y3RzX2hpc3RvcnlfZW5hYmxlZGAsIEFERCBDT0xVTU4gYGNsZWFudXBfZGV2aWNlX2luZm9ybWF0aW9uX2NwdV9oaXN0b3J5X2VuYWJsZWRgIElOVCBOVUxMIERFRkFVTFQgJzEnIEFGVEVSIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9hbnRpdmlydXNfcHJvZHVjdHNfaGlzdG9yeV9kYXlzYCwgQUREIENPTFVNTiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fY3B1X2hpc3RvcnlfZGF5c2AgSU5UIE5VTEwgREVGQVVMVCAnMTUnIEFGVEVSIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9jcHVfaGlzdG9yeV9lbmFibGVkYCwgQUREIENPTFVNTiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fY3JvbmpvYnNfaGlzdG9yeV9lbmFibGVkYCBJTlQgTlVMTCBERUZBVUxUICcxJyBBRlRFUiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fY3B1X2hpc3RvcnlfZGF5c2AsIEFERCBDT0xVTU4gYGNsZWFudXBfZGV2aWNlX2luZm9ybWF0aW9uX2Nyb25qb2JzX2hpc3RvcnlfZGF5c2AgSU5UIE5VTEwgREVGQVVMVCAnMTUnIEFGVEVSIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9jcm9uam9ic19oaXN0b3J5X2VuYWJsZWRgLCBBREQgQ09MVU1OIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9kaXNrc19oaXN0b3J5X2VuYWJsZWRgIElOVCBOVUxMIERFRkFVTFQgJzEnIEFGVEVSIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9jcm9uam9ic19oaXN0b3J5X2RheXNgLCBBREQgQ09MVU1OIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9kaXNrc19oaXN0b3J5X2RheXNgIElOVCBOVUxMIERFRkFVTFQgJzE1JyBBRlRFUiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fZGlza3NfaGlzdG9yeV9lbmFibGVkYCwgQUREIENPTFVNTiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fZ2VuZXJhbF9oaXN0b3J5X2VuYWJsZWRgIElOVCBOVUxMIERFRkFVTFQgJzEnIEFGVEVSIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9kaXNrc19oaXN0b3J5X2RheXNgLCBBREQgQ09MVU1OIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9nZW5lcmFsX2hpc3RvcnlfZGF5c2AgSU5UIE5VTEwgREVGQVVMVCAnMTUnIEFGVEVSIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9nZW5lcmFsX2hpc3RvcnlfZW5hYmxlZGAsIEFERCBDT0xVTU4gYGNsZWFudXBfZGV2aWNlX2luZm9ybWF0aW9uX2hpc3RvcnlfZW5hYmxlZGAgSU5UIE5VTEwgREVGQVVMVCAnMScgQUZURVIgYGNsZWFudXBfZGV2aWNlX2luZm9ybWF0aW9uX2dlbmVyYWxfaGlzdG9yeV9kYXlzYCwgQUREIENPTFVNTiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25faGlzdG9yeV9kYXlzYCBJTlQgTlVMTCBERUZBVUxUICcxNScgQUZURVIgYGNsZWFudXBfZGV2aWNlX2luZm9ybWF0aW9uX2hpc3RvcnlfZW5hYmxlZGAsIEFERCBDT0xVTU4gYGNsZWFudXBfZGV2aWNlX2luZm9ybWF0aW9uX25ldHdvcmtfYWRhcHRlcnNfaGlzdG9yeV9lbmFibGVkYCBJTlQgTlVMTCBERUZBVUxUICcxJyBBRlRFUiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25faGlzdG9yeV9kYXlzYCwgQUREIENPTFVNTiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fbmV0d29ya19hZGFwdGVyc19oaXN0b3J5X2RheXNgIElOVCBOVUxMIERFRkFVTFQgJzE1JyBBRlRFUiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fbmV0d29ya19hZGFwdGVyc19oaXN0b3J5X2VuYWJsZWRgLCBBREQgQ09MVU1OIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9yYW1faGlzdG9yeV9lbmFibGVkYCBJTlQgTlVMTCBERUZBVUxUICcxJyBBRlRFUiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fbmV0d29ya19hZGFwdGVyc19oaXN0b3J5X2RheXNgLCBBREQgQ09MVU1OIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl9yYW1faGlzdG9yeV9kYXlzYCBJTlQgTlVMTCBERUZBVUxUICcxNScgQUZURVIgYGNsZWFudXBfZGV2aWNlX2luZm9ybWF0aW9uX3JhbV9oaXN0b3J5X2VuYWJsZWRgLCBBREQgQ09MVU1OIGBjbGVhbnVwX2RldmljZV9pbmZvcm1hdGlvbl90YXNrX21hbmFnZXJfaGlzdG9yeV9lbmFibGVkYCBJTlQgTlVMTCBERUZBVUxUICcxJyBBRlRFUiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fcmFtX2hpc3RvcnlfZGF5c2AsIEFERCBDT0xVTU4gYGNsZWFudXBfZGV2aWNlX2luZm9ybWF0aW9uX3Rhc2tfbWFuYWdlcl9oaXN0b3J5X2RheXNgIElOVCBOVUxMIERFRkFVTFQgJzE1JyBBRlRFUiBgY2xlYW51cF9kZXZpY2VfaW5mb3JtYXRpb25fdGFza19tYW5hZ2VyX2hpc3RvcnlfZW5hYmxlZGA7DQpBTFRFUiBUQUJMRSBgc2V0dGluZ3NgIEFERCBDT0xVTU4gYGNsZWFudXBfZXZlbnRzX2hpc3RvcnlfZW5hYmxlZGAgSU5UIE5VTEwgREVGQVVMVCAnMScgQUZURVIgYGNsZWFudXBfZGV2aWNlX2luZm9ybWF0aW9uX3Rhc2tfbWFuYWdlcl9oaXN0b3J5X2RheXNgLCBBREQgQ09MVU1OIGBjbGVhbnVwX2V2ZW50c19oaXN0b3J5X2RheXNgIElOVCBOVUxMIERFRkFVTFQgJzE1JyBBRlRFUiBgY2xlYW51cF9ldmVudHNfaGlzdG9yeV9lbmFibGVkYDsNCkFMVEVSIFRBQkxFIGBzZXR0aW5nc2AgQ0hBTkdFIENPTFVNTiBgY2xlYW51cF9ldmVudHNfaGlzdG9yeV9kYXlzYCBgY2xlYW51cF9ldmVudHNfaGlzdG9yeV9kYXlzYCBJTlQgTlVMTCBERUZBVUxUICczMCcgQUZURVIgYGNsZWFudXBfZXZlbnRzX2hpc3RvcnlfZW5hYmxlZGA7DQpBTFRFUiBUQUJMRSBgc2V0dGluZ3NgIENIQU5HRSBDT0xVTU4gYGNsZWFudXBfZXZlbnRzX2hpc3RvcnlfZGF5c2AgYGNsZWFudXBfZXZlbnRzX2hpc3RvcnlfZGF5c2AgSU5UIE5VTEwgREVGQVVMVCAnMTgwJyBBRlRFUiBgY2xlYW51cF9ldmVudHNfaGlzdG9yeV9lbmFibGVkYDs=";
        private static string upgrade_script_2_5_1_1_to_2_5_1_2 = "QUxURVIgVEFCTEUgYGFjY291bnRzYCBEUk9QIENPTFVNTiBgc2Vzc2lvbl9ndWlkYDsNCkFMVEVSIFRBQkxFIGBhY2NvdW50c2AgQUREIENPTFVNTiBgcmVtb3RlX3Nlc3Npb25fdG9rZW5gIE1FRElVTVRFWFQgTlVMTCBERUZBVUxUIE5VTEwgQUZURVIgYHRlbmFudHNgOw==";
        private static string upgrade_script_2_5_1_2_to_2_5_1_3 = "QUxURVIgVEFCTEUgYHNldHRpbmdzYCBBREQgQ09MVU1OIGBzZXNzaW9uX3JlY29yZGluZ19mb3JjZV9lbmFibGVkYCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiBgY2xlYW51cF9ldmVudHNfaGlzdG9yeV9kYXlzYDsNCkFMVEVSIFRBQkxFIGBzZXR0aW5nc2AgQ0hBTkdFIENPTFVNTiBgc2Vzc2lvbl9yZWNvcmRpbmdfZm9yY2VfZW5hYmxlZGAgYHNlc3Npb25fcmVjb3JkaW5nX2ZvcmNlZF9lbmFibGVkYCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiBgY2xlYW51cF9ldmVudHNfaGlzdG9yeV9kYXlzYDsNCkFMVEVSIFRBQkxFIGBzZXR0aW5nc2AgQ0hBTkdFIENPTFVNTiBgc2Vzc2lvbl9yZWNvcmRpbmdfZm9yY2VkX2VuYWJsZWRgIGByZW1vdGVfc2NyZWVuX3Nlc3Npb25fcmVjb3JkaW5nX2ZvcmNlZF9lbmFibGVkYCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiBgY2xlYW51cF9ldmVudHNfaGlzdG9yeV9kYXlzYDsNCkFMVEVSIFRBQkxFIGBzZXR0aW5nc2AgQUREIENPTFVNTiBgcmVtb3RlX3NjcmVlbl9zZXNzaW9uX3JlY29yZGluZ19mb3JjZWRfZGF5c2AgSU5UIE5VTEwgREVGQVVMVCAnMTUnIEFGVEVSIGByZW1vdGVfc2NyZWVuX3Nlc3Npb25fcmVjb3JkaW5nX2ZvcmNlZF9lbmFibGVkYDsNCkFMVEVSIFRBQkxFIGBzZXR0aW5nc2AgQUREIENPTFVNTiBgcmVtb3RlX3NjcmVlbl9zZXNzaW9uX3JlY29yZGluZ19hdXRvX2NsZWFuX2VuYWJsZWRgIElOVCBOVUxMIERFRkFVTFQgJzEnIEFGVEVSIGByZW1vdGVfc2NyZWVuX3Nlc3Npb25fcmVjb3JkaW5nX2ZvcmNlZF9lbmFibGVkYDs=";
        private static string upgrade_script_2_5_1_3_to_2_5_1_6 = "QUxURVIgVEFCTEUgYGRldmljZXNgIEFERCBDT0xVTU4gYGxhc3RfYWN0aXZlX3VzZXJgIFZBUkNIQVIoMjU1KSBOVUxMIERFRkFVTFQgTlVMTCBBRlRFUiBgdXB0aW1lX21vbml0b3JpbmdfZW5hYmxlZGA7";
        private static string upgrade_script_2_5_1_6_to_2_5_2_2 = "QUxURVIgVEFCTEUgZGV2aWNlcyBBREQgQ09MVU1OIHVwZGF0ZV9wZW5kaW5nIElOVCBOVUxMIERFRkFVTFQgJzAnIEFGVEVSIGxhc3RfYWN0aXZlX3VzZXI7DQpBTFRFUiBUQUJMRSBkZXZpY2VzIEFERCBDT0xVTU4gdXBkYXRlX3N0YXJ0ZWQgREFURVRJTUUgTlVMTCBERUZBVUxUICcyMDAwLTAxLTAxIDAwOjAwOjAwJyBBRlRFUiB1cGRhdGVfcGVuZGluZzs=";
        private static string upgrade_script_2_5_1_6_to_2_5_2_2v2 = "QUxURVIgVEFCTEUgc2V0dGluZ3MgQ0hBTkdFIENPTFVNTiBhZ2VudF91cGRhdGVzX2VuYWJsZWQgYWdlbnRfdXBkYXRlc193aW5kb3dzX2VuYWJsZWQgSU5UIE5VTEwgREVGQVVMVCAnMCcgQUZURVIgcGFja2FnZV9wcm92aWRlcl91cmwsIEFERCBDT0xVTU4gYWdlbnRfdXBkYXRlc19saW51eF9lbmFibGVkIElOVCBOVUxMIERFRkFVTFQgJzAnIEFGVEVSIGFnZW50X3VwZGF0ZXNfd2luZG93c19lbmFibGVkLCBBREQgQ09MVU1OIGFnZW50X3VwZGF0ZXNfbWFjb3NfZW5hYmxlZCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiBhZ2VudF91cGRhdGVzX2xpbnV4X2VuYWJsZWQ7DQpBTFRFUiBUQUJMRSBzZXR0aW5ncyBBREQgQ09MVU1OIGFnZW50X3VwZGF0ZXNfbWF4X2NvbmN1cnJlbnRfdXBkYXRlcyBJTlQgTlVMTCBERUZBVUxUICc1JyBBRlRFUiBhZ2VudF91cGRhdGVzX21hY29zX2VuYWJsZWQ7";
        private static string upgrade_script_2_5_1_6_to_2_5_2_2c = "QUxURVIgVEFCTEUgYWNjb3VudHMgQUREIENPTFVNTiBjaGFuZ2Vsb2dfcmVhZCBJTlQgTlVMTCBERUZBVUxUICcwJyBBRlRFUiByZW1vdGVfc2Vzc2lvbl90b2tlbjs=";


        // Execute installation SQL script
        public static async Task<bool> Execute_Installation_Script()
        {
            // Read old settings:
            // smtp
            string smtp = String.Empty; 
            smtp = await MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "smtp");

            // files api key
            string files_api_key = String.Empty;
                files_api_key = await MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "files_api_key");

            // Generate random files api key if empty
            if (String.IsNullOrEmpty(files_api_key))
                files_api_key = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();

            try
            {
                // Installation scripts
                await Classes.MySQL.Handler.Execute_Command(await Base64.Handler.Decode(installation_script_2_5_0_0));
                await Classes.MySQL.Handler.Execute_Command(await Base64.Handler.Decode(installation_script_defaults));

                Logging.Handler.Debug("Execute_Installation_Script", "Result", "Installation script executed.");
                Console.WriteLine("Installation script executed.");

                // Delete old settings
                await Classes.MySQL.Handler.Execute_Command("DELETE FROM settings;");

                // Add new settings
                await Classes.MySQL.Handler.Execute_Command("INSERT INTO settings (db_version, files_api_key, smtp) VALUES ('" + Application_Settings.db_version + "', '" + files_api_key + "', '" + smtp + "');");

                // Add admin user
                string password = BCrypt.Net.BCrypt.HashPassword("admin");
                string permissions = @"{
  ""dashboard_enabled"": true,
  ""devices_authorized_enabled"": true,
  ""devices_general"": true,
  ""devices_software"": true,
  ""devices_task_manager"": true,
  ""devices_antivirus"": true,
  ""devices_events"": true,
  ""devices_remote_shell"": true,
  ""devices_remote_file_browser"": true,
  ""devices_remote_control"": true,
  ""devices_deauthorize"": true,
  ""devices_move"": true,
  ""devices_unauthorized_enabled"": true,
  ""devices_unauthorized_authorize"": true,
  ""tenants_enabled"": true,
  ""tenants_add"": true,
  ""tenants_manage"": true,
  ""tenants_edit"": true,
  ""tenants_delete"": true,
  ""tenants_locations_add"": true,
  ""tenants_locations_manage"": true,
  ""tenants_locations_edit"": true,
  ""tenants_locations_delete"": true,
  ""tenants_groups_add"": true,
  ""tenants_groups_edit"": true,
  ""tenants_groups_delete"": true,
  ""automation_enabled"": true,
  ""automation_add"": true,
  ""automation_edit"": true,
  ""automation_delete"": true,
  ""policies_enabled"": true,
  ""policies_add"": true,
  ""policies_manage"": true,
  ""policies_edit"": true,
  ""policies_delete"": true,
  ""collections_enabled"": true,
  ""collections_antivirus_controlled_folder_access_enabled"": true,
  ""collections_antivirus_controlled_folder_access_add"": true,
  ""collections_antivirus_controlled_folder_access_manage"": true,
  ""collections_antivirus_controlled_folder_access_edit"": true,
  ""collections_antivirus_controlled_folder_access_delete"": true,
  ""collections_antivirus_controlled_folder_access_processes_add"": true,
  ""collections_antivirus_controlled_folder_access_processes_edit"": true,
  ""collections_antivirus_controlled_folder_access_processes_delete"": true,
  ""collections_sensors_enabled"": true,
  ""collections_sensors_add"": true,
  ""collections_sensors_edit"": true,
  ""collections_sensors_delete"": true,
  ""collections_scripts_enabled"": true,
  ""collections_scripts_add"": true,
  ""collections_scripts_edit"": true,
  ""collections_scripts_delete"": true,
  ""collections_jobs_enabled"": true,
  ""collections_jobs_add"": true,
  ""collections_jobs_edit"": true,
  ""collections_jobs_delete"": true,
  ""collections_files_enabled"": true,
  ""collections_files_add"": true,
  ""collections_files_edit"": true,
  ""collections_files_delete"": true,
  ""collections_files_netlock"": true,
  ""events_enabled"": true,
  ""users_enabled"": true,
  ""users_add"": true,
  ""users_manage"": true,
  ""users_edit"": true,
  ""users_delete"": true,
  ""settings_enabled"": true,
  ""settings_notifications_enabled"": true,
  ""settings_notifications_mail_enabled"": true,
  ""settings_notifications_mail_add"": true,
  ""settings_notifications_mail_smtp"": true,
  ""settings_notifications_mail_test"": true,
  ""settings_notifications_mail_edit"": true,
  ""settings_notifications_mail_delete"": true,
  ""settings_notifications_microsoft_teams_enabled"": true,
  ""settings_notifications_microsoft_teams_add"": true,
  ""settings_notifications_microsoft_teams_test"": true,
  ""settings_notifications_microsoft_teams_edit"": true,
  ""settings_notifications_microsoft_teams_delete"": true,
  ""settings_notifications_telegram_enabled"": true,
  ""settings_notifications_telegram_add"": true,
  ""settings_notifications_telegram_test"": true,
  ""settings_notifications_telegram_edit"": true,
  ""settings_notifications_telegram_delete"": true,
  ""settings_notifications_ntfysh_enabled"": true,
  ""settings_notifications_ntfysh_add"": true,
  ""settings_notifications_ntfysh_test"": true,
  ""settings_notifications_ntfysh_edit"": true,
  ""settings_notifications_ntfysh_delete"": true,
  ""settings_system_enabled"": true,
  ""settings_protocols_enabled"": true
}";

                await Classes.MySQL.Handler.Execute_Command("INSERT INTO accounts (username, password, reset_password, role, permissions, tenants) VALUES ('admin', '" + password + "', 1, 'Administrator', '" + permissions + "', '[\r\n  {\r\n    \"id\": \"3\",\r\n    \"guid\": \"96a08389-4e34-4c0f-91a4-d5b22e036a8c\"\r\n  }\r\n]');");

                Logging.Handler.Debug("Execute_Installation_Script", "Result", "Admin user added successfully.");
                Console.WriteLine("Admin user added successfully.");
                Console.WriteLine("Username: admin");
                Console.WriteLine("Password: admin");

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Execute_Installation_Script", "Result", ex.ToString());
                return false;
            }
        }

        // Execute upgrade script
        public static async Task Execute_Update_Scripts()
        {
            List<string> scripts = new List<string>();

            // Get DB version
            string db_version = await MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "db_version");

            scripts.Add(upgrade_script_2_0_0_0_to_2_5_0_0);
            scripts.Add(upgrade_script_2_5_0_0_to_2_5_0_2);
            scripts.Add(upgrade_script_2_5_0_2_to_2_5_0_7);
            scripts.Add(upgrade_script_2_5_0_7_to_2_5_0_9);
            scripts.Add(upgrade_script_2_5_1_0_to_2_5_1_1);
            scripts.Add(upgrade_script_2_5_1_1_to_2_5_1_2);
            scripts.Add(upgrade_script_2_5_1_2_to_2_5_1_3);
            scripts.Add(upgrade_script_2_5_1_3_to_2_5_1_6);
            scripts.Add(upgrade_script_2_5_1_6_to_2_5_2_2);
            scripts.Add(upgrade_script_2_5_1_6_to_2_5_2_2v2);
            scripts.Add(upgrade_script_2_5_1_6_to_2_5_2_2c);

            // Disabled due to testing...

            /*if (db_version == "2.0.0.0")
            {
                scripts.Add(upgrade_script_2_0_0_0_to_2_5_0_0);
                scripts.Add(upgrade_script_2_5_0_0_to_2_5_0_2);
            }

            if (db_version == "2.5.0.0")
                scripts.Add(upgrade_script_2_5_0_0_to_2_5_0_2);
            */

            // Execute script
            foreach (var script in scripts)
            {
                await MySQL.Handler.Execute_Command(await Base64.Handler.Decode(script));
                Thread.Sleep(1000); // Sleep for 1 second to avoid issues with MySQL
            }

            // Convert sensors
            if (db_version == "2.0.0.0")
            {
                await Convert_Sensors_2_0_to_2_5();

                // Save sensors
                await Save_Sensors();
            }

            await Update_DB_Version();
        }

        // Update DB version
        public static async Task Update_DB_Version()
        {
            // Check if the database version is different from the current application version
            if (Application_Settings.db_version == await Classes.MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "db_version"))
            {
                Logging.Handler.Debug("Classes.MySQL.Database", "Update_DB_Version", "Database version is already up to date.");
                return;
            }

            await Classes.MySQL.Handler.Execute_Command("UPDATE settings SET db_version = '" + Application_Settings.db_version + "';");
            
            await Classes.MySQL.Handler.Execute_Command("UPDATE accounts SET changelog_read = '0';");
        }

        public static List<Sensors_Entity> sensors_mysql_data;

        public class Sensors_Entity
        {
            public string? id { get; set; } = String.Empty;
            public string? name { get; set; } = String.Empty;
            public string? description { get; set; } = String.Empty;
            public string? author { get; set; } = String.Empty;
            public string? date { get; set; } = String.Empty;
            public string? category { get; set; } = String.Empty;
            public string? sub_category { get; set; } = String.Empty;
            public string? disk_category { get; set; } = String.Empty;
            public string? severity { get; set; } = String.Empty;
            public string? json { get; set; } = String.Empty;
            public string? platform { get; set; } = String.Empty;
        }

        // Sensors conversion script, add platform = Windows to all sensors
        public static async Task Convert_Sensors_2_0_to_2_5()
        {
            sensors_mysql_data = new List<Sensors_Entity>();

            string query = "SELECT * FROM sensors;";
            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();
                MySqlCommand command = new MySqlCommand(query, conn);

                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (reader.HasRows)
                    {
                        while (await reader.ReadAsync())
                        {
                            var entity = new Sensors_Entity
                            {
                                id = reader["id"].ToString() ?? string.Empty,
                                name = reader["name"].ToString() ?? string.Empty,
                                description = reader["description"].ToString() ?? string.Empty,
                                author = reader["author"].ToString() ?? string.Empty,
                                date = reader["date"].ToString() ?? string.Empty,
                                category = reader["category"].ToString() ?? string.Empty,
                                sub_category = reader["sub_category"].ToString() ?? string.Empty,
                                disk_category = reader["disk_category"].ToString() ?? string.Empty,
                                severity = reader["severity"].ToString() ?? string.Empty,
                                json = reader["json"].ToString() ?? string.Empty,
                                platform = "Windows" // Here we set the default value for platform
                            };

                            sensors_mysql_data.Add(entity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }
        }

        // Sensors conversion script, add platform = Windows to all sensors
        public static async Task Save_Sensors()
        {
            foreach (var sensor in sensors_mysql_data)
            {
                string query = "UPDATE sensors SET platform = @platform WHERE id = @id;";
                
                MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);
                
                try
                {
                    await conn.OpenAsync();

                    MySqlCommand command = new MySqlCommand(query, conn);
                    command.Parameters.AddWithValue("@platform", sensor.platform);
                    command.Parameters.AddWithValue("@id", sensor.id);

                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    Logging.Handler.Error("Classes.MySQL.Database", "Result", ex.ToString());
                }
                finally
                {
                    await conn.CloseAsync();
                }
            }
        }

        public static async Task Fix_Settings()
        {
            try
            {
                // smtp
                string smtp = String.Empty;
                smtp = await Classes.MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "smtp");

                // files api key
                string files_api_key = String.Empty;
                files_api_key = await Classes.MySQL.Handler.Quick_Reader("SELECT * FROM settings;", "files_api_key");

                if (String.IsNullOrEmpty(files_api_key))
                {
                    // Generate random files api key if empty
                    if (String.IsNullOrEmpty(files_api_key))
                        files_api_key = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();

                    // Delete old settings
                    await Classes.MySQL.Handler.Execute_Command("DELETE FROM settings;");

                    // Add new settings
                    await Classes.MySQL.Handler.Execute_Command("INSERT INTO settings (db_version, files_api_key, smtp) VALUES ('" + Application_Settings.db_version + "', '" + files_api_key + "', '" + smtp + "');");
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database", "MySQL_Query", ex.ToString());
            }
        }

        public static async Task EnforceCloudSettings() // Preventing data flood on our side, needs to be adjusted on server side too in the future
        {
            string query = "UPDATE settings SET agent_updates_windows_enabled = 0, agent_updates_linux_enabled = 0, agent_updates_macos_enabled = 0, cleanup_applications_drivers_history_enabled = 1, cleanup_applications_drivers_history_days = 1, cleanup_applications_installed_history_enabled = 1, cleanup_applications_installed_history_days = 1, cleanup_applications_logon_history_enabled = 1, cleanup_applications_logon_history_days = 1, cleanup_applications_scheduled_tasks_history_enabled = 1, cleanup_applications_scheduled_tasks_history_days = 1, cleanup_applications_services_history_enabled = 1, cleanup_applications_services_history_days = 1, cleanup_device_information_antivirus_products_history_enabled = 1, cleanup_device_information_antivirus_products_history_days = 1, cleanup_device_information_cpu_history_enabled = 1, cleanup_device_information_cpu_history_days = 1, cleanup_device_information_cronjobs_history_enabled = 1, cleanup_device_information_cronjobs_history_days = 1, cleanup_device_information_disks_history_enabled = 1, cleanup_device_information_disks_history_days = 1, cleanup_device_information_general_history_enabled = 1, cleanup_device_information_general_history_days = 1, cleanup_device_information_history_enabled = 1, cleanup_device_information_history_days = 1, cleanup_device_information_network_adapters_history_enabled = 1, cleanup_device_information_network_adapters_history_days = 1, cleanup_device_information_ram_history_enabled = 1, cleanup_device_information_ram_history_days = 1, cleanup_device_information_task_manager_history_enabled = 1, cleanup_device_information_task_manager_history_days = 1, cleanup_events_history_enabled = 0;";

            MySqlConnection conn = new MySqlConnection(Configuration.MySQL.Connection_String);

            try
            {
                await conn.OpenAsync();

                MySqlCommand command = new MySqlCommand(query, conn);
                
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Classes.MySQL.Database", "Result", ex.ToString());
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
