using Newtonsoft.Json;
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

namespace NetLock_RMM_Comm_Agent_Windows.Device_Information
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
            // Currently not using parallel execution because of possible performance issues caused by the wmi process
            try
            {
                // Create a list of JSON strings for each process
                List<string> processJsonList = new List<string>();

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
                            string processJson = JsonConvert.SerializeObject(processInfo, Formatting.Indented);
                            processJsonList.Add(processJson);
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Device_Information("Device_Information.Process_List.Collect", "Failed.", ex.Message);
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

                // Create and log JSON array
                string processes_json = "[" + string.Join("," + Environment.NewLine, processJsonList) + "]";

                Logging.Handler.Device_Information("Device_Information.Process_List.Collect", "Collected the following process information.", processes_json);
                return processes_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Process_List.Collect", "Failed.", ex.ToString());
                return "[]";
            }   
        }

        public static int Get_CPU_Usage_By_ID(int process_id)
        {
            try
            {
                Logging.Handler.Device_Information("Device_Information.Processes.Get_CPU_Usage_By_ID", "process_id", process_id.ToString());

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
                int cpuUsageNormalizedRounded = (int)Math.Round(cpuUsageNormalized);

                Logging.Handler.Device_Information("Device_Information.Processes.Get_CPU_Usage_By_ID", "cpu process usage (id): " + process.Id, cpuUsage + "(%)");
                Logging.Handler.Device_Information("Device_Information.Processes.Get_CPU_Usage_By_ID", "cpu process usage normalized (id): " + process.Id, cpuUsageNormalizedRounded + "(%)");

                return cpuUsageNormalizedRounded;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Processes.Get_CPU_Usage_By_Name", "Failed.", ex.ToString());

                return 0;
            }
        }

        public static int Get_RAM_Usage_By_ID(int process_id, bool percentage) // percentage = %, otherwise MB
        {
            try
            {
                Logging.Handler.Device_Information("Device_Information.Processes.Get_RAM_Usage_By_ID", "process_id", process_id.ToString());

                Process process = Process.GetProcessById(process_id);

                // Determine RAM utilisation in bytes
                long memoryUsageInBytes = process.WorkingSet64;

                // Convert RAM usage to megabytes
                double memoryUsageInMB = memoryUsageInBytes / (1024.0 * 1024.0);

                if (!percentage) // MB
                {
                    Logging.Handler.Device_Information("Device_Information.Processes.Get_RAM_Usage_By_ID", "process ram usage in MB (id): " + process.Id, memoryUsageInMB + "(MB)");

                    return Convert.ToInt32(Math.Round(memoryUsageInMB));
                }
                else // percentage
                {
                    int totalMemory = Convert.ToInt32(Helper.WMI.Search("root\\cimv2", "SELECT * FROM Win32_OperatingSystem", "TotalVisibleMemorySize"));
                    int availableMemory = Convert.ToInt32(Helper.WMI.Search("root\\cimv2", "SELECT * FROM Win32_OperatingSystem", "FreePhysicalMemory"));

                    // Umrechnung in MB
                    double totalMemoryInMB = totalMemory / 1024.0;
                    double availableMemoryInMB = availableMemory / 1024.0;

                    int ramUsagePercentage = Convert.ToInt32((memoryUsageInMB / totalMemoryInMB) * 100);

                    Logging.Handler.Device_Information("Device_Information.Processes.Get_RAM_Usage_By_ID", "process ram usage in % (id): " + process.Id, ramUsagePercentage + "(%)");

                    return ramUsagePercentage;
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Processes.Get_RAM_Usage_By_ID", "Failed.", ex.ToString());

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
                Logging.Handler.Error("Device_Information.Processes.Get_Process_Instance_Name", "Failed.", ex.ToString());
                return "";
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);
        public static string Process_Owner(Process process)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                OpenProcessToken(process.Handle, 8, out processHandle);
                WindowsIdentity wi = new WindowsIdentity(processHandle);
                string user = wi.Name;
                return user.Contains(@"\") ? user.Substring(user.IndexOf(@"\") + 1) : user;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (processHandle != IntPtr.Zero)
                {
                    CloseHandle(processHandle);
                }
            }
        }
    }
}
