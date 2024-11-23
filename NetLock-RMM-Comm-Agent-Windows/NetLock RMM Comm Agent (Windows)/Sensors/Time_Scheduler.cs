using NetLock_Agent.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;
using NetLock_RMM_Comm_Agent_Windows.Microsoft_Defender_Antivirus;
using _x101.HWID_System;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using System.Xml;
using System.Runtime.Remoting.Contexts;
using System.ServiceProcess;
using System.Net.NetworkInformation;
using static NetLock_RMM_Comm_Agent_Windows.Sensors.Time_Scheduler;

namespace NetLock_RMM_Comm_Agent_Windows.Sensors
{
    internal class Time_Scheduler
    {
        public class Sensor
        {
            public string id { get; set; }
            public string name { get; set; }
            public string date { get; set; }
            public string last_run { get; set; }
            public string author { get; set; }
            public string description { get; set; }
            public int severity { get; set; }
            public int category { get; set; }
            public int sub_category { get; set; }
            public int utilization_category { get; set; }
            public int notification_treshold_count { get; set; }
            public int notification_treshold_max { get; set; }
            public string notification_history { get; set; }
            public int action_treshold_count { get; set; }
            public int action_treshold_max { get; set; }

            public string action_history { get; set; }
            public bool auto_reset { get; set; }
            public string script { get; set; }
            public string script_action { get; set; }
            public int cpu_usage { get; set; }
            public string process_name { get; set; }
            public int ram_usage { get; set; }
            public int disk_usage { get; set; }
            public int disk_minimum_capacity { get; set; }
            public int disk_category { get; set; }
            public string disk_letters { get; set; }
            public bool disk_include_network_disks { get; set; }
            public bool disk_include_removable_disks { get; set; }
            public string eventlog { get; set; }
            public string eventlog_event_id { get; set; }
            public string expected_result { get; set; }

            //service sensor
            public string service_name { get; set; }
            public int service_condition { get; set; }
            public int service_action { get; set; }

            //ping sensor
            public string ping_address { get; set; }
            public int ping_timeout { get; set; }
            public int ping_condition { get; set; }

            //time schedule
            public int time_scheduler_type { get; set; }
            public int time_scheduler_seconds { get; set; }
            public int time_scheduler_minutes { get; set; }
            public int time_scheduler_hours { get; set; }
            public string time_scheduler_time { get; set; }
            public string time_scheduler_date { get; set; }
            public bool time_scheduler_monday { get; set; }
            public bool time_scheduler_tuesday { get; set; }
            public bool time_scheduler_wednesday { get; set; }
            public bool time_scheduler_thursday { get; set; }
            public bool time_scheduler_friday { get; set; }
            public bool time_scheduler_saturday { get; set; }
            public bool time_scheduler_sunday { get; set; }

            // NetLock notifications
            public bool notifications_mail { get; set; }
            public bool notifications_microsoft_teams { get; set; }
            public bool notifications_telegram { get; set; }
            public bool notifications_ntfy_sh { get; set; }
        }

        public class Notifications
        {
            public bool mail { get; set; }
            public bool microsoft_teams { get; set; }
            public bool telegram { get; set; }
            public bool ntfy_sh { get; set; }
        }

        public class Process_Information
        {
            public int id { get; set; }
            public string name { get; set; }
            public string cpu { get; set; }
            public string ram { get; set; }
            public string user { get; set; }
            public string created { get; set; }
            public string path { get; set; }
            public string cmd { get; set; }
        }

