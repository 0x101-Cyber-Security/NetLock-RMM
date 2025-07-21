using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using NetLock_RMM_Agent_Comm;
using Global.Helper;

namespace Global.Events
{
    internal class Logger
    {
        public static void Insert_Event(string severity, string reported_by, string _event, string description, string notification_json, int type, int language)
        {
            while (Device_Worker.events_processing == true)
            {
                Logging.Debug("Events.Logger.Insert_Event", "", "Events processing. Waiting...");
                Thread.Sleep(1000);
            }

            try
            {
                Device_Worker.events_data_table.Rows.Add(severity, reported_by, _event, description, type, language, notification_json);
                Logging.Debug("Events.Logger.Insert_Event", "", "Done");
            }
            catch (Exception ex)
            {
                Logging.Error("Events.Logger.Insert_Event", "Failed", ex.ToString());
            }
        }

        public static void Consume_Events()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            try
            {
                Logging.Debug("Events.Logger.Consume_Events", "", "Start");

                using (SqliteConnection db_conn = new SqliteConnection(Application_Settings.NetLock_Events_Database_String))
                {
                    db_conn.Open();

                    foreach (DataRow row in Device_Worker.events_data_table.Rows)
                    {
                        SqliteCommand command = new SqliteCommand("INSERT INTO events ('severity', 'reported_by', 'event', 'description', 'notification_json', 'type', 'language', 'status') VALUES (" +
                            "'" + Encryption.String_Encryption.Encrypt(row["severity"].ToString(), Application_Settings.NetLock_Local_Encryption_Key) + "', " + //_event
                            "'" + Encryption.String_Encryption.Encrypt(row["reported_by"].ToString(), Application_Settings.NetLock_Local_Encryption_Key) + "', " + //_event
                            "'" + Encryption.String_Encryption.Encrypt(row["event"].ToString(), Application_Settings.NetLock_Local_Encryption_Key) + "', " + //_event
                            "'" + Encryption.String_Encryption.Encrypt(row["description"].ToString(), Application_Settings.NetLock_Local_Encryption_Key) + "', " + //description
                            "'" + Encryption.String_Encryption.Encrypt(row["notification_json"].ToString(), Application_Settings.NetLock_Local_Encryption_Key) + "', " + //notification_json
                            "'" + Encryption.String_Encryption.Encrypt(row["type"].ToString(), Application_Settings.NetLock_Local_Encryption_Key) + "', " + //type
                            "'" + Encryption.String_Encryption.Encrypt(row["language"].ToString(), Application_Settings.NetLock_Local_Encryption_Key) + "', " + //language
                            "'" + Encryption.String_Encryption.Encrypt("0", Application_Settings.NetLock_Local_Encryption_Key) + "'" + //status
                            ")"
                            , db_conn);

                        command.ExecuteNonQuery();
                    }

                    db_conn.Close();
                    db_conn.Dispose();
                }

                Device_Worker.events_data_table.Rows.Clear();

                Logging.Debug("Events.Logger.Consume_Events", "", "Stop");
            }
            catch (Exception ex)
            {
                Logging.Error("Events.Logger.Consume_Events", "Failed", ex.ToString());
            }
        }

        public static async Task Process_Events()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            try
            {
                Logging.Debug("Events.Logger.Process_Events", "", "Started");

                int count = 0;

                using (SqliteConnection db_conn = new SqliteConnection(Application_Settings.NetLock_Events_Database_String))
                {
                    db_conn.Open();

                    //Count Reader
                    SqliteCommand count_scdCommand = new SqliteCommand("SELECT * FROM events;", db_conn);
                    SqliteDataReader count_reader = count_scdCommand.ExecuteReader();

                    while (count_reader.Read())
                        count++;

                    //If events existing to report, do connection check
                    if (count == 0)
                    {
                        Logging.Debug("Events.Logger.Process_Events", "Get Events", "No events. Canceling event processing...");
                        return;
                    }

                    //Connection result
                    if (!Device_Worker.communication_server_status)
                    {
                        Logging.Debug("Events.Logger.Process_Events", "Connection Check", "Not online.");
                        return;
                    }
                    else
                        Logging.Debug("Events.Logger.Process_Events", "Connection Check", "Online.");

                    //Start processing
                    SqliteCommand scdCommand = new SqliteCommand("SELECT * FROM events;", db_conn);
                    SqliteDataReader reader = scdCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        Logging.Debug("Events.Logger.Process_Events", "Process Event", Environment.NewLine + "Reported by: " + reader["reported_by"].ToString() + Environment.NewLine + "Event: " + reader["event"].ToString() + Environment.NewLine + "Description: " + reader["description"].ToString() + Environment.NewLine + "Type: " + reader["type"].ToString() + Environment.NewLine + "Language: " + reader["language"].ToString());

                        //Send status if not send already 
                        bool status_send = false;

                        if (Encryption.String_Encryption.Decrypt(reader["status"].ToString(), Application_Settings.NetLock_Local_Encryption_Key) == "0")
                            status_send = await Events.Sender.Send_Event(Encryption.String_Encryption.Decrypt(reader["severity"].ToString(), Application_Settings.NetLock_Local_Encryption_Key), Encryption.String_Encryption.Decrypt(reader["reported_by"].ToString(), Application_Settings.NetLock_Local_Encryption_Key), Encryption.String_Encryption.Decrypt(reader["event"].ToString(), Application_Settings.NetLock_Local_Encryption_Key), Encryption.String_Encryption.Decrypt(reader["description"].ToString(), Application_Settings.NetLock_Local_Encryption_Key), Encryption.String_Encryption.Decrypt(reader["notification_json"].ToString(), Application_Settings.NetLock_Local_Encryption_Key), Encryption.String_Encryption.Decrypt(reader["type"].ToString(), Application_Settings.NetLock_Local_Encryption_Key), Encryption.String_Encryption.Decrypt(reader["language"].ToString(), Application_Settings.NetLock_Local_Encryption_Key));

                        //If status send success, update its status in db
                        if (status_send)
                        {
                            SqliteCommand command = new SqliteCommand("UPDATE events SET status = '" + Encryption.String_Encryption.Encrypt("1", Application_Settings.NetLock_Local_Encryption_Key) + "' WHERE id = '" + reader["id"].ToString() + "';", db_conn);
                            command.ExecuteNonQuery();
                        }

                        Logging.Debug("Events.Logger.Process_Events", "Process Event", "Send status: " + status_send);

                        //Delete event if everything done
                        bool log_delete = false;

                        if (Encryption.String_Encryption.Decrypt(reader["status"].ToString(), Application_Settings.NetLock_Local_Encryption_Key) == "1")
                            log_delete = true;

                        Logging.Debug("Events.Logger.Process_Events", "Process Event", "log_delete: " + log_delete);

                        //Delete processed event
                        if (log_delete)
                        {
                            SqliteCommand command = new SqliteCommand("DELETE FROM events WHERE id = '" + reader["id"].ToString() + "';", db_conn);
                            command.ExecuteNonQuery();
                        }
                    }

                    reader.Close();
                    db_conn.Close();
                    db_conn.Dispose();

                    Logging.Debug("Events.Logger.Process_Events", "Stopped", "");
                }
            }
            catch (Exception ex)
            {
                Logging.Debug("Events.Logger.Process_Events", "Failed", ex.ToString());
            }
        }
    }
}
