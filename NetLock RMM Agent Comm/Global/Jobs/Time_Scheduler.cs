﻿using Global.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Management;
using NetLock_RMM_Agent_Comm;

namespace Global.Jobs
{
    internal class Time_Scheduler
    {
        public class Job
        {
            public string id { get; set; }
            public string name { get; set; }
            public string date { get; set; }
            public string last_run { get; set; }
            public string author { get; set; }
            public string description { get; set; }
            public string platform { get; set; }
            public string type { get; set; }
            public string script { get; set; }

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
        }

        public static void Check_Execution()
        {
            Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Check job execution", "Start");

            Initialization.Health.Check_Directories();

            try
            {
                DateTime os_up_time = Global.Helper.Globalization.GetLastBootUpTime(); // Environment.TickCount is not reliable, use WMI instead

                List<Job> job_items = JsonSerializer.Deserialize<List<Job>>(Device_Worker.policy_jobs_json);

                // Write each job to disk if not already exists, check if script has changed
                foreach (var job in job_items)
                {
                    // Check if job is for the current platform
                    if (OperatingSystem.IsWindows() && job.platform != "Windows")
                        continue;
                    else if (OperatingSystem.IsLinux() && job.platform != "Linux")
                        continue;
                    else if (OperatingSystem.IsMacOS() && job.platform != "MacOS")
                        continue;
                    
                    Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Check if job exists on disk", "Job: " + job.name + " Job id: " + job.id);

                    string job_json = JsonSerializer.Serialize(job);
                    string job_path = Path.Combine(Application_Paths.program_data_jobs, job.id + ".json");

                    if (!File.Exists(job_path))
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Check if job exists on disk", "false");
                        File.WriteAllText(job_path, job_json);
                    }

                    // Check if script has changed
                    if (File.Exists(job_path))
                    {
                        string existing_job_json = File.ReadAllText(job_path);
                        Job existing_job = JsonSerializer.Deserialize<Job>(existing_job_json);
                        if (existing_job.script != job.script)
                        {
                            Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Job script has changed. Updating it.", "Job: " + job.name + " Job id: " + job.id);
                            File.WriteAllText(job_path, job_json);
                        }
                    }
                }

                // Clean up old jobs not existing anymore
                foreach (string file in Directory.GetFiles(Application_Paths.program_data_jobs))
                {
                    Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Clean old jobs", "Job: " + file);

                    string file_name = Path.GetFileName(file);
                    string file_id = file_name.Replace(".json", "");

                    bool found = false;

                    foreach (var job in job_items)
                    {
                        if (job.id == file_id)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Clean old jobs", "Delete job: " + file);
                        File.Delete(file);
                    }
                }

                // Now read & consume each job
                foreach (var job in Directory.GetFiles(Application_Paths.program_data_jobs))
                {
                    string job_json = File.ReadAllText(job);
                    Job job_item = JsonSerializer.Deserialize<Job>(job_json);

                    Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Check job execution", "Job: " + job_item.name + " time_scheduler_type: " + job_item.time_scheduler_type);

                    // Check enabled
                    /*if (!job_item.enabled)
                    {
                        Logging.Handler.Jobs("Jobs.Time_Scheduler.Check_Execution", "Check job execution", "Job disabled");

                        continue;
                    }*/

                    bool execute = false;

                    if (job_item.time_scheduler_type == 0) // system boot
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "System boot", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run ?? DateTime.Now.ToString()) + " Last boot: " + os_up_time.ToString());

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = DateTime.Now.ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        if (DateTime.Parse(job_item.last_run) < os_up_time)
                            execute = true;
                    }
                    else if (job_item.time_scheduler_type == 1) // date & time
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "date & time", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run ?? DateTime.Now.ToString()));

                        DateTime scheduledDateTime = DateTime.ParseExact($"{job_item.time_scheduler_date.Split(' ')[0]} {job_item.time_scheduler_time}", "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                        // Check if last run is empty, if so, subsract 24 hours from scheduled time to trigger the execution
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = (scheduledDateTime - TimeSpan.FromHours(24)).ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        DateTime lastRunDateTime = DateTime.Parse(job_item.last_run);

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "date & time", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " scheduledDateTime: " + scheduledDateTime.ToString() + " execute: " + execute.ToString());

                        if (DateTime.Now.Date >= scheduledDateTime.Date && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;
                    }
                    else if (job_item.time_scheduler_type == 2) // all x seconds
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "all x seconds", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = DateTime.Now.ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        if (DateTime.Parse(job_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(job_item.time_scheduler_seconds))
                            execute = true;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "all x seconds", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (job_item.time_scheduler_type == 3) // all x minutes
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "all x minutes", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = DateTime.Now.ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        if (DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(job_item.time_scheduler_minutes))
                            execute = true;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "all x minutes", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (job_item.time_scheduler_type == 4) // all x hours
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "all x hours", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = DateTime.Now.ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        if (DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromHours(job_item.time_scheduler_hours))
                            execute = true;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "all x hours", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (job_item.time_scheduler_type == 5) // date, all x seconds
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "date, all x seconds", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = DateTime.Now.ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        if (DateTime.Now.Date == DateTime.Parse(job_item.time_scheduler_date).Date && DateTime.Parse(job_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(job_item.time_scheduler_seconds))
                            execute = true;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "date, all x seconds", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (job_item.time_scheduler_type == 6) // date, all x minutes
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "date, all x minutes", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = DateTime.Now.ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        if (DateTime.Now.Date == DateTime.Parse(job_item.time_scheduler_date).Date && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(job_item.time_scheduler_minutes))
                            execute = true;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "date, all x minutes", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (job_item.time_scheduler_type == 7) // date, all x hours
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "date, all x hours", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = DateTime.Now.ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        if (DateTime.Now.Date == DateTime.Parse(job_item.time_scheduler_date).Date && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromHours(job_item.time_scheduler_hours))
                            execute = true;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "date, all x hours", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (job_item.time_scheduler_type == 8) // following days at X time
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "following days at X time", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run ?? DateTime.Now.ToString()));

                        DateTime scheduledDateTime = DateTime.ParseExact($"{job_item.time_scheduler_date.Split(' ')[0]} {job_item.time_scheduler_time}", "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                        // Check if last run is empty, if so, subsract 24 hours from scheduled time to trigger the execution
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = (scheduledDateTime - TimeSpan.FromHours(24)).ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        DateTime lastRunDateTime = DateTime.Parse(job_item.last_run);

                        if (DateTime.Now.DayOfWeek.ToString() == "Monday" && job_item.time_scheduler_monday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Tuesday" && job_item.time_scheduler_tuesday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Wednesday" && job_item.time_scheduler_wednesday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Thursday" && job_item.time_scheduler_thursday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Friday" && job_item.time_scheduler_friday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Saturday" && job_item.time_scheduler_saturday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Sunday" && job_item.time_scheduler_sunday && DateTime.Now.TimeOfDay >= scheduledDateTime.TimeOfDay && lastRunDateTime < scheduledDateTime)
                            execute = true;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "following days at X time", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (job_item.time_scheduler_type == 9) // following days, x seconds
                    {
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "following days, x seconds", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run ?? DateTime.Now.ToString()));

                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = DateTime.Now.ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        if (DateTime.Now.DayOfWeek.ToString() == "Monday" && job_item.time_scheduler_monday && DateTime.Parse(job_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(job_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Tuesday" && job_item.time_scheduler_tuesday && DateTime.Parse(job_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(job_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Wednesday" && job_item.time_scheduler_wednesday && DateTime.Parse(job_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(job_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Thursday" && job_item.time_scheduler_thursday && DateTime.Parse(job_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(job_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Friday" && job_item.time_scheduler_friday && DateTime.Parse(job_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(job_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Saturday" && job_item.time_scheduler_saturday && DateTime.Parse(job_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(job_item.time_scheduler_seconds))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Sunday" && job_item.time_scheduler_sunday && DateTime.Parse(job_item.last_run) <= DateTime.Now - TimeSpan.FromSeconds(job_item.time_scheduler_seconds))
                            execute = true;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "following days, x seconds", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (job_item.time_scheduler_type == 10) // following days, x minutes
                    {
                        // Check if last run is empty, if so set it to now
                        if (String.IsNullOrEmpty(job_item.last_run))
                            job_item.last_run = DateTime.Now.ToString();

                        if (DateTime.Now.DayOfWeek.ToString() == "Monday" && job_item.time_scheduler_monday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(job_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Tuesday" && job_item.time_scheduler_tuesday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(job_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Wednesday" && job_item.time_scheduler_wednesday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(job_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Thursday" && job_item.time_scheduler_thursday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(job_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Friday" && job_item.time_scheduler_friday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(job_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Saturday" && job_item.time_scheduler_saturday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(job_item.time_scheduler_minutes))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Sunday" && job_item.time_scheduler_sunday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromMinutes(job_item.time_scheduler_minutes))
                            execute = true;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "following days, x minutes", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " execute: " + execute.ToString());
                    }
                    else if (job_item.time_scheduler_type == 11) // following days, x hours
                    {
                        DateTime scheduledDateTime = DateTime.ParseExact($"{job_item.time_scheduler_date.Split(' ')[0]} {job_item.time_scheduler_time}", "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);

                        // Check if last run is empty, if so, subsract 24 hours from scheduled time to trigger the execution
                        if (String.IsNullOrEmpty(job_item.last_run))
                        {
                            job_item.last_run = (scheduledDateTime - TimeSpan.FromHours(24)).ToString();
                            string updated_job_json = JsonSerializer.Serialize(job_item);
                            File.WriteAllText(job, updated_job_json);
                        }

                        DateTime lastRunDateTime = DateTime.Parse(job_item.last_run);

                        if (DateTime.Now.DayOfWeek.ToString() == "Monday" && job_item.time_scheduler_monday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromHours(job_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Tuesday" && job_item.time_scheduler_tuesday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromHours(job_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Wednesday" && job_item.time_scheduler_wednesday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromHours(job_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Thursday" && job_item.time_scheduler_thursday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromHours(job_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Friday" && job_item.time_scheduler_friday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromHours(job_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Saturday" && job_item.time_scheduler_saturday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromHours(job_item.time_scheduler_hours))
                            execute = true;

                        if (DateTime.Now.DayOfWeek.ToString() == "Sunday" && job_item.time_scheduler_sunday && DateTime.Parse(job_item.last_run) < DateTime.Now - TimeSpan.FromHours(job_item.time_scheduler_hours))
                            execute = true;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "following days, x hours", "name: " + job_item.name + " id: " + job_item.id + " last_run: " + DateTime.Parse(job_item.last_run) + " execute: " + execute.ToString());
                    }

                    // Execute if needed
                    if (execute)
                    {
                        string result = String.Empty;

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Execute job", "name: " + job_item.name + " id: " + job_item.id);

                        //Execute job
                        if (OperatingSystem.IsWindows())
                            result = Windows.Helper.PowerShell.Execute_Script("Jobs.Time_Scheduler.Check_Execution (execute job) " + job_item.name, job_item.script);
                        else if (OperatingSystem.IsLinux())
                            result = Linux.Helper.Bash.Execute_Script("Jobs.Time_Scheduler.Check_Execution (execute job) " + job_item.name, true, job_item.script);
                        else if (OperatingSystem.IsMacOS())
                            result = MacOS.Helper.Zsh.Execute_Script("Jobs.Time_Scheduler.Check_Execution (execute job) " + job_item.name, true, job_item.script);

                        // Insert event
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Job executed", "name: " + job_item.name + " id: " + job_item.id + " result: " + result);

                        // Check if job description is empty
                        if (String.IsNullOrEmpty(job_item.description) && Configuration.Agent.language == "en-US")
                            job_item.description = "No description";
                        else if (String.IsNullOrEmpty(job_item.description) && Configuration.Agent.language == "de-DE")
                            job_item.description = "Keine Beschreibung";

                        if (Configuration.Agent.language == "en-US")
                            Events.Logger.Insert_Event("0", "Job", job_item.name + " completed", "Job: " + job_item.name + " (" + job_item.description + ") " + Environment.NewLine + Environment.NewLine + "Result: " + Environment.NewLine + result, String.Empty, 1, 0);
                        else if (Configuration.Agent.language == "de-DE")
                            Events.Logger.Insert_Event("0", "Job", job_item.name + " fertiggestellt.", "Job: " + job_item.name + " (" + job_item.description + ") " + Environment.NewLine + Environment.NewLine + "Ergebnis: " + Environment.NewLine + result, String.Empty, 1, 1);

                        // Update last run
                        job_item.last_run = DateTime.Now.ToString();
                        string updated_job_json = JsonSerializer.Serialize(job_item);
                        File.WriteAllText(job, updated_job_json);

                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Execution finished", "name: " + job_item.name + " id: " + job_item.id);
                    }
                    else
                        Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Job will not be executed", "name: " + job_item.name + " id: " + job_item.id);
                }

                Logging.Jobs("Jobs.Time_Scheduler.Check_Execution", "Check job execution", "Stop");
            }
            catch (Exception ex)
            {
                Logging.Error("Jobs.Time_Scheduler.Check_Execution", "General Error", ex.ToString());
            }
        }
    }
}