        public static void Check_Execution()
        {
            Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Check sensor execution", "Start");

            Initialization.Health.Check_Directories();

            string sensor_id = String.Empty; // needed for logging

            try
            {
                DateTime os_up_time = ManagementDateTimeConverter.ToDateTime(Helper.WMI.Search("root\\cimv2", "SELECT LastBootUpTime FROM Win32_OperatingSystem", "LastBootUpTime")); // Environment.TickCount is not reliable, use WMI instead

                List<Sensor> scan_sensorItems = JsonSerializer.Deserialize<List<Sensor>>(Service.policy_sensors_json);

                // Write each sensor to disk if not already exists
                foreach (var sensor in scan_sensorItems)
                {
                    Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Check if sensor exists on disk", "Sensor: " + sensor.name + " Sensor id: " + sensor.id);

                    string sensor_json = JsonSerializer.Serialize(sensor);
                    string sensor_path = Application_Paths.program_data_sensors + "\\" + sensor.id + ".json";

                    if (!File.Exists(sensor_path))
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Check if sensor exists on disk", "false");
                        File.WriteAllText(sensor_path, sensor_json);
                    }
                }

                // Clean up old sensors not existing anymore
                foreach (string file in Directory.GetFiles(Application_Paths.program_data_sensors))
                {
                    Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Clean old sensors", "Sensor: " + file);

                    string file_name = Path.GetFileName(file);
                    string file_id = file_name.Replace(".json", "");

                    bool found = false;

                    foreach (var sensor in scan_sensorItems)
                    {
                        if (sensor.id == file_id)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Clean old sensors", "Delete sensor: " + file);
                        File.Delete(file);
                    }
                }

                // Now read & consume each sensor
                foreach (var sensor in Directory.GetFiles(Application_Paths.program_data_sensors))
                {
                    string sensor_json = File.ReadAllText(sensor);
                    Sensor sensor_item = JsonSerializer.Deserialize<Sensor>(sensor_json);
                    sensor_id = sensor_item.id; // needed for logging

                    Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Check sensor execution", "Sensor: " + sensor_item.name + " time_scheduler_type: " + sensor_item.time_scheduler_type);

                    // Check thresholds
                    // Check notification treshold
                    if (string.IsNullOrEmpty(sensor_item.notification_treshold_count.ToString()))
                    {
                        sensor_item.notification_treshold_count = 0;
                        string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                        File.WriteAllText(sensor, updated_sensor_json);
                    }

                    // Check action treshold
                    if (string.IsNullOrEmpty(sensor_item.action_treshold_count.ToString()))
                    {
                        sensor_item.action_treshold_count = 0;
                        string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                        File.WriteAllText(sensor, updated_sensor_json);
                    }

                    // Check enabled
                    /*if (!sensor_item.enabled)
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Check sensor execution", "Sensor disabled");

                        continue;
                    }*/

                    bool execute = false;

                    if (sensor_item.time_scheduler_type == 0) // system boot
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "System boot", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run ?? DateTime.Now.ToString()) + " Last boot: " + os_up_time.ToString());

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = DateTime.Now.ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        if (DateTime.Parse(sensor_item.last_run) < os_up_time)
                            execute = true;
                    }
                    else if (sensor_item.time_scheduler_type == 1) // date & time
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "date & time", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run ?? DateTime.Now.ToString()));

                        DateTime scheduledDateTime = DateTime.ParseExact($"{sensor_item.time_scheduler_date.Split(' ')[0]} {sensor_item.time_scheduler_time}", "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                        // Check if last run is empty, if so, subsract 24 hours from scheduled time to trigger the execution
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = (scheduledDateTime - TimeSpan.FromHours(24)).ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        DateTime lastRunDateTime = DateTime.Parse(sensor_item.last_run);

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "date & time", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " scheduledDateTime: " + scheduledDateTime.ToString() + " execute: " + execute.ToString());

                        if (DateTime.Now.Date >= scheduledDateTime.Date && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;
                    }
                    else if (sensor_item.time_scheduler_type == 2) // all x seconds
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "all x seconds", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = DateTime.Now.ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        if (DateTime.Parse(sensor_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(sensor_item.time_scheduler_seconds))
                            execute = true;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "all x seconds", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (sensor_item.time_scheduler_type == 3) // all x minutes
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "all x minutes", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = DateTime.Now.ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        if (DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(sensor_item.time_scheduler_minutes))
                            execute = true;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "all x minutes", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (sensor_item.time_scheduler_type == 4) // all x hours
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "all x hours", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = DateTime.Now.ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        if (DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromHours(sensor_item.time_scheduler_hours))
                            execute = true;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "all x hours", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (sensor_item.time_scheduler_type == 5) // date, all x seconds
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "date, all x seconds", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = DateTime.Now.ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        if (DateTime.Now.Date == DateTime.Parse(sensor_item.time_scheduler_date).Date && DateTime.Parse(sensor_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(sensor_item.time_scheduler_seconds))
                            execute = true;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "date, all x seconds", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (sensor_item.time_scheduler_type == 6) // date, all x minutes
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "date, all x minutes", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = DateTime.Now.ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        if (DateTime.Now.Date == DateTime.Parse(sensor_item.time_scheduler_date).Date && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(sensor_item.time_scheduler_minutes))
                            execute = true;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "date, all x minutes", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (sensor_item.time_scheduler_type == 7) // date, all x hours
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "date, all x hours", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = DateTime.Now.ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        if (DateTime.Now.Date == DateTime.Parse(sensor_item.time_scheduler_date).Date && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromHours(sensor_item.time_scheduler_hours))
                            execute = true;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "date, all x hours", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (sensor_item.time_scheduler_type == 8) // following days at X time
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "following days at X time", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run ?? DateTime.Now.ToString()));

                        DateTime scheduledDateTime = DateTime.ParseExact($"{sensor_item.time_scheduler_date.Split(' ')[0]} {sensor_item.time_scheduler_time}", "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                        // Check if last run is empty, if so, subsract 24 hours from scheduled time to trigger the execution
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = (scheduledDateTime - TimeSpan.FromHours(24)).ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        DateTime lastRunDateTime = DateTime.Parse(sensor_item.last_run);

                        if (DateTime.Now.DayOfWeek.ToString() == "Monday" && sensor_item.time_scheduler_monday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Tuesday" && sensor_item.time_scheduler_tuesday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Wednesday" && sensor_item.time_scheduler_wednesday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Thursday" && sensor_item.time_scheduler_thursday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Friday" && sensor_item.time_scheduler_friday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Saturday" && sensor_item.time_scheduler_saturday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Sunday" && sensor_item.time_scheduler_sunday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "following days at X time", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (sensor_item.time_scheduler_type == 9) // following days, x seconds
                    {
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "following days, x seconds", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = DateTime.Now.ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        if (DateTime.Now.DayOfWeek.ToString() == "Monday" && sensor_item.time_scheduler_monday && DateTime.Parse(sensor_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(sensor_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Tuesday" && sensor_item.time_scheduler_tuesday && DateTime.Parse(sensor_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(sensor_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Wednesday" && sensor_item.time_scheduler_wednesday && DateTime.Parse(sensor_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(sensor_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Thursday" && sensor_item.time_scheduler_thursday && DateTime.Parse(sensor_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(sensor_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Friday" && sensor_item.time_scheduler_friday && DateTime.Parse(sensor_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(sensor_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Saturday" && sensor_item.time_scheduler_saturday && DateTime.Parse(sensor_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(sensor_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Sunday" && sensor_item.time_scheduler_sunday && DateTime.Parse(sensor_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(sensor_item.time_scheduler_seconds))
                            execute = true;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "following days, x seconds", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (sensor_item.time_scheduler_type == 10) // following days, x minutes
                    {
                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                            sensor_item.last_run = DateTime.Now.ToString();

                        if (DateTime.Now.DayOfWeek.ToString() == "Monday" && sensor_item.time_scheduler_monday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(sensor_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Tuesday" && sensor_item.time_scheduler_tuesday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(sensor_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Wednesday" && sensor_item.time_scheduler_wednesday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(sensor_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Thursday" && sensor_item.time_scheduler_thursday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(sensor_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Friday" && sensor_item.time_scheduler_friday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(sensor_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Saturday" && sensor_item.time_scheduler_saturday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(sensor_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Sunday" && sensor_item.time_scheduler_sunday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(sensor_item.time_scheduler_minutes))
                            execute = true;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "following days, x minutes", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (sensor_item.time_scheduler_type == 11) // following days, x hours
                    {
                        DateTime scheduledDateTime = DateTime.ParseExact($"{sensor_item.time_scheduler_date.Split(' ')[0]} {sensor_item.time_scheduler_time}", "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                        // Check if last run is empty, if so, subsract 24 hours from scheduled time to trigger the execution
                        if (String.IsNullOrEmpty(sensor_item.last_run))
                        {
                            sensor_item.last_run = (scheduledDateTime - TimeSpan.FromHours(24)).ToString();
                            string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                            File.WriteAllText(sensor, updated_sensor_json);
                        }

                        DateTime lastRunDateTime = DateTime.Parse(sensor_item.last_run);

                        if (DateTime.Now.DayOfWeek.ToString() == "Monday" && sensor_item.time_scheduler_monday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromHours(sensor_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Tuesday" && sensor_item.time_scheduler_tuesday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromHours(sensor_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Wednesday" && sensor_item.time_scheduler_wednesday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromHours(sensor_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Thursday" && sensor_item.time_scheduler_thursday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromHours(sensor_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Friday" && sensor_item.time_scheduler_friday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromHours(sensor_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Saturday" && sensor_item.time_scheduler_saturday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromHours(sensor_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Sunday" && sensor_item.time_scheduler_sunday && DateTime.Parse(sensor_item.last_run) < DateTime.Now - TimeSpan.FromHours(sensor_item.time_scheduler_hours))
                            execute = true;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "following days, x hours", "name: " + sensor_item.name + " id: " + sensor_item.id + " last_run: " + DateTime.Parse(sensor_item.last_run) + " execute: " + execute.ToString());
                    }

                    // Execute if needed
                    if (execute)
                    {
                        bool triggered = false;
                        var endTime = DateTime.Now;

                        string action_result = String.Empty;

                        if (sensor_item.action_treshold_max != 1)
                            action_result = "[" + DateTime.Now.ToString() + "]";

                        string details = String.Empty;
                        string additional_details = String.Empty;
                        string notification_history = String.Empty;
                        string action_history = String.Empty;

                        List<Process_Information> process_information_list = new List<Process_Information>();

                        int resource_usage = 0;

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Execute sensor", "name: " + sensor_item.name + " id: " + sensor_item.id);

                        if (sensor_item.category == 0) // utilization 
                        {
                            if (sensor_item.sub_category == 0) // cpu
                            {
                                resource_usage = Device_Information.Hardware.CPU_Utilization();

                                if (sensor_item.cpu_usage < resource_usage) // Check if CPU utilization is higher than the treshold
                                {
                                    triggered = true;

                                    // if action treshold is reached, execute the action and reset the counter
                                    if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                    {
                                        action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                        // Create action history if not exists
                                        if (String.IsNullOrEmpty(sensor_item.action_history))
                                        {
                                            List<string> action_history_list = new List<string>
                                            {
                                                action_result
                                            };

                                            sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                        }
                                        else // if exists, add the result to the list
                                        {
                                            List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                            action_history_list.Add(action_result);
                                            sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                        }

                                        // Reset the counter
                                        sensor_item.action_treshold_count = 0;
                                        string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                        File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                    }
                                    else // if not, increment the counter
                                    {
                                        sensor_item.action_treshold_count++;
                                        string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                        File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                    }

                                    // Create event
                                    if (Service.language == "en-US")
                                    {
                                        details =
                                            "Name: " + sensor_item.name + Environment.NewLine +
                                            "Description: " + sensor_item.description + Environment.NewLine +
                                            "Type: Processor" + Environment.NewLine +
                                            "Selected limit: " + sensor_item.cpu_usage + " (%)" + Environment.NewLine +
                                            "In usage: " + resource_usage + " (%)" + Environment.NewLine +
                                            "Action result: " + Environment.NewLine + action_result;
                                    }
                                    else if (Service.language == "de-DE")
                                    {
                                        details =
                                           "Name: " + sensor_item.name + Environment.NewLine +
                                           "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                           "Typ: Prozessor" + Environment.NewLine +
                                           "Festgelegtes Limit: " + sensor_item.cpu_usage + " (%)" + Environment.NewLine +
                                           "In Verwendung: " + resource_usage + " (%)" + Environment.NewLine +
                                           "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                    }

                                    // Create notification history if not exists
                                    if (String.IsNullOrEmpty(sensor_item.notification_history))
                                    {
                                        List<string> notification_history_list = new List<string>
                                        {
                                            details
                                        };

                                        sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                    }
                                    else // if exists, add the result to the list
                                    {
                                        List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                        notification_history_list.Add(details);
                                        sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                    }
                                }
                                else
                                    continue;
                            }
                            else if (sensor_item.sub_category == 1) // RAM
                            {
                                int ram_usage = Device_Information.Hardware.RAM_Utilization();

                                if (sensor_item.ram_usage < ram_usage)
                                {
                                    triggered = true;

                                    // if action treshold is reached, execute the action and reset the counter
                                    if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                    {
                                        action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                        // Create action history if not exists
                                        if (String.IsNullOrEmpty(sensor_item.action_history))
                                        {
                                            List<string> action_history_list = new List<string>
                                            {
                                                action_result
                                            };

                                            sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                        }
                                        else // if exists, add the result to the list
                                        {
                                            List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                            action_history_list.Add(action_result);
                                            sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                        }

                                        // Reset the counter
                                        sensor_item.action_treshold_count = 0;
                                        string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                        File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                    }
                                    else // if not, increment the counter
                                    {
                                        sensor_item.action_treshold_count++;
                                        string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                        File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                    }

                                    // Create event
                                    if (Service.language == "en-US")
                                    {
                                        details =
                                            "Name: " + sensor_item.name + Environment.NewLine +
                                            "Description: " + sensor_item.description + Environment.NewLine +
                                            "Type: RAM" + Environment.NewLine +
                                            "Selected limit: " + sensor_item.ram_usage + " (%)" + Environment.NewLine +
                                            "In usage: " + ram_usage + " (%)" + Environment.NewLine +
                                            "Action result: " + Environment.NewLine + action_result;
                                    }
                                    else if (Service.language == "de-DE")
                                    {
                                        details =
                                           "Name: " + sensor_item.name + Environment.NewLine +
                                           "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                           "Typ: Arbeitsspeicher" + Environment.NewLine +
                                           "Festgelegtes Limit: " + sensor_item.ram_usage + " (%)" + Environment.NewLine +
                                           "In Verwendung: " + ram_usage + " (%)" + Environment.NewLine +
                                           "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                    }

                                    // Create notification history if not exists
                                    if (String.IsNullOrEmpty(sensor_item.notification_history))
                                    {
                                        List<string> notification_history_list = new List<string>
                                        {
                                            details
                                        };

                                        sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                    }
                                    else // if exists, add the result to the list
                                    {
                                        List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                        notification_history_list.Add(details);
                                        sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                    }
                                }
                                else
                                    continue;
                            }
                            else if (sensor_item.sub_category == 2) // Disks
                            {
                                // Get all disks
                                List<DriveInfo> drives = DriveInfo.GetDrives().ToList();

                                List<string> drive_letters = sensor_item.disk_letters.Split(',').Select(letter => letter.Trim()).ToList();

                                foreach (var drive in drives)
                                {
                                    string drive_name = drive.Name.Replace(":\\", "");

                                    Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "foreach drive", "name: " + drive_name + " " + true.ToString());

                                    // Check if the disk is included in the drives list that should be checked
                                    if (drive_letters.Contains(drive_name) || drive_letters.Count == 0)
                                    {
                                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "disk_included", "name: " + drive_name + " " + true.ToString());

                                        // Check if the disk is a network disk or a removable disk, if so & they should not be scanned, skip it
                                        if (drive.DriveType == DriveType.Network && !sensor_item.disk_include_network_disks)
                                            continue;

                                        if (drive.DriveType == DriveType.Removable && !sensor_item.disk_include_removable_disks)
                                            continue;

                                        // Get specification
                                        string specification = String.Empty;
                                        // if type 0 = gb
                                        if (sensor_item.disk_category == 0 || sensor_item.disk_category == 1)
                                            specification = "(GB)";
                                        else if (sensor_item.disk_category == 2 || sensor_item.disk_category == 3)
                                            specification = "(%)";

                                        // Check disk usage
                                        int drive_total_space_gb = Device_Information.Hardware.Drive_Size(Convert.ToChar(drive_name));
                                        int drive_free_space_gb = Device_Information.Hardware.Drive_Size(Convert.ToChar(drive_name));
                                        int drive_usage = Device_Information.Hardware.Drive_Usage(sensor_item.disk_category, Convert.ToChar(drive_name));
                                        
                                        // 0 = More than X GB occupied, 1 = Less than X GB free, 2 = More than X percent occupied, 3 = Less than X percent free
                                        if (sensor_item.disk_category == 0) // 0 = More than X GB occupied
                                        {
                                            if (drive_usage > sensor_item.disk_usage && drive_total_space_gb > sensor_item.disk_minimum_capacity)
                                            {
                                                triggered = true;

                                                // if action treshold is reached, execute the action and reset the counter
                                                if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                                {
                                                    action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                                    // Create action history if not exists
                                                    if (String.IsNullOrEmpty(sensor_item.action_history))
                                                    {
                                                        List<string> action_history_list = new List<string>
                                                        {
                                                            action_result
                                                        };

                                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                                    }
                                                    else // if exists, add the result to the list
                                                    {
                                                        List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                                        action_history_list.Add(action_result);
                                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                                    }

                                                    // Reset the counter
                                                    sensor_item.action_treshold_count = 0;
                                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                                }
                                                else // if not, increment the counter
                                                {
                                                    sensor_item.action_treshold_count++;
                                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                                }

                                                // Create event
                                                if (Service.language == "en-US")
                                                {
                                                    details =
                                                        "Time: " + sensor_item.name + Environment.NewLine +
                                                        "Description: " + sensor_item.description + Environment.NewLine +
                                                        "Type: Drive (more than X GB occupied)" + Environment.NewLine +
                                                        "Drive size: " + drive_total_space_gb + " (GB)" + Environment.NewLine +
                                                        "Drive free space: " + drive_free_space_gb + " (GB)" + Environment.NewLine +
                                                        "Selected limit: " + sensor_item.disk_usage + $" {specification}" + Environment.NewLine +
                                                        "In usage: " + drive_usage + $" {specification}" + Environment.NewLine +
                                                        "Action result: " + Environment.NewLine + action_result;
                                                }
                                                else if (Service.language == "de-DE")
                                                {
                                                    details =
                                                        "Uhrzeit: " + DateTime.Now + Environment.NewLine +
                                                        "Name: " + sensor_item.name + Environment.NewLine +
                                                        "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                                        "Typ: Laufwerk (mehr als X GB belegt)" + Environment.NewLine +
                                                        "Laufwerksgröße: " + drive_total_space_gb + " (GB)" + Environment.NewLine +
                                                        "Freier Laufwerksspeicher: " + drive_free_space_gb + " (GB)" + Environment.NewLine +
                                                        "Festgelegtes Limit: " + sensor_item.disk_usage + $" {specification}" + Environment.NewLine +
                                                        "In Verwendung: " + drive_usage + $" {specification}" + Environment.NewLine +
                                                        "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                                }

                                                // Create notification history if not exists
                                                if (String.IsNullOrEmpty(sensor_item.notification_history))
                                                {
                                                    List<string> notification_history_list = new List<string>
                                                    {
                                                        details
                                                    };

                                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                                }
                                                else // if exists, add the result to the list
                                                {
                                                    List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                                    notification_history_list.Add(details);
                                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                                }
                                            }
                                        }
                                        else if (sensor_item.disk_category == 1) // 1 = Less than X GB free
                                        {
                                            if (drive_usage < sensor_item.disk_usage && drive_total_space_gb > sensor_item.disk_minimum_capacity)
                                            {
                                                triggered = true;

                                                // if action treshold is reached, execute the action and reset the counter
                                                if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                                {
                                                    action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                                    // Create action history if not exists
                                                    if (String.IsNullOrEmpty(sensor_item.action_history))
                                                    {
                                                        List<string> action_history_list = new List<string>
                                                        {
                                                            action_result
                                                        };

                                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                                    }
                                                    else // if exists, add the result to the list
                                                    {
                                                        List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                                        action_history_list.Add(action_result);
                                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                                    }

                                                    // Reset the counter
                                                    sensor_item.action_treshold_count = 0;
                                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                                }
                                                else // if not, increment the counter
                                                {
                                                    sensor_item.action_treshold_count++;
                                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                                }

                                                // Create event
                                                if (Service.language == "en-US")
                                                {
                                                    details =
                                                        "Time: " + DateTime.Now + Environment.NewLine +
                                                        "Name: " + sensor_item.name + Environment.NewLine +
                                                        "Description: " + sensor_item.description + Environment.NewLine +
                                                        "Type: Drive (less than X GB free)" + Environment.NewLine +
                                                        "Drive size: " + drive_total_space_gb + " (GB)" + Environment.NewLine +
                                                        "Drive free space: " + drive_free_space_gb + " (GB)" + Environment.NewLine +
                                                        "Selected limit: " + sensor_item.disk_usage + $" {specification}" + Environment.NewLine +
                                                        "In usage: " + drive_usage + $" {specification}" + Environment.NewLine +
                                                        "Action result: " + Environment.NewLine + action_result;
                                                }
                                                else if (Service.language == "de-DE")
                                                {
                                                    details =
                                                        "Uhrzeit: " + DateTime.Now + Environment.NewLine +
                                                        "Name: " + sensor_item.name + Environment.NewLine +
                                                        "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                                        "Typ: Laufwerk (weniger als X GB frei)" + Environment.NewLine +
                                                        "Laufwerksgröße: " + drive_total_space_gb + " (GB)" + Environment.NewLine +
                                                        "Freier Platz auf dem Laufwerk: " + drive_free_space_gb + " (GB)" + Environment.NewLine +
                                                        "Festgelegtes Limit: " + sensor_item.disk_usage + $" {specification}" + Environment.NewLine +
                                                        "In Verwendung: " + drive_usage + $" {specification}" + Environment.NewLine +
                                                        "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                                }

                                                // Create notification history if not exists
                                                if (String.IsNullOrEmpty(sensor_item.notification_history))
                                                {
                                                    List<string> notification_history_list = new List<string>
                                                    {
                                                        details
                                                    };

                                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                                }
                                                else // if exists, add the result to the list
                                                {
                                                    List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                                    notification_history_list.Add(details);
                                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                                }
                                            }
                                        }
                                        else if (sensor_item.disk_category == 2) // 2 = More than X percent occupied
                                        {
                                            if (drive_usage > sensor_item.disk_usage && drive_total_space_gb > sensor_item.disk_minimum_capacity)
                                            {
                                                triggered = true;

                                                // if action treshold is reached, execute the action and reset the counter
                                                if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                                {
                                                    action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                                    // Create action history if not exists
                                                    if (String.IsNullOrEmpty(sensor_item.action_history))
                                                    {
                                                        List<string> action_history_list = new List<string>
                                                        {
                                                            action_result
                                                        };

                                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                                    }
                                                    else // if exists, add the result to the list
                                                    {
                                                        List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                                        action_history_list.Add(action_result);
                                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                                    }

                                                    // Reset the counter
                                                    sensor_item.action_treshold_count = 0;
                                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                                }
                                                else // if not, increment the counter
                                                {
                                                    sensor_item.action_treshold_count++;
                                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                                }

                                                // Create event
                                                if (Service.language == "en-US")
                                                {
                                                    details =
                                                        "Time: " + DateTime.Now + Environment.NewLine +
                                                        "Name: " + sensor_item.name + Environment.NewLine +
                                                        "Description: " + sensor_item.description + Environment.NewLine +
                                                        "Type: Drive (more than X percent occupied)" + Environment.NewLine +
                                                        "Drive size: " + drive_total_space_gb + " (GB)" + Environment.NewLine +
                                                        "Drive free space: " + drive_free_space_gb + " (GB)" + Environment.NewLine +
                                                        "Selected limit: " + sensor_item.disk_usage + $" {specification}" + Environment.NewLine +
                                                        "In usage: " + drive_usage + $" {specification}" + Environment.NewLine +
                                                        "Action result: " + Environment.NewLine + action_result;
                                                }
                                                else if (Service.language == "de-DE")
                                                {
                                                    details =
                                                        "Uhrzeit: " + DateTime.Now + Environment.NewLine +
                                                        "Name: " + sensor_item.name + Environment.NewLine +
                                                        "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                                        "Typ: Laufwerk (mehr als X Prozent belegt)" + Environment.NewLine +
                                                        "Laufwerksgröße: " + drive_total_space_gb + " (GB)" + Environment.NewLine +
                                                        "Freier Platz auf dem Laufwerk: " + drive_free_space_gb + " (GB)" + Environment.NewLine +
                                                        "Festgelegtes Limit: " + sensor_item.disk_usage + $" {specification}" + Environment.NewLine +
                                                        "In Verwendung: " + drive_usage + $" {specification}" + Environment.NewLine +
                                                        "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                                }

                                                // Create notification history if not exists
                                                if (String.IsNullOrEmpty(sensor_item.notification_history))
                                                {
                                                    List<string> notification_history_list = new List<string>
                                                    {
                                                        details
                                                    };

                                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                                }
                                                else // if exists, add the result to the list
                                                {
                                                    List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                                    notification_history_list.Add(details);
                                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                                }
                                            }
                                        }
                                        else if (sensor_item.disk_category == 3) // 3 = Less than X percent free
                                        {
                                            if (drive_usage < sensor_item.disk_usage && drive_total_space_gb > sensor_item.disk_minimum_capacity)
                                            {
                                                triggered = true;

                                                // if action treshold is reached, execute the action and reset the counter
                                                if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                                {
                                                    action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                                    // Create action history if not exists
                                                    if (String.IsNullOrEmpty(sensor_item.action_history))
                                                    {
                                                        List<string> action_history_list = new List<string>
                                                        {
                                                            action_result
                                                        };

                                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                                    }
                                                    else // if exists, add the result to the list
                                                    {
                                                        List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                                        action_history_list.Add(action_result);
                                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                                    }

                                                    // Reset the counter
                                                    sensor_item.action_treshold_count = 0;
                                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                                }
                                                else // if not, increment the counter
                                                {
                                                    sensor_item.action_treshold_count++;
                                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                                }


                                                // Create event
                                                if (Service.language == "en-US")
                                                {
                                                    details =
                                                        "Time: " + DateTime.Now + Environment.NewLine +
                                                        "Name: " + sensor_item.name + Environment.NewLine +
                                                        "Description: " + sensor_item.description + Environment.NewLine +
                                                        "Type: Drive (less than X percent free)" + Environment.NewLine +
                                                        "Drive size: " + drive_total_space_gb + " (GB)" + Environment.NewLine +
                                                        "Drive free space: " + drive_free_space_gb + " (GB)" + Environment.NewLine +
                                                        "Selected limit: " + sensor_item.disk_usage + $" {specification}" + Environment.NewLine +
                                                        "In usage: " + drive_usage + $" {specification}" + Environment.NewLine +
                                                        "Action result: " + Environment.NewLine + action_result;
                                                }
                                                else if (Service.language == "de-DE")
                                                {
                                                    details =
                                                        "Uhrzeit: " + DateTime.Now + Environment.NewLine +
                                                        "Name: " + sensor_item.name + Environment.NewLine +
                                                        "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                                        "Typ: Laufwerk (weniger als X Prozent frei)" + Environment.NewLine +
                                                        "Laufwerksgröße: " + drive_total_space_gb + " (GB)" + Environment.NewLine +
                                                        "Freier Platz auf dem Laufwerk: " + drive_free_space_gb + " (GB)" + Environment.NewLine +
                                                        "Festgelegtes Limit: " + sensor_item.disk_usage + $" {specification}" + Environment.NewLine +
                                                        "In Verwendung: " + drive_usage + $" {specification}" + Environment.NewLine +
                                                        "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                                }

                                                // Create notification history if not exists
                                                if (String.IsNullOrEmpty(sensor_item.notification_history))
                                                {
                                                    List<string> notification_history_list = new List<string>
                                                        {
                                                            details
                                                        };

                                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                                }
                                                else // if exists, add the result to the list
                                                {
                                                    List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                                    notification_history_list.Add(details);
                                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (sensor_item.sub_category == 3) // Process cpu utilization (%)
                            {
                                //foreach process
                                foreach (Process process in Process.GetProcesses())
                                {
                                    if (process.ProcessName.ToLower() != sensor_item.process_name.Replace(".exe", "").ToLower()) // Check if the process name is the same, replace .exe to catch user fails
                                        continue;

                                    resource_usage = Device_Information.Processes.Get_CPU_Usage_By_ID(process.Id);
                                    //Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "process cpu utilization", "name: " + sensor_item.name + " id: " + sensor_item.id);

                                    if (resource_usage > sensor_item.cpu_usage)
                                    {
                                        triggered = true;

                                        //Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "process cpu utilization sensor triggered", "name: " + sensor_item.name + " id: " + sensor_item.id);

                                        int ram = Device_Information.Processes.Get_RAM_Usage_By_ID(process.Id, false);
                                        string user = Device_Information.Processes.Process_Owner(process);
                                        string created = process.StartTime.ToString();
                                        string path = process.MainModule.FileName;
                                        string cmd = Helper.WMI.Search("root\\cimv2", "SELECT * FROM Win32_Process WHERE ProcessId = " + process.Id, "CommandLine");

                                        Process_Information proc_info = new Process_Information
                                        {
                                            id = process.Id,
                                            name = process.ProcessName,
                                            cpu = resource_usage.ToString(),
                                            ram = ram.ToString(),
                                            user = user,
                                            created = created,
                                            path = path,
                                            cmd = cmd
                                        };

                                        process_information_list.Add(proc_info);

                                        // if action treshold is reached, execute the action and reset the counter
                                        if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                        {
                                            action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                            // Create action history if not exists
                                            if (String.IsNullOrEmpty(sensor_item.action_history))
                                            {
                                                List<string> action_history_list = new List<string>
                                                {
                                                    action_result
                                                };

                                                sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                            }
                                            else // if exists, add the result to the list
                                            {
                                                List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                                action_history_list.Add(action_result);
                                                sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                            }

                                            // Reset the counter
                                            sensor_item.action_treshold_count = 0;
                                            string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                            File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                        }
                                        else // if not, increment the counter
                                        {
                                            sensor_item.action_treshold_count++;
                                            string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                            File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                        }

                                        // Create event
                                        if (Service.language == "en-US")
                                        {
                                            details =
                                                "Time: " + DateTime.Now + Environment.NewLine +
                                                "Name: " + sensor_item.name + Environment.NewLine +
                                                "Description: " + sensor_item.description + Environment.NewLine +
                                                "Type: Process CPU usage (%)" + Environment.NewLine +
                                                "Process name: " + sensor_item.process_name + " (" + process.Id + ")" + Environment.NewLine +
                                                "Selected limit: " + sensor_item.cpu_usage + " (%)" + Environment.NewLine +
                                                "In usage: " + resource_usage + " (%)" + Environment.NewLine +
                                                "Ram usage: " + ram + " (MB)" + Environment.NewLine +
                                                "User: " + user + Environment.NewLine +
                                                "Commandline: " + cmd + Environment.NewLine +
                                                "Action result: " + Environment.NewLine + action_result;
                                        }
                                        else if (Service.language == "de-DE")
                                        {
                                            details =
                                                "Uhrzeit: " + DateTime.Now + Environment.NewLine +
                                                "Name: " + sensor_item.name + Environment.NewLine +
                                                "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                                "Typ: Prozess-CPU-Nutzung" + Environment.NewLine +
                                                "Prozess Name: " + sensor_item.process_name + " (%)" + Environment.NewLine +
                                                "Festgelegtes Limit: " + sensor_item.cpu_usage + " (%)" + Environment.NewLine +
                                                "In Verwendung: " + resource_usage + " (%)" + Environment.NewLine +
                                                "Ram Nutzung: " + ram + " (MB)" + Environment.NewLine +
                                                "Benutzer: " + user + Environment.NewLine +
                                                "Commandline: " + cmd + Environment.NewLine +
                                                "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                        }

                                        // Create notification history if not exists
                                        if (String.IsNullOrEmpty(sensor_item.notification_history))
                                        {
                                            List<string> notification_history_list = new List<string>
                                            {
                                                details
                                            };

                                            sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                        }
                                        else // if exists, add the result to the list
                                        {
                                            List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                            notification_history_list.Add(details);
                                            sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                        }
                                    }
                                }
                            }
                            else if (sensor_item.sub_category == 4) // Process ram utilization (%)
                            {
                                //foreach process
                                foreach (Process process in Process.GetProcesses())
                                {
                                    if (process.ProcessName.ToLower() != sensor_item.process_name.Replace(".exe", "").ToLower()) // Check if the process name is the same, replace .exe to catch user fails
                                        continue;

                                    resource_usage = Device_Information.Processes.Get_RAM_Usage_By_ID(process.Id, true);
                                    //Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "process cpu utilization", "name: " + sensor_item.name + " id: " + sensor_item.id);

                                    if (resource_usage > sensor_item.ram_usage)
                                    {
                                        triggered = true;

                                        //Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "process cpu utilization sensor triggered", "name: " + sensor_item.name + " id: " + sensor_item.id);

                                        int ram = Device_Information.Processes.Get_RAM_Usage_By_ID(process.Id, false);
                                        string user = Device_Information.Processes.Process_Owner(process);
                                        string created = process.StartTime.ToString();
                                        string path = process.MainModule.FileName;
                                        string cmd = Helper.WMI.Search("root\\cimv2", "SELECT * FROM Win32_Process WHERE ProcessId = " + process.Id, "CommandLine");

                                        Process_Information proc_info = new Process_Information
                                        {
                                            id = process.Id,
                                            name = process.ProcessName,
                                            cpu = resource_usage.ToString(),
                                            ram = ram.ToString(),
                                            user = user,
                                            created = created,
                                            path = path,
                                            cmd = cmd
                                        };

                                        process_information_list.Add(proc_info);

                                        // if action treshold is reached, execute the action and reset the counter
                                        if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                        {
                                            action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                            // Create action history if not exists
                                            if (String.IsNullOrEmpty(sensor_item.action_history))
                                            {
                                                List<string> action_history_list = new List<string>
                                                {
                                                    action_result
                                                };

                                                sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                            }
                                            else // if exists, add the result to the list
                                            {
                                                List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                                action_history_list.Add(action_result);
                                                sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                            }

                                            // Reset the counter
                                            sensor_item.action_treshold_count = 0;
                                            string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                            File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                        }
                                        else // if not, increment the counter
                                        {
                                            sensor_item.action_treshold_count++;
                                            string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                            File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                        }

                                        // Create event
                                        if (Service.language == "en-US")
                                        {
                                            details =
                                                "Time: " + DateTime.Now + Environment.NewLine +
                                                "Name: " + sensor_item.name + Environment.NewLine +
                                                "Description: " + sensor_item.description + Environment.NewLine +
                                                "Type: Process RAM usage (%)" + Environment.NewLine +
                                                "Process name: " + sensor_item.process_name + " (" + process.Id + ")" + Environment.NewLine +
                                                "Selected limit: " + sensor_item.ram_usage + " (%)" + Environment.NewLine +
                                                "In usage: " + resource_usage + " (%)" + Environment.NewLine +
                                                "Ram usage: " + ram + " (MB)" + Environment.NewLine +
                                                "User: " + user + Environment.NewLine +
                                                "Commandline: " + cmd + Environment.NewLine +
                                                "Action result: " + Environment.NewLine + action_result;
                                        }
                                        else if (Service.language == "de-DE")
                                        {
                                            details =
                                                "Uhrzeit: " + DateTime.Now + Environment.NewLine +
                                                "Name: " + sensor_item.name + Environment.NewLine +
                                                "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                                "Typ: Prozess-RAM-Nutzung (%)" + Environment.NewLine +
                                                "Prozess Name: " + sensor_item.process_name + " (%)" + Environment.NewLine +
                                                "Festgelegtes Limit: " + sensor_item.ram_usage + " (%)" + Environment.NewLine +
                                                "In Verwendung: " + resource_usage + " (%)" + Environment.NewLine +
                                                "Ram Nutzung: " + ram + " (MB)" + Environment.NewLine +
                                                "Benutzer: " + user + Environment.NewLine +
                                                "Commandline: " + cmd + Environment.NewLine +
                                                "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                        }

                                        // Create notification history if not exists
                                        if (String.IsNullOrEmpty(sensor_item.notification_history))
                                        {
                                            List<string> notification_history_list = new List<string>
                                            {
                                                details
                                            };

                                            sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                        }
                                        else // if exists, add the result to the list
                                        {
                                            List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                            notification_history_list.Add(details);
                                            sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                        }
                                    }
                                }
                            }
                            else if (sensor_item.sub_category == 5) // Process ram utilization (MB)
                            {
                                //foreach process
                                foreach (Process process in Process.GetProcesses())
                                {
                                    if (process.ProcessName.ToLower() != sensor_item.process_name.Replace(".exe", "").ToLower()) // Check if the process name is the same, replace .exe to catch user fails
                                        continue;

                                    resource_usage = Device_Information.Processes.Get_RAM_Usage_By_ID(process.Id, false);
                                    //Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "process cpu utilization", "name: " + sensor_item.name + " id: " + sensor_item.id);

                                    if (resource_usage > sensor_item.ram_usage)
                                    {
                                        triggered = true;

                                        //Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "process cpu utilization sensor triggered", "name: " + sensor_item.name + " id: " + sensor_item.id);

                                        int ram = Device_Information.Processes.Get_RAM_Usage_By_ID(process.Id, false);
                                        string user = Device_Information.Processes.Process_Owner(process);
                                        string created = process.StartTime.ToString();
                                        string path = process.MainModule.FileName;
                                        string cmd = Helper.WMI.Search("root\\cimv2", "SELECT * FROM Win32_Process WHERE ProcessId = " + process.Id, "CommandLine");

                                        Process_Information proc_info = new Process_Information
                                        {
                                            id = process.Id,
                                            name = process.ProcessName,
                                            cpu = resource_usage.ToString(),
                                            ram = ram.ToString(),
                                            user = user,
                                            created = created,
                                            path = path,
                                            cmd = cmd
                                        };

                                        process_information_list.Add(proc_info);

                                        // if action treshold is reached, execute the action and reset the counter
                                        if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                        {
                                            action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                            // Create action history if not exists
                                            if (String.IsNullOrEmpty(sensor_item.action_history))
                                            {
                                                List<string> action_history_list = new List<string>
                                                {
                                                    action_result
                                                };

                                                sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                            }
                                            else // if exists, add the result to the list
                                            {
                                                List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                                action_history_list.Add(action_result);
                                                sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                            }

                                            // Reset the counter
                                            sensor_item.action_treshold_count = 0;
                                            string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                            File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                        }
                                        else // if not, increment the counter
                                        {
                                            sensor_item.action_treshold_count++;
                                            string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                            File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                        }

                                        // Create event
                                        if (Service.language == "en-US")
                                        {
                                            details =
                                                "Time: " + DateTime.Now + Environment.NewLine +
                                                "Name: " + sensor_item.name + Environment.NewLine +
                                                "Description: " + sensor_item.description + Environment.NewLine +
                                                "Type: Process RAM usage (MB)" + Environment.NewLine +
                                                "Process name: " + sensor_item.process_name + " (" + process.Id + ")" + Environment.NewLine +
                                                "Selected limit: " + sensor_item.ram_usage + " (MB)" + Environment.NewLine +
                                                "In usage: " + resource_usage + " (MB)" + Environment.NewLine +
                                                "User: " + user + Environment.NewLine +
                                                "Commandline: " + cmd + Environment.NewLine +
                                                "Action result: " + Environment.NewLine + action_result;
                                        }
                                        else if (Service.language == "de-DE")
                                        {
                                            details =
                                                "Uhrzeit: " + DateTime.Now + Environment.NewLine +
                                                "Name: " + sensor_item.name + Environment.NewLine +
                                                "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                                "Typ: Prozess-RAM-Nutzung (MB)" + Environment.NewLine +
                                                "Prozess Name: " + sensor_item.process_name + " (%)" + Environment.NewLine +
                                                "Festgelegtes Limit: " + sensor_item.ram_usage + " (MB)" + Environment.NewLine +
                                                "In Verwendung: " + resource_usage + " (MB)" + Environment.NewLine +
                                                "Benutzer: " + user + Environment.NewLine +
                                                "Commandline: " + cmd + Environment.NewLine +
                                                "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                        }

                                        // Create notification history if not exists
                                        if (String.IsNullOrEmpty(sensor_item.notification_history))
                                        {
                                            List<string> notification_history_list = new List<string>
                                            {
                                                details
                                            };

                                            sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                        }
                                        else // if exists, add the result to the list
                                        {
                                            List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                            notification_history_list.Add(details);
                                            sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                        }
                                    }
                                }
                            }
                        }
                        else if (sensor_item.category == 1) // Windows Eventlog
                        {
                            DateTime startTime = DateTime.Parse(sensor_item.last_run);
                            bool event_log_existing = false;
                            bool action_already_executed = false; // prevents the action from being executed multiple times

                            EventLogQuery query;
                            EventLogReader reader = null;
                            EventRecord eventRecord;

                            try
                            {
                                // Filter by time range and event ID
                                query = new EventLogQuery(sensor_item.eventlog, PathType.LogName, string.Format("*[System[(EventID={0}) and TimeCreated[@SystemTime >= '{1}'] and TimeCreated[@SystemTime <= '{2}']]]", sensor_item.eventlog_event_id, startTime.ToUniversalTime().ToString("o"), endTime.ToUniversalTime().ToString("o")));

                                reader = new EventLogReader(query);

                                event_log_existing = true;

                                Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Check event log existence (" + sensor_item.eventlog + ")", event_log_existing.ToString());

                                // Read each event from event logs
                                while ((eventRecord = reader.ReadEvent()) != null)
                                {
                                    //Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Found events", eventRecord.Id.ToString());

                                    if (DateTime.Parse(sensor_item.last_run) > eventRecord.TimeCreated)
                                    {
                                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Check event time (" + eventRecord.TimeCreated + ")", "Last scan is newer than last event log.");
                                    }
                                    else
                                    {
                                        // Print current scanning event log
                                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Scan event", "EventID: " + eventRecord.Id.ToString() + " Timestamp: " + eventRecord.TimeCreated.Value.ToString() + " lastscan: " + sensor_item.last_run);
                                        
                                        string result = "-";
                                        string content = eventRecord.FormatDescription();
                                        bool regex_match = false;

                                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "event log content", content);

                                        if (String.IsNullOrEmpty(content))
                                        {
                                            content = eventRecord.ToXml();

                                            // code below is not reliable enough in current state. Maybe we can use it in the future.
                                            //XmlDocument doc = new XmlDocument();
                                            //doc.LoadXml(eventRecord.ToXml());

                                            //XmlNode messageNode = doc.SelectSingleNode("//Event/EventData/Data[@Name='Message']");
                                            //content = messageNode?.InnerText ?? "N/A";

                                            Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "event log content xml extracted", content);
                                        }

                                        if (String.IsNullOrEmpty(sensor_item.expected_result))
                                        {
                                            // Increment notification counter for each hit
                                            sensor_item.notification_treshold_count++;

                                            triggered = true;
                                        }
                                        else if (Regex.IsMatch(content, sensor_item.expected_result))
                                        {
                                            // Increment notification counter for each hit
                                            sensor_item.notification_treshold_count++;

                                            triggered = true;
                                        }

                                        // if action treshold is reached, execute the action and reset the counter
                                        if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                        {
                                            if (!action_already_executed)
                                            {
                                                action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);
                                                action_already_executed = true;
                                            }

                                            // Create action history if not exists
                                            if (String.IsNullOrEmpty(sensor_item.action_history))
                                            {
                                                List<string> action_history_list = new List<string>
                                                {
                                                    action_result
                                                };

                                                sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                            }
                                            else // if exists, add the result to the list
                                            {
                                                List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                                action_history_list.Add(action_result);
                                                sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                            }

                                            // Reset the counter
                                            sensor_item.action_treshold_count = 0;
                                            string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                            File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                        }
                                        else // if not, increment the counter
                                        {
                                            sensor_item.action_treshold_count++;
                                            string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                            File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                        }

                                        // Create event
                                        if (Service.language == "en-US")
                                        {
                                            details =
                                                "Time: " + DateTime.Now + Environment.NewLine +
                                                "Name: " + sensor_item.name + Environment.NewLine +
                                                "Description: " + sensor_item.description + Environment.NewLine +
                                                "Type: Windows Eventlog" + Environment.NewLine +
                                                "Eventlog: " + sensor_item.eventlog + Environment.NewLine +
                                                "Event id: " + sensor_item.eventlog_event_id + Environment.NewLine +
                                                "Expected result: " + sensor_item.expected_result + Environment.NewLine +
                                                "Content: " + content + Environment.NewLine +
                                                "Level: " + eventRecord.Level + Environment.NewLine +
                                                "Process id: " + eventRecord.ProcessId + Environment.NewLine +
                                                "Created: " + eventRecord.TimeCreated + Environment.NewLine +
                                                "User id: " + eventRecord.UserId + Environment.NewLine +
                                                "Version: " + eventRecord.Version + Environment.NewLine +
                                                "Action result: " + Environment.NewLine + action_result;
                                        }
                                        else if (Service.language == "de-DE")
                                        {
                                            details =
                                                "Uhrzeit: " + DateTime.Now + Environment.NewLine +
                                                "Name: " + sensor_item.name + Environment.NewLine +
                                                "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                                "Typ: Windows Eventlog" + Environment.NewLine +
                                                "Eventlog: " + sensor_item.eventlog + Environment.NewLine +
                                                "Event ID: " + sensor_item.eventlog_event_id + Environment.NewLine +
                                                "Erwartetes Ergebnis: " + sensor_item.expected_result + Environment.NewLine +
                                                "Inhalt: " + content + Environment.NewLine +
                                                "Level: " + eventRecord.Level + Environment.NewLine +
                                                "Prozess ID: " + eventRecord.ProcessId + Environment.NewLine +
                                                "Erstellt: " + eventRecord.TimeCreated + Environment.NewLine +
                                                "Benutzer ID: " + eventRecord.UserId + Environment.NewLine +
                                                "Version: " + eventRecord.Version + Environment.NewLine +
                                                "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                        }

                                        // Create notification history if not exists
                                        if (String.IsNullOrEmpty(sensor_item.notification_history))
                                        {
                                            List<string> notification_history_list = new List<string>
                                            {
                                                details
                                            };

                                            sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                        }
                                        else // if exists, add the result to the list
                                        {
                                            List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                            notification_history_list.Add(details);
                                            sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                        }

                                        //Trigger incident response | NetLock legacy code
                                        /*if (sensor["incident_response_ruleset"].ToString() != "LQ==")
                                        {
                                            //Trigger it
                                            Incident_Response.Handler.Get_Incident_Response_Ruleset(sensor["incident_response_ruleset"].ToString());
                                        }*/
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Check event log existence (" + sensor_item.eventlog + ")", event_log_existing.ToString() + " error: " + ex.ToString());
                            }
                        }
                        else if (sensor_item.category == 2) // PowerShell
                        {
                            string result = PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script);

                            if (Regex.IsMatch(result, sensor_item.expected_result))
                            {
                                triggered = true;

                                // if action treshold is reached, execute the action and reset the counter
                                if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                {
                                    action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                    // Create action history if not exists
                                    if (String.IsNullOrEmpty(sensor_item.action_history))
                                    {
                                        List<string> action_history_list = new List<string>
                                        {
                                            action_result
                                        };

                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                    }
                                    else // if exists, add the result to the list
                                    {
                                        List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                        action_history_list.Add(action_result);
                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                    }

                                    // Reset the counter
                                    sensor_item.action_treshold_count = 0;
                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                }
                                else // if not, increment the counter
                                {
                                    sensor_item.action_treshold_count++;
                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                }

                                // Create event
                                if (Service.language == "en-US")
                                {
                                    details =
                                        "Name: " + sensor_item.name + Environment.NewLine +
                                        "Description: " + sensor_item.description + Environment.NewLine +
                                        "Type: PowerShell" + Environment.NewLine +
                                        "Script: " + sensor_item.script + Environment.NewLine +
                                        "Pattern: " + sensor_item.expected_result + Environment.NewLine +
                                        "Result: " + result + Environment.NewLine +
                                        "Action result: " + Environment.NewLine + action_result;
                                }
                                else if (Service.language == "de-DE")
                                {
                                    details =
                                        "Name: " + sensor_item.name + Environment.NewLine +
                                        "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                        "Typ: PowerShell" + Environment.NewLine +
                                        "Skript: " + sensor_item.script + Environment.NewLine +
                                        "Pattern: " + sensor_item.expected_result + Environment.NewLine +
                                        "Ergebnis: " + result + Environment.NewLine +
                                        "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                }

                                // Create notification history if not exists
                                if (String.IsNullOrEmpty(sensor_item.notification_history))
                                {
                                    List<string> notification_history_list = new List<string>
                                    {
                                        details
                                    };

                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                }
                                else // if exists, add the result to the list
                                {
                                    List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                    notification_history_list.Add(details);
                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                }
                            } 
                        }
                        else if (sensor_item.category == 3) // Service
                        {
                            bool service_start_failed = false;
                            string service_error_message = String.Empty;
                            string service_status = String.Empty;

                            try
                            {
                                ServiceController sc = new ServiceController(sensor_item.service_name);
                                service_status = sc.Status.Equals(ServiceControllerStatus.Paused).ToString();
                                
                                if (sensor_item.service_condition == 0 && sc.Status.Equals(ServiceControllerStatus.Running)) // if service is running and condition is 0 = running
                                {
                                    triggered = true;

                                    if (sensor_item.service_action == 1) // stop the service if it's running and the action is 1 = stop
                                        sc.Stop();
                                }
                                else if (sensor_item.service_condition == 1 && sc.Status.Equals(ServiceControllerStatus.Paused)) // if service is paused and condition is 1 = paused
                                {
                                    triggered = true;

                                    if (sensor_item.service_action == 2) // restart the service if it's paused and the action is 2 = restart
                                    {
                                        sc.Stop();
                                        sc.WaitForStatus(ServiceControllerStatus.Stopped);
                                        sc.Start();
                                    }
                                }
                                else if (sensor_item.service_condition == 2 && sc.Status.Equals(ServiceControllerStatus.Stopped)) // if service is stopped and condition is 2 = stopped
                                {
                                    triggered = true;

                                    if (sensor_item.service_action == 0) // start the service if it's stopped and the action is 0 = start
                                        sc.Start();
                                }
                            }
                            catch (Exception ex)
                            {
                                service_start_failed = true;
                                service_error_message = ex.Message;
                                Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Checking service state, or performing action failed", ex.ToString());
                            }

                            if (triggered)
                            {
                                // if action treshold is reached, execute the action and reset the counter
                                if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                {
                                    action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                    // Create action history if not exists
                                    if (String.IsNullOrEmpty(sensor_item.action_history))
                                    {
                                        List<string> action_history_list = new List<string>
                                        {
                                            action_result
                                        };

                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                    }
                                    else // if exists, add the result to the list
                                    {
                                        List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                        action_history_list.Add(action_result);
                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                    }

                                    // Reset the counter
                                    sensor_item.action_treshold_count = 0;
                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                }
                                else // if not, increment the counter
                                {
                                    sensor_item.action_treshold_count++;
                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                }

                                // Create event
                                if (Service.language == "en-US")
                                {
                                    if (service_start_failed)
                                    {
                                        details =
                                            "Name: " + sensor_item.name + Environment.NewLine +
                                            "Description: " + sensor_item.description + Environment.NewLine +
                                            "Type: Service" + Environment.NewLine +
                                            "Service: " + sensor_item.service_name + Environment.NewLine +
                                            "Result: The requested service action could not be performed." + Environment.NewLine +
                                            "Error: " + service_error_message + Environment.NewLine +
                                            "Action result: " + Environment.NewLine + action_result;
                                    }
                                    else
                                    {
                                        details =
                                            "Name: " + sensor_item.name + Environment.NewLine +
                                            "Description: " + sensor_item.description + Environment.NewLine +
                                            "Type: Service" + Environment.NewLine +
                                            "Service: " + sensor_item.service_name + Environment.NewLine +
                                            "Result: The requested service action was successfully executed." + Environment.NewLine +
                                            "Action result: " + Environment.NewLine + action_result;
                                    }
                                }
                                else if (Service.language == "de-DE")
                                {
                                    if (service_start_failed)
                                    {
                                        details =
                                            "Name: " + sensor_item.name + Environment.NewLine +
                                            "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                            "Typ: Dienst" + Environment.NewLine +
                                            "Dienst: " + sensor_item.service_name + Environment.NewLine +
                                            "Ergebnis: The requested service action could not be performed." + Environment.NewLine +
                                            "Fehler: " + service_error_message + Environment.NewLine +
                                            "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                    }
                                    else
                                    {
                                        details =
                                            "Name: " + sensor_item.name + Environment.NewLine +
                                            "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                            "Typ: Dienst" + Environment.NewLine +
                                            "Dienst: " + sensor_item.service_name + Environment.NewLine +
                                            "Ergebnis: Die gewünschte Dienst Aktion wurde erfolgreich ausgeführt." + Environment.NewLine +
                                            "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                    }
                                }

                                // Create notification history if not exists
                                if (String.IsNullOrEmpty(sensor_item.notification_history))
                                {
                                    List<string> notification_history_list = new List<string>
                                    {
                                        details
                                    };

                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                }
                                else // if exists, add the result to the list
                                {
                                    List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                    notification_history_list.Add(details);
                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                }
                            }
                        }
                        else if (sensor_item.category == 4) // Ping
                        {
                            bool ping_status = Device_Information.Network.Ping(sensor_item.ping_address, sensor_item.ping_timeout);

                            if (ping_status && sensor_item.ping_condition == 0)
                                triggered = true;
                            else if (!ping_status && sensor_item.ping_condition == 1)
                                triggered = true;

                            if (triggered)
                            {
                                // if action treshold is reached, execute the action and reset the counter
                                if (sensor_item.action_treshold_count >= sensor_item.action_treshold_max)
                                {
                                    action_result += Environment.NewLine + Environment.NewLine + " [" + DateTime.Now.ToString() + "]" + Environment.NewLine + PowerShell.Execute_Script("Sensors.Time_Scheduler.Check_Execution (execute action) " + sensor_item.name, sensor_item.script_action);

                                    // Create action history if not exists
                                    if (String.IsNullOrEmpty(sensor_item.action_history))
                                    {
                                        List<string> action_history_list = new List<string>
                                        {
                                            action_result
                                        };

                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                    }
                                    else // if exists, add the result to the list
                                    {
                                        List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);
                                        action_history_list.Add(action_result);
                                        sensor_item.action_history = JsonSerializer.Serialize(action_history_list);
                                    }

                                    // Reset the counter
                                    sensor_item.action_treshold_count = 0;
                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                }
                                else // if not, increment the counter
                                {
                                    sensor_item.action_treshold_count++;
                                    string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                    File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                                }

                                // Create event
                                string ping_result = String.Empty;

                                if (Service.language == "en-US")
                                {
                                    if (sensor_item.ping_condition == 0)
                                        ping_result = "Successful";
                                    else if (sensor_item.ping_condition == 1)
                                        ping_result = "Failed";

                                    details =
                                        "Name: " + sensor_item.name + Environment.NewLine +
                                        "Description: " + sensor_item.description + Environment.NewLine +
                                        "Type: Ping" + Environment.NewLine +
                                        "Address: " + sensor_item.ping_address + Environment.NewLine +
                                        "Timeout: " + sensor_item.ping_timeout + Environment.NewLine +
                                        "Result: " + ping_result + Environment.NewLine +
                                        "Action result: " + Environment.NewLine + action_result;
                                }
                                else if (Service.language == "de-DE")
                                {
                                    if (sensor_item.ping_condition == 0)
                                        ping_result = "Erfolgreich";
                                    else if (sensor_item.ping_condition == 1)
                                        ping_result = "Fehlgeschlagen";

                                    details =
                                        "Name: " + sensor_item.name + Environment.NewLine +
                                        "Beschreibung: " + sensor_item.description + Environment.NewLine +
                                        "Typ: Ping" + Environment.NewLine +
                                        "Adresse: " + sensor_item.ping_address + Environment.NewLine +
                                        "Timeout: " + sensor_item.ping_timeout + Environment.NewLine +
                                        "Ergebnis: " + ping_result + Environment.NewLine +
                                        "Ergebnis der Aktion: " + Environment.NewLine + action_result;
                                }

                                // Create notification history if not exists
                                if (String.IsNullOrEmpty(sensor_item.notification_history))
                                {
                                    List<string> notification_history_list = new List<string>
                                    {
                                        details
                                    };

                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                }
                                else // if exists, add the result to the list
                                {
                                    List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);
                                    notification_history_list.Add(details);
                                    sensor_item.notification_history = JsonSerializer.Serialize(notification_history_list);
                                }
                            }
                        }

                        // Execution finished, set last run time
                        endTime = DateTime.Now; // set end time for the next scan

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Sensor executed", "name: " + sensor_item.name + " id: " + sensor_item.id);

                        // Build additional details string
                        foreach (var process in process_information_list)
                        {
                            if (Service.language == "en-US")
                                additional_details += Environment.NewLine + "Process ID: " + process.id + Environment.NewLine + "Process name: " + process.name + Environment.NewLine + "Usage: " + process.cpu + " (%)" + Environment.NewLine + "RAM usage: " + process.ram + " (MB)" + Environment.NewLine + "User: " + process.user + Environment.NewLine + "Created: " + process.created + Environment.NewLine + "Path: " + process.path + Environment.NewLine + "Commandline: " + process.cmd + Environment.NewLine;
                            else if (Service.language == "de-DE")
                                additional_details += Environment.NewLine + "Prozess ID: " + process.id + Environment.NewLine + "Prozess Name: " + process.name + Environment.NewLine + "Nutzung: " + process.cpu + " (%)" + Environment.NewLine + "RAM Nutzung: " + process.ram + " (MB)" + Environment.NewLine + "Benutzer: " + process.user + Environment.NewLine + "Erstellt: " + process.created + Environment.NewLine + "Pfad: " + process.path + Environment.NewLine + "Commandline: " + process.cmd + Environment.NewLine;
                        }

                        // Insert event
                        if (triggered)
                        {
                            Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Triggered (id)", triggered.ToString() + " (" + sensor_item.id + ")");
                            
                            // if notification treshold is reached, insert event and reset the counter
                            if (sensor_item.notification_treshold_count >= sensor_item.notification_treshold_max)
                            {
                                // Create action history, if treshold is not 1
                                if (sensor_item.notification_treshold_max != 1)
                                {
                                    List<string> action_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.action_history);

                                    foreach (var action_history_item in action_history_list)
                                        action_history += Environment.NewLine + action_history_item + Environment.NewLine;

                                    Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "action_history", action_history + " (" + sensor_item.id + ")");

                                    // Clear action history
                                    action_history_list.Clear();
                                    sensor_item.action_history = null;
                                    string action_history_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                    File.WriteAllText(sensor, action_history_updated_sensor_json);
                                }

                                // Create notification history
                                if (!String.IsNullOrEmpty(sensor_item.notification_history))
                                {
                                    List<string> notification_history_list = JsonSerializer.Deserialize<List<string>>(sensor_item.notification_history);

                                    foreach (var notification_history_item in notification_history_list)
                                        notification_history += Environment.NewLine + notification_history_item + Environment.NewLine;

                                    Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "notification_history", notification_history + " (" + sensor_item.id + ")");

                                    // Clear notification history
                                    notification_history_list.Clear();
                                    sensor_item.notification_history = null;
                                    string notification_history_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                    File.WriteAllText(sensor, notification_history_updated_sensor_json);
                                }

                                // Create notification_json
                                Notifications notifications = new Notifications
                                {
                                    mail = sensor_item.notifications_mail,
                                    microsoft_teams = sensor_item.notifications_microsoft_teams,
                                    telegram = sensor_item.notifications_telegram,
                                    ntfy_sh = sensor_item.notifications_ntfy_sh
                                };

                                // Serializing the extracted properties to JSON
                                string notifications_json = JsonSerializer.Serialize(notifications, new JsonSerializerOptions { WriteIndented = true });

                                // Create event based on category and sub category
                                if (sensor_item.category == 0) //utilization
                                {
                                    if (sensor_item.sub_category == 0)
                                    {
                                        // CPU usage
                                        if (Service.language == "en-US")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor CPU (" + sensor_item.name +  ")", notification_history + "History of actions" + Environment.NewLine + action_history, notifications_json, 2, 0);
                                        else if (Service.language == "de-DE")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor CPU (" + sensor_item.name + ")", notification_history + "Historie der Aktionen" + Environment.NewLine + action_history, notifications_json, 2, 1);
                                    }
                                    else if (sensor_item.sub_category == 1) // RAM usage
                                    {
                                        if (Service.language == "en-US")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor RAM (" + sensor_item.name + ")", notification_history + "History of actions" + Environment.NewLine + action_history, notifications_json, 2, 0);
                                        else if (Service.language == "de-DE")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor RAM (" + sensor_item.name + ")", notification_history + "Historie der Aktionen" + Environment.NewLine + action_history, notifications_json, 2, 1);
                                    }
                                    else if (sensor_item.sub_category == 2) // Disks
                                    {
                                        if (Service.language == "en-US")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor Drive (" + sensor_item.name + ")", notification_history + "History of actions" + Environment.NewLine + action_history, notifications_json, 2, 0);
                                        else if (Service.language == "de-DE")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor Laufwerk (" + sensor_item.name + ")", notification_history + "Historie der Aktionen" + Environment.NewLine + action_history, notifications_json, 2, 1);
                                    }
                                    else if (sensor_item.sub_category == 3) // CPU process usage
                                    {                                         
                                        if (Service.language == "en-US")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor Process CPU usage (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "Further information" + additional_details + Environment.NewLine + Environment.NewLine + "History of actions" + Environment.NewLine + action_history, notifications_json, 2, 0);
                                        else if (Service.language == "de-DE")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor Prozess-CPU-Nutzung (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "Weitere Informationen" + additional_details + Environment.NewLine + Environment.NewLine + "Historie der Aktionen" + Environment.NewLine + action_history, notifications_json, 2, 1);                       
                                    }
                                    else if (sensor_item.sub_category == 4) // RAM process usage in %
                                    {                                         
                                        if (Service.language == "en-US")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor Process RAM usage (%) (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "Further information" + additional_details + Environment.NewLine + Environment.NewLine + "History of actions" + Environment.NewLine + action_history, notifications_json, 2, 0);
                                        else if (Service.language == "de-DE")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor Prozess-RAM-Nutzung (%) (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "Weitere Informationen" + additional_details + Environment.NewLine + Environment.NewLine + "Historie der Aktionen" + Environment.NewLine + action_history, notifications_json, 2, 1);                       
                                    }
                                    else if (sensor_item.sub_category == 5) // RAM process usage in MB
                                    {                                         
                                        if (Service.language == "en-US")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor Process RAM usage (MB) (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "Further information" + additional_details + Environment.NewLine + Environment.NewLine + "History of actions" + Environment.NewLine + action_history, notifications_json, 2, 0);
                                        else if (Service.language == "de-DE")
                                            Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Sensor Prozess-RAM-Nutzung (MB) (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "Weitere Informationen" + additional_details + Environment.NewLine + Environment.NewLine + "Historie der Aktionen" + Environment.NewLine + action_history, notifications_json, 2, 1);                       
                                    }
                                }
                                else if (sensor_item.category == 1) // Windows Eventlog
                                {
                                    if (Service.language == "en-US")
                                        Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Windows Eventlog (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "History of actions" + Environment.NewLine + action_history, notifications_json, 2, 0);
                                    else if (Service.language == "de-DE")
                                        Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Windows Eventlog (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "Historie der Aktionen" + Environment.NewLine + action_history, notifications_json, 2, 1);
                                }
                                else if (sensor_item.category == 2) // PowerShell
                                {
                                    if (Service.language == "en-US")
                                        Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "PowerShell (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "History of actions" + Environment.NewLine + action_history, notifications_json, 2, 0);
                                    else if (Service.language == "de-DE")
                                        Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "PowerShell (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "Historie der Aktionen" + Environment.NewLine + action_history, notifications_json, 2, 1);
                                }
                                else if (sensor_item.category == 3) // Service
                                {
                                    if (Service.language == "en-US")
                                        Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Service (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "History of actions" + Environment.NewLine + action_history, notifications_json, 2, 0);
                                    else if (Service.language == "de-DE")
                                        Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Dienst (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "Historie der Aktionen" + Environment.NewLine + action_history, notifications_json, 2, 1);
                                }
                                else if (sensor_item.category == 4) // Ping
                                {
                                    if (Service.language == "en-US")
                                        Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Ping (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "History of actions" + Environment.NewLine + action_history, notifications_json, 2, 0);
                                    else if (Service.language == "de-DE")
                                        Events.Logger.Insert_Event(sensor_item.severity.ToString(), "Sensor", "Ping (" + sensor_item.name + ")", notification_history + Environment.NewLine + Environment.NewLine + "Historie der Aktionen" + Environment.NewLine + action_history, notifications_json, 2, 1);
                                }

                                sensor_item.notification_treshold_count = 0;
                                string notification_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                File.WriteAllText(sensor, notification_treshold_updated_sensor_json);
                            }
                            else // if not, increment the counter
                            {
                                sensor_item.notification_treshold_count++;
                                string notification_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                File.WriteAllText(sensor, notification_treshold_updated_sensor_json);
                            }
                        }
                        else //not triggered
                        {
                            Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Triggered", false.ToString());

                            // if auto reset is enabled, reset the counters
                            if (sensor_item.auto_reset)
                            {
                                // Reset notification counter
                                sensor_item.notification_treshold_count = 0;
                                string notification_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                File.WriteAllText(sensor, notification_treshold_updated_sensor_json);

                                // Reset action counter
                                sensor_item.action_treshold_count = 0;
                                string action_treshold_updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                                File.WriteAllText(sensor, action_treshold_updated_sensor_json);
                            }
                        }

                        // Update last run
                        sensor_item.last_run = endTime.ToString();
                        string updated_sensor_json = JsonSerializer.Serialize(sensor_item);
                        File.WriteAllText(sensor, updated_sensor_json);

                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Execution finished", "name: " + sensor_item.name + " id: " + sensor_item.id);
                    }
                    else
                        Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Sensor will not be executed", "name: " + sensor_item.name + " id: " + sensor_item.id);
                }

                Logging.Handler.Sensors("Sensors.Time_Scheduler.Check_Execution", "Check sensor execution", "Stop");
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Sensors.Time_Scheduler.Check_Execution", "General Error (id)", ex.ToString() + "(" + sensor_id + ")");
            }
        }
    }
}
