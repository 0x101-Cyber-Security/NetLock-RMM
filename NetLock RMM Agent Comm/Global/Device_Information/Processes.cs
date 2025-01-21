using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;
using Global.Helper;
using System.Text.RegularExpressions;

namespace Global.Device_Information
{
    public class Process_Data
    {
        public string name { get; set; }
        public string pid { get; set; }
        public string parent_name { get; set; }
        public string parent_pid { get; set; }
        public string cpu { get; set; }
        public string ram { get; set; }
        public string user { get; set; }
        public string created { get; set; }
        public string path { get; set; }
        public string cmd { get; set; }
        public string handles { get; set; }
        public string threads { get; set; }
        public string read_operations { get; set; }
        public string read_transfer { get; set; }
        public string write_operations { get; set; }
        public string write_transfer { get; set; }
    }

    internal class Processes
    {
        public static string Collect()
        {
            // Create a list of JSON strings for each process
            List<string> processJsonList = new List<string>();

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "select * from Win32_Process"))
                    {
                        // Read wmi without parallel
                        foreach (ManagementObject reader in searcher.Get())
                        {
                            try
                            {
                                int pid = Convert.ToInt32(reader["ProcessId"]);
                                Process process_info = Process.GetProcessById(pid);

                                // Collect process information
                                string name = reader["Name"].ToString();
                                int cpu_percentage = 0;
                                int parent_id = Convert.ToInt32(reader["ParentProcessId"]);
                                string parent_name = Process.GetProcessById(parent_id).ProcessName;
                                double ram_usage = 0;
                                string user = Helper.Process_User.Get(process_info) + " (" + process_info.SessionId + ")";
                                string created = process_info.StartTime.ToString();
                                string path = process_info.MainModule.FileName;
                                string commandline = reader["CommandLine"].ToString();
                                int handles = 0, threads = 0, read_operations = 0, read_transfer = 0, write_operations = 0, write_transfer = 0;

                                int.TryParse(reader["HandleCount"].ToString(), out handles);
                                int.TryParse(reader["ThreadCount"].ToString(), out threads);
                                int.TryParse(reader["ReadOperationCount"].ToString(), out read_operations);
                                int.TryParse(reader["WriteOperationCount"].ToString(), out write_operations);
                                double.TryParse(reader["ReadTransferCount"].ToString(), out double readTransfer);
                                double.TryParse(reader["WriteTransferCount"].ToString(), out double writeTransfer);
                                read_transfer = (int)(readTransfer / (1024 * 1024));
                                write_transfer = (int)(writeTransfer / (1024 * 1024));

                                // RAM usage in MB
                                var ram_perf_c = new PerformanceCounter("Process", "Working Set - Private", process_info.ProcessName);
                                ram_usage = Math.Round((double)ram_perf_c.RawValue / (1024 * 1024), 2);

                                // CPU usage in %
                                using (ManagementObjectSearcher searcher_perf = new ManagementObjectSearcher("root\\CIMV2", "select * from Win32_PerfFormattedData_PerfProc_Process WHERE IDProcess = '" + pid + "'"))
                                {
                                    foreach (ManagementObject reader_perf in searcher_perf.Get())
                                        cpu_percentage = Convert.ToInt32(reader_perf["PercentProcessorTime"]) / Environment.ProcessorCount;
                                }

                                // Create process object
                                Process_Data processInfo = new Process_Data
                                {
                                    name = name,
                                    pid = pid.ToString(),
                                    parent_name = parent_name,
                                    parent_pid = parent_id.ToString(),
                                    cpu = cpu_percentage.ToString(),
                                    ram = ram_usage.ToString(),
                                    user = user,
                                    created = created,
                                    path = path,
                                    cmd = commandline,
                                    handles = handles.ToString(),
                                    threads = threads.ToString(),
                                    read_operations = read_operations.ToString(),
                                    read_transfer = read_transfer.ToString(),
                                    write_operations = write_operations.ToString(),
                                    write_transfer = write_transfer.ToString()
                                };

                                // Serialize the process object into a JSON string and add it to the list
                                string processJson = JsonSerializer.Serialize(processInfo, new JsonSerializerOptions { WriteIndented = true });
                                processJsonList.Add(processJson);
                            }
                            catch (Exception ex)
                            {
                                Logging.Device_Information("Device_Information.Process_List.Collect", "Failed.", ex.Message);
                            }
                        }

                        // Parallel execution of the loop

                        /*Parallel.ForEach(searcher.Get().OfType<ManagementObject>(), reader =>
                        {
                            try
                            {
                                int pid = Convert.ToInt32(reader["ProcessId"]);
                                Process process_info = Process.GetProcessById(pid);

                                // Collect process information
                                string name = reader["Name"].ToString();
                                int cpu_percentage = 0;
                                int parent_id = Convert.ToInt32(reader["ParentProcessId"]);
                                string parent_name = Process.GetProcessById(parent_id).ProcessName;
                                double ram_usage = 0;
                                string user = Helper.Process_User.Get(process_info) + " (" + process_info.SessionId + ")";
                                string created = process_info.StartTime.ToString();
                                string path = process_info.MainModule.FileName;
                                string commandline = reader["CommandLine"].ToString();
                                int handles = 0, threads = 0, read_operations = 0, read_transfer = 0, write_operations = 0, write_transfer = 0;

                                int.TryParse(reader["HandleCount"].ToString(), out handles);
                                int.TryParse(reader["ThreadCount"].ToString(), out threads);
                                int.TryParse(reader["ReadOperationCount"].ToString(), out read_operations);
                                int.TryParse(reader["WriteOperationCount"].ToString(), out write_operations);
                                double.TryParse(reader["ReadTransferCount"].ToString(), out double readTransfer);
                                double.TryParse(reader["WriteTransferCount"].ToString(), out double writeTransfer);
                                read_transfer = (int)(readTransfer / (1024 * 1024));
                                write_transfer = (int)(writeTransfer / (1024 * 1024));

                                // RAM usage in MB
                                var ram_perf_c = new PerformanceCounter("Process", "Working Set - Private", process_info.ProcessName);
                                ram_usage = Math.Round((double)ram_perf_c.RawValue / (1024 * 1024), 2);

                                // CPU usage in %
                                using (ManagementObjectSearcher searcher_perf = new ManagementObjectSearcher("root\\CIMV2", "select * from Win32_PerfFormattedData_PerfProc_Process WHERE IDProcess = '" + pid + "'"))
                                {
                                    foreach (ManagementObject reader_perf in searcher_perf.Get())
                                        cpu_percentage = Convert.ToInt32(reader_perf["PercentProcessorTime"]) / Environment.ProcessorCount;
                                }

                                // Create process object
                                Process_Data processInfo = new Process_Data
                                {
                                    name = name,
                                    pid = pid.ToString(),
                                    parent_name = parent_name,
                                    parent_pid = parent_id.ToString(),
                                    cpu = cpu_percentage.ToString(),
                                    ram = ram_usage.ToString(),
                                    user = user,
                                    created = created,
                                    path = path,
                                    cmd = commandline,
                                    handles = handles.ToString(),
                                    threads = threads.ToString(),
                                    read_operations = read_operations.ToString(),
                                    read_transfer = read_transfer.ToString(),
                                    write_operations = write_operations.ToString(),
                                    write_transfer = write_transfer.ToString()
                                };

                                // Serialize the process object into a JSON string and add it to the list
                                string processJson = JsonConvert.SerializeObject(processInfo, Formatting.Indented);
                                processJsonList.Add(processJson);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Device_Information("Device_Information.Process_List.Collect", "Failed.", ex.Message);
                            }
                        });
                        */
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Process_List.Collect", "Failed.", ex.ToString());
                    return "[]";
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                // Linux process list
                try
                {
                    int processorCount = Environment.ProcessorCount;

                    string processes_string = Linux.Helper.Bash.Execute_Script("Collect Application", false, "ps -eo pid,ppid,comm,user,pcpu,pmem,etime,cmd --sort=-pcpu -w");

                    Logging.Device_Information("Device_Information.Process_List.Collect", "ps output", processes_string);

                    string[] processes = processes_string.Split('\n');

                    // Die erste Zeile ignorieren (Header)
                    for (int i = 1; i < processes.Length; i++)
                    {
                        string process = processes[i].Trim();
                        if (string.IsNullOrWhiteSpace(process))
                            continue;

                        // Spalten korrekt extrahieren, cmd mit Regex
                        string[] process_info = Regex.Split(process, @"\s+");
                        if (process_info.Length < 8)
                            continue;

                        // Parse PID sicher
                        if (!int.TryParse(process_info[0], out int pid))
                        {
                            Logging.Device_Information("Device_Information.Process_List.Collect", $"Skipping invalid PID: ", process_info[0]);
                            continue;
                        }

                        string parent_pid = process_info[1];
                        string name = process_info[2];
                        string user = process_info[3];
                        string cpu = process_info[4];

                        double cpuUsage = double.Parse(cpu);
                        double cpuUsagePerCore = cpuUsage / processorCount;
                        cpu = Math.Round(cpuUsagePerCore).ToString();

                        string ram = process_info[5];
                        string created = process_info[6];

                        // Der restliche Teil der cmd-Zeile wird hier zusammengefügt
                        string commandline = string.Join(" ", process_info.Skip(7));

                        // Get process path
                        string processPath = Linux.Helper.Bash.Execute_Script("Collect Applications", false, $"readlink -f /proc/{pid}/exe");

                        Process_Data processInfo = new Process_Data
                        {
                            name = name,
                            pid = pid.ToString(),
                            parent_pid = parent_pid,
                            cpu = cpu,
                            ram = ram,
                            user = user,
                            created = created,
                            path = processPath,
                            cmd = commandline
                        };

                        string processJson = JsonSerializer.Serialize(processInfo, new JsonSerializerOptions { WriteIndented = true });
                        processJsonList.Add(processJson);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Process_List.Collect", "Failed.", ex.ToString());
                    return "[]";
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                // macOS process list
                try
                {
                    int processorCount = Environment.ProcessorCount;

                    // macOS-kompatibler ps-Befehl
                    string processes_string = MacOS.Helper.Zsh.Execute_Script("Collect", false, "ps -Ao pid,ppid,comm,user,%cpu,%mem,etime,command -r");

                    Logging.Device_Information("Device_Information.Process_List.Collect", "ps output", processes_string);

                    string[] processes = processes_string.Split('\n');

                    // Die erste Zeile ignorieren (Header)
                    for (int i = 1; i < processes.Length; i++)
                    {
                        string process = processes[i].Trim();
                        if (string.IsNullOrWhiteSpace(process))
                            continue;

                        // Spalten korrekt extrahieren, cmd mit Regex
                        string[] process_info = Regex.Split(process, @"\s+");
                        if (process_info.Length < 8)
                            continue;

                        // Parse PID sicher
                        if (!int.TryParse(process_info[0], out int pid))
                        {
                            Logging.Device_Information("Device_Information.Process_List.Collect", $"Skipping invalid PID: ", process_info[0]);
                            continue;
                        }

                        string parent_pid = process_info[1];
                        string name = process_info[2];
                        string user = process_info[3];
                        string cpu = process_info[4];

                        double cpuUsage = double.Parse(cpu);
                        double cpuUsagePerCore = cpuUsage / processorCount;
                        cpu = Math.Round(cpuUsagePerCore).ToString();

                        string ram = process_info[5];
                        string created = process_info[6];

                        // Der restliche Teil der cmd-Zeile wird hier zusammengefügt
                        string commandline = string.Join(" ", process_info.Skip(7));

                        Process_Data processInfo = new Process_Data
                        {
                            name = name,
                            pid = pid.ToString(),
                            parent_pid = parent_pid,
                            cpu = cpu,
                            ram = ram,
                            user = user,
                            created = created,
                            cmd = commandline
                        };

                        string processJson = JsonSerializer.Serialize(processInfo, new JsonSerializerOptions { WriteIndented = true });
                        processJsonList.Add(processJson);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Process_List.Collect", "Failed.", ex.ToString());
                    return "[]";
                }
            }

            // Currently not using parallel execution because of possible performance issues caused by the wmi process
            try
            {
                // Create and log JSON array
                string processes_json = "[" + string.Join("," + Environment.NewLine, processJsonList) + "]";

                Logging.Device_Information("Device_Information.Process_List.Collect", "Collected the following process information.", processes_json);
                return processes_json;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Process_List.Collect", "Failed.", ex.ToString());
                return "[]";
            }
        }

        public static int Get_CPU_Usage_By_ID(int process_id)
        {
            try
            {
                Logging.Device_Information("Device_Information.Processes.Get_CPU_Usage_By_ID", "process_id", process_id.ToString());

                int cpuUsageNormalizedRounded = 0;

                if (OperatingSystem.IsWindows())
                {
                    // Get process by id
                    Process process = Process.GetProcessById(process_id);

                    string instanceName = Get_Process_Instance_Name(process);
                    if (String.IsNullOrEmpty(instanceName))
                        return 0;

                    PerformanceCounter cpuCounter = new PerformanceCounter("Process", "% Processor Time", instanceName, true);

                    // Da der erste Aufruf oft ungenaue Daten liefert, warten wir kurz und rufen den Wert erneut ab
                    cpuCounter.NextValue();
                    Thread.Sleep(1000);

                    // CPU-Auslastung ermitteln
                    float cpuUsage = cpuCounter.NextValue();
                    int processorCount = Environment.ProcessorCount;
                    float cpuUsageNormalized = cpuUsage / processorCount;

                    // Rundung und Umwandlung in int
                    cpuUsageNormalizedRounded = (int)Math.Round(cpuUsageNormalized);

                    Logging.Device_Information("Device_Information.Processes.Get_CPU_Usage_By_ID", "cpu process usage (id): " + process.Id, cpuUsage + "(%)");
                    Logging.Device_Information("Device_Information.Processes.Get_CPU_Usage_By_ID", "cpu process usage normalized (id): " + process.Id, cpuUsageNormalizedRounded + "(%)");
                }
                else if (OperatingSystem.IsLinux())
                {
                    // Pfad zur Prozessstatistikdatei
                    string statPath = $"/proc/{process_id}/stat";

                    if (!File.Exists(statPath))
                    {
                        throw new Exception($"Process with ID {process_id} does not exist or access is denied.");
                    }

                    // Leseinhalt der Datei
                    string statContent = File.ReadAllText(statPath);
                    string[] statFields = statContent.Split(' ');

                    // Überprüfen, ob genug Felder vorhanden sind
                    if (statFields.Length < 17)
                    {
                        throw new Exception($"Unexpected format in /proc/{process_id}/stat.");
                    }

                    // Werte aus der Datei extrahieren
                    long utime = long.Parse(statFields[13]); // Nutzerzeit in "Ticks"
                    long stime = long.Parse(statFields[14]); // Systemzeit in "Ticks"
                    long starttime = long.Parse(statFields[21]); // Startzeit in "Ticks"

                    // Gesamte Prozess-CPU-Zeit in Ticks
                    long totalTime = utime + stime;

                    // Aktuelle Systemuptime abrufen
                    string uptimeContent = File.ReadAllText("/proc/uptime");
                    double systemUptime = double.Parse(uptimeContent.Split(' ')[0], System.Globalization.CultureInfo.InvariantCulture);

                    // System-Ticks pro Sekunde (Hertz/Jiffies)
                    const long hertz = 100; // Standardwert für Linux-Systeme

                    // Anzahl der CPU-Kerne abrufen
                    int numCores = Environment.ProcessorCount;

                    // Berechnung der CPU-Auslastung
                    double elapsedTimeInSeconds = systemUptime - (starttime / (double)hertz);
                    if (elapsedTimeInSeconds <= 0)
                    {
                        return 0; // Verhindere Division durch 0 oder negative Zeit
                    }

                    double cpuUsage = (totalTime / (double)hertz) / elapsedTimeInSeconds * 100;

                    // CPU-Auslastung pro Kern berücksichtigen
                    double cpuUsagePerCore = cpuUsage / numCores;

                    // Runden und Rückgabe
                    cpuUsageNormalizedRounded = (int)Math.Round(cpuUsagePerCore);

                    Logging.Device_Information("Device_Information.Processes.Get_CPU_Usage_By_ID", "CPU-Prozessnutzung (ID): " + process_id, cpuUsageNormalizedRounded + "(%)");
                }
                else if (OperatingSystem.IsMacOS())
                {
                    try
                    {
                        // Execute `ps` command to get process information
                        string command = $"ps -p {process_id} -o %cpu";
                        string output = MacOS.Helper.Zsh.Execute_Script("Get_CPU_Usage_By_ID", false, command);

                        // Parse the output
                        var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        if (lines.Length < 2)
                        {
                            throw new Exception($"Process with ID {process_id} does not exist or CPU usage information is not available.");
                        }

                        // The CPU usage is typically on the second line
                        string cpuUsageString = lines[1].Trim();
                        if (double.TryParse(cpuUsageString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double cpuUsage))
                        {
                            // Normalize CPU usage by number of cores
                            int numCores = Environment.ProcessorCount;
                            double cpuUsageNormalized = cpuUsage / numCores;

                            // Round and return
                            cpuUsageNormalizedRounded = (int)Math.Round(cpuUsageNormalized);
                            Logging.Device_Information("Device_Information.Processes.Get_CPU_Usage_By_ID", "CPU process usage (id): " + process_id, cpuUsageNormalizedRounded + "(%)");
                        }
                        else
                        {
                            throw new Exception($"Unable to parse CPU usage for process ID {process_id}.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Device_Information.Processes.Get_CPU_Usage_By_ID", "Failed to get CPU usage.", ex.Message);
                        cpuUsageNormalizedRounded = 0; // Default to 0 if an error occurs
                    }
                }

                return cpuUsageNormalizedRounded;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Processes.Get_CPU_Usage_By_Name", "Failed.", ex.ToString());

                return 0;
            }
        }

        public static int Get_RAM_Usage_By_ID(int process_id, bool percentage) // percentage = %, otherwise MB
        {
            try
            {
                Logging.Device_Information("Device_Information.Processes.Get_RAM_Usage_By_ID", "process_id", process_id.ToString());

                if (OperatingSystem.IsWindows())
                {
                    Process process = Process.GetProcessById(process_id);

                    // Determine RAM utilisation in bytes
                    long memoryUsageInBytes = process.WorkingSet64;

                    // Convert RAM usage to megabytes
                    double memoryUsageInMB = memoryUsageInBytes / (1024.0 * 1024.0);

                    if (!percentage) // MB
                    {
                        Logging.Device_Information("Device_Information.Processes.Get_RAM_Usage_By_ID", "process ram usage in MB (id): " + process.Id, memoryUsageInMB + "(MB)");

                        return Convert.ToInt32(Math.Round(memoryUsageInMB));
                    }
                    else // percentage
                    {
                        int totalMemory = Convert.ToInt32(Windows.Helper.WMI.Search("root\\cimv2", "SELECT * FROM Win32_OperatingSystem", "TotalVisibleMemorySize"));
                        int availableMemory = Convert.ToInt32(Windows.Helper.WMI.Search("root\\cimv2", "SELECT * FROM Win32_OperatingSystem", "FreePhysicalMemory"));

                        // Conversion to MB
                        double totalMemoryInMB = totalMemory / 1024.0;
                        double availableMemoryInMB = availableMemory / 1024.0;

                        int ramUsagePercentage = Convert.ToInt32((memoryUsageInMB / totalMemoryInMB) * 100);

                        Logging.Device_Information("Device_Information.Processes.Get_RAM_Usage_By_ID", "process ram usage in % (id): " + process.Id, ramUsagePercentage + "(%)");

                        return ramUsagePercentage;
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    // Path to the process's status file
                    string procStatusPath = $"/proc/{process_id}/status";

                    if (!File.Exists(procStatusPath))
                    {
                        throw new Exception($"Process with ID {process_id} does not exist or access is denied.");
                    }

                    // Read the status file
                    string[] lines = File.ReadAllLines(procStatusPath);

                    // Extract memory usage information
                    long memoryUsageInKB = 0;
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("VmRSS:", StringComparison.Ordinal))
                        {
                            // Parse the line: e.g., "VmRSS:    123456 kB"
                            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2 && long.TryParse(parts[1], out long value))
                            {
                                memoryUsageInKB = value; // Value is in kB
                                break;
                            }
                        }
                    }

                    if (memoryUsageInKB == 0)
                    {
                        throw new Exception($"Unable to determine memory usage for process ID {process_id}.");
                    }

                    // Convert RAM usage to megabytes
                    double memoryUsageInMB = memoryUsageInKB / 1024.0;

                    if (!percentage) // MB
                    {
                        Logging.Device_Information("Device_Information.Processes.Get_RAM_Usage_By_ID", "process ram usage in MB (id): " + process_id, memoryUsageInMB + " (MB)");
                        return Convert.ToInt32(Math.Round(memoryUsageInMB));
                    }
                    else // percentage
                    {
                        // Get total memory from /proc/meminfo
                        string memInfoPath = "/proc/meminfo";
                        if (!File.Exists(memInfoPath))
                        {
                            throw new Exception("Unable to read /proc/meminfo to determine total memory.");
                        }

                        long totalMemoryInKB = 0;
                        foreach (string line in File.ReadLines(memInfoPath))
                        {
                            if (line.StartsWith("MemTotal:", StringComparison.Ordinal))
                            {
                                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2 && long.TryParse(parts[1], out long value))
                                {
                                    totalMemoryInKB = value; // Value is in kB
                                    break;
                                }
                            }
                        }

                        if (totalMemoryInKB == 0)
                        {
                            throw new Exception("Unable to determine total memory from /proc/meminfo.");
                        }

                        double totalMemoryInMB = totalMemoryInKB / 1024.0;

                        // Calculate RAM usage as a percentage
                        int ramUsagePercentage = Convert.ToInt32((memoryUsageInMB / totalMemoryInMB) * 100);

                        Logging.Device_Information("Device_Information.Processes.Get_RAM_Usage_By_ID", "process ram usage in % (id): " + process_id, ramUsagePercentage + " (%)");

                        return ramUsagePercentage;
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    try
                    {
                        // Execute `ps` command to get memory usage percentage
                        string output = MacOS.Helper.Zsh.Execute_Script("Get_RAM_Usage_By_ID", false, $"ps -p {process_id} -o %mem");

                        // Parse the output
                        var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (lines.Length < 2)
                        {
                            throw new Exception($"Process with ID {process_id} does not exist or memory usage information is not available.");
                        }

                        // The memory usage is typically on the second line
                        string memoryUsageString = lines[1].Trim();

                        if (!double.TryParse(memoryUsageString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double memoryUsagePercentage))
                        {
                            throw new Exception($"Unable to parse memory usage percentage for process ID {process_id}.");
                        }

                        if (!percentage) // Return memory usage in MB
                        {
                            // Get total RAM in bytes
                            string totalRamOutput = MacOS.Helper.Zsh.Execute_Script("Get_RAM_Usage_By_ID", false, "sysctl hw.memsize");

                            // Parse total RAM size
                            var totalRamParts = totalRamOutput.Split(':', StringSplitOptions.RemoveEmptyEntries);
                            if (totalRamParts.Length < 2 || !long.TryParse(totalRamParts[1].Trim(), out long totalRamBytes))
                            {
                                throw new Exception("Failed to retrieve total RAM size.");
                            }

                            double totalRamMB = totalRamBytes / (1024.0 * 1024.0); // Convert bytes to MB

                            // Calculate memory usage in MB
                            double memoryUsageInMB = (memoryUsagePercentage / 100.0) * totalRamMB;

                            Logging.Device_Information("Device_Information.Processes.Get_RAM_Usage_By_ID", $"Process RAM usage in MB (id: {process_id})", $"{Math.Round(memoryUsageInMB)} MB");

                            return Convert.ToInt32(Math.Round(memoryUsageInMB));
                        }
                        else // Return memory usage as a percentage
                        {
                            Logging.Debug("Device_Information.Processes.Get_RAM_Usage_By_ID", "Memory usage percentage", memoryUsagePercentage.ToString());
                            return Convert.ToInt32(memoryUsagePercentage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Device_Information.Processes.Get_RAM_Usage_By_ID", "Failed to get RAM usage.", ex.ToString());
                        return 0; // Return 0 on failure
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Processes.Get_RAM_Usage_By_ID", "Failed.", ex.ToString());

                return 0;
            }
        }

        private static string Get_Process_Instance_Name(Process process)
        {
            try
            {
                PerformanceCounterCategory category = new PerformanceCounterCategory("Process");
                string[] instanceNames = category.GetInstanceNames().Where(name => name.StartsWith(process.ProcessName)).ToArray();

                foreach (string instanceName in instanceNames)
                {
                    using (PerformanceCounter counter = new PerformanceCounter("Process", "ID Process", instanceName, true))
                    {
                        if ((int)counter.RawValue == process.Id)
                            return instanceName;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Processes.Get_Process_Instance_Name", "Failed.", ex.ToString());
                return "";
            }
        }

        // Code for getting the owner of a process on Windows and Linux
        /// <summary>
        /// Opens the access token associated with a process.
        /// </summary>
        /// <param name="ProcessHandle">A handle to the process whose access token is opened.</param>
        /// <param name="DesiredAccess">Specifies an access mask that specifies the requested types of access to the access token.</param>
        /// <param name="TokenHandle">A pointer to a handle that identifies the newly opened access token when the function returns.</param>
        /// <returns>True if the function succeeds; otherwise, false.</returns>
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        /// <summary>
        /// Closes an open object handle.
        /// </summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        /// <returns>True if the function succeeds; otherwise, false.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Gets the owner of the specified process.
        /// </summary>
        /// <param name="process">The process whose owner is to be determined.</param>
        /// <returns>The owner of the process.</returns>
        /// <exception cref="PlatformNotSupportedException">Thrown when the operating system is not supported.</exception>
        public static string Process_Owner(Process process)
        {
            if (OperatingSystem.IsWindows())
            {
                return GetWindowsProcessOwner(process);
            }
            else if (OperatingSystem.IsLinux())
            {
                return GetLinuxProcessOwner(process);
            }
            else if (OperatingSystem.IsMacOS())
            {
                return GetMacOSProcessOwner(process);
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported operating system.");
            }
        }

        /// <summary>
        /// Gets the owner of the specified process on Windows.
        /// </summary>
        /// <param name="process">The process whose owner is to be determined.</param>
        /// <returns>The owner of the process.</returns>
        private static string GetWindowsProcessOwner(Process process)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                if (OpenProcessToken(process.Handle, 8, out processHandle))
                {
                    WindowsIdentity wi = new WindowsIdentity(processHandle);
                    string user = wi.Name;
                    return user.Contains(@"\") ? user.Substring(user.IndexOf(@"\") + 1) : user;
                }
            }
            catch
            {
                // Log or handle exception as needed
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the owner of the specified process on Linux.
        /// </summary>
        /// <param name="process">The process whose owner is to be determined.</param>
        /// <returns>The owner of the process.</returns>
        private static string GetLinuxProcessOwner(Process process)
        {
            try
            {
                // Path to the process status file in /proc
                string statusPath = $"/proc/{process.Id}/status";

                // Read the file and search for the "Uid" line
                foreach (var line in File.ReadLines(statusPath))
                {
                    if (line.StartsWith("Uid:"))
                    {
                        string[] parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1)
                        {
                            string uid = parts[1];

                            // Convert UID to username using getpwuid
                            string username = GetUsernameFromUid(uid);
                            return username;
                        }
                    }
                }
            }
            catch
            {
                // Log or handle exception as needed
            }

            return null;
        }

        private static string GetMacOSProcessOwner(Process process)
        {
            try
            {
                // Execute `ps` command to get process information
                string command = $"ps -p {process.Id} -o user";
                string output = MacOS.Helper.Zsh.Execute_Script("Get_Process_Owner", false, command);
                // Parse the output
                var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 2)
                {
                    throw new Exception($"Process with ID {process.Id} does not exist or owner information is not available.");
                }
                // The owner is typically on the second line
                string owner = lines[1].Trim();
                return owner;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Processes.Get_Process_Owner", "Failed to get process owner.", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Converts a UID to a username on Linux.
        /// </summary>
        /// <param name="uid">The UID to be converted.</param>
        /// <returns>The username corresponding to the UID.</returns>
        private static string GetUsernameFromUid(string uid)
        {
            try
            {
                // Use "getent passwd" to fetch username from UID
                string result = Linux.Helper.Bash.Execute_Script("GetUsernameFromUid", false, $"getent passwd {uid}");
                if (!string.IsNullOrEmpty(result))
                {
                    string[] parts = result.Split(':');
                    if (parts.Length > 0)
                    {
                        return parts[0]; // Username is the first field
                    }
                }
            }
            catch
            {
                // Log or handle exception as needed
            }

            return null;
        }
    }
}
