using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Global.Online_Mode.Handler;
using Global.Helper;
using System.Text.Json.Serialization;
using System.Text.Json;
using NetLock_RMM_Agent_Comm;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Configuration.Internal;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Metrics;
using Microsoft.Data.Sqlite;

namespace Global.Device_Information
{
    internal class Hardware
    {
        public static string CPU_Name()
        {
            if (OperatingSystem.IsWindows())
            {
                return Windows.Helper.WMI.Search("root\\CIMV2", "SELECT Name FROM Win32_Processor", "Name");
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    string cpu_name = string.Empty;
                    // Read the CPU information from the /proc/cpuinfo file
                    string cpuInfoFile = "/proc/cpuinfo";
                    if (File.Exists(cpuInfoFile))
                    {
                        // Read the file contents
                        string[] cpuInfoLines = File.ReadAllLines(cpuInfoFile);
                        // Search for the "model name" line
                        foreach (string line in cpuInfoLines)
                        {
                            if (line.StartsWith("model name"))
                            {
                                // Extract the CPU name
                                cpu_name = line.Split(':')[1].Trim();
                                break;
                            }
                        }
                    }
                    return cpu_name;
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.CPU_Name", "General error.", ex.ToString());
                    return "N/A";
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    string cpu_name = string.Empty;
                    // Read the CPU information from the system_profiler command
                    string systemProfilerOutput = MacOS.Helper.Zsh.Execute_Script("CPU_Name", false, "system_profiler SPHardwareDataType");
                    if (!string.IsNullOrEmpty(systemProfilerOutput))
                    {
                        // Search for the "Processor Name" line
                        foreach (string line in systemProfilerOutput.Split('\n'))
                        {
                            if (line.Contains("Processor Name"))
                            {
                                // Extract the CPU name
                                cpu_name = line.Split(':')[1].Trim();
                                break;
                            }
                        }
                    }
                    return cpu_name;
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.CPU_Name", "General error.", ex.ToString());
                    return "N/A";
                }
            }
            else
            {
                return "N/A";
            }
        }

        public static string CPU_Information()
        {
            string cpu_information_json = string.Empty;

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            HashSet<string> uniqueSockets = new HashSet<string>();

                            try
                            {
                                if (obj != null && obj["SocketDesignation"] != null)
                                {
                                    string socket_designation = obj["SocketDesignation"].ToString();

                                    // Check if the socket_designation is not empty and add to uniqueSockets
                                    if (!string.IsNullOrEmpty(socket_designation))
                                    {
                                        uniqueSockets.Add(socket_designation);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.Error("Device_Information.Hardware.CPU_Information", "CPU_Information", "An error occurred while querying for WMI data: " + ex.ToString());
                            }

                            int numberOfSockets = uniqueSockets.Count;

                            try
                            {
                                CPU_Information cpuInfo = new CPU_Information
                                {
                                    name = obj["Name"]?.ToString() ?? "N/A",
                                    socket_designation = obj["SocketDesignation"]?.ToString() ?? "N/A",
                                    processor_id = obj["ProcessorId"]?.ToString() ?? "N/A",
                                    revision = obj["Revision"]?.ToString() ?? "N/A",
                                    usage = obj["LoadPercentage"]?.ToString() ?? "0", // Default to "0" if usage isn't available
                                    voltage = obj["CurrentVoltage"]?.ToString() ?? "N/A",
                                    currentclockspeed = obj["CurrentClockSpeed"]?.ToString() ?? "N/A",

                                    // Use fallback if there are any issues retrieving process information
                                    processes = GetSafeProcessCount(),
                                    threads = GetSafeThreadCount(),
                                    handles = GetSafeHandleCount(),

                                    maxclockspeed = obj["MaxClockSpeed"]?.ToString() ?? "N/A",
                                    sockets = numberOfSockets.ToString(),
                                    cores = obj["NumberOfCores"]?.ToString() ?? "N/A",
                                    logical_processors = obj["NumberOfLogicalProcessors"]?.ToString() ?? "N/A",
                                    virtualization = obj["VirtualizationFirmwareEnabled"]?.ToString() ?? "N/A",
                                    //l1_cache = SafeCacheSize(obj["L1CacheSize"]),
                                    l1_cache = "N/A",
                                    l2_cache = SafeCacheSize(obj["L2CacheSize"]),
                                    l3_cache = SafeCacheSize(obj["L3CacheSize"])
                                };

                                // Serialize the process object into a JSON string and add it to the list

                                cpu_information_json = JsonSerializer.Serialize(cpuInfo, new JsonSerializerOptions { WriteIndented = true });
                                Logging.Device_Information("Device_Information.Hardware.CPU_Information", "cpu_information_json", cpu_information_json);
                            }
                            catch (Exception ex)
                            {
                                Logging.Error("Device_Information.Hardware.CPU_Information", "CPU_Information", "An error occurred while querying for WMI data: " + ex.ToString());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.CPU_Information", "CPU_Information", "An error occurred while querying for WMI data: " + ex.ToString());
                    return "{}";
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    string cpu_name = string.Empty;
                    string cpu_cores = string.Empty;
                    string cpu_threads = string.Empty;
                    string cpu_usage = string.Empty;
                    string cpu_speed = string.Empty;
                    string cpu_cache = string.Empty;
                    // Read the CPU information from the /proc/cpuinfo file
                    string cpuInfoFile = "/proc/cpuinfo";
                    if (File.Exists(cpuInfoFile))
                    {
                        // Read the file contents
                        string[] cpuInfoLines = File.ReadAllLines(cpuInfoFile);
                        // Search for the "model name" line
                        foreach (string line in cpuInfoLines)
                        {
                            if (line.StartsWith("model name"))
                            {
                                // Extract the CPU name
                                cpu_name = line.Split(':')[1].Trim();
                            }
                            else if (line.StartsWith("cpu cores"))
                            {
                                // Extract the CPU cores
                                cpu_cores = line.Split(':')[1].Trim();
                            }
                            else if (line.StartsWith("siblings"))
                            {
                                // Extract the CPU threads
                                cpu_threads = line.Split(':')[1].Trim();
                            }
                            else if (line.StartsWith("cpu MHz"))
                            {
                                // Extract the CPU speed
                                cpu_speed = line.Split(':')[1].Trim();

                                // Wandle den extrahierten Wert in eine Gleitkommazahl um
                                if (double.TryParse(cpu_speed, out double cpuSpeed))
                                {
                                    double roundedCpuSpeed = Math.Round(cpuSpeed);

                                    cpu_speed = roundedCpuSpeed.ToString();
                                }
                            }
                            else if (line.StartsWith("cache size"))
                            {
                                // Extract the CPU cache size
                                cpu_cache = line.Split(':')[1].Trim();
                            }
                        }
                    }
                    // Get CPU usage
                    cpu_usage = CPU_Usage().ToString();
                    // Create JSON
                    CPU_Information cpuInfo = new CPU_Information
                    {
                        name = cpu_name,
                        socket_designation = "N/A",
                        processor_id = "N/A",
                        revision = "N/A",
                        usage = cpu_usage,
                        voltage = "N/A",
                        currentclockspeed = cpu_speed,
                        processes = GetSafeProcessCount(),
                        threads = GetSafeThreadCount(),
                        handles = GetSafeHandleCount(),
                        maxclockspeed = "N/A",
                        sockets = "N/A",
                        cores = cpu_cores,
                        logical_processors = cpu_threads,
                        virtualization = "N/A",
                        l1_cache = "N/A",
                        l2_cache = "N/A",
                        l3_cache = cpu_cache
                    };

                    cpu_information_json = JsonSerializer.Serialize(cpuInfo, new JsonSerializerOptions { WriteIndented = true });

                    // Create and log JSON array
                    Logging.Device_Information("Device_Information.Hardware.CPU_Information", "cpu_information_json", cpu_information_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.CPU_Information", "CPU_Information", "An error occurred while reading /proc/cpuinfo: " + ex.ToString());
                    return "{}";
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    string cpu_name = string.Empty;
                    string cpu_cores = string.Empty;
                    string cpu_threads = string.Empty;
                    string cpu_usage = string.Empty;
                    string cpu_speed = string.Empty;
                    string cpu_cache = string.Empty;

                    // CPU Name
                    cpu_name = CPU_Name();

                    // CPU Cores and Threads
                    cpu_cores = MacOS.Helper.Zsh.Execute_Script("CPU_Information", false, "sysctl -n hw.physicalcpu").Trim();
                    cpu_threads = MacOS.Helper.Zsh.Execute_Script("CPU_Information", false, "sysctl -n hw.logicalcpu").Trim();

                    // CPU Speed
                    string cpu_speed_hz = MacOS.Helper.Zsh.Execute_Script("CPU_Information", false, "sysctl -n hw.cpufrequency").Trim();
                    if (long.TryParse(cpu_speed_hz, out long cpuSpeedHz))
                    {
                        double cpuSpeedGHz = cpuSpeedHz / 1_000_000_000.0; // Convert to GHz
                        cpu_speed = Math.Round(cpuSpeedGHz, 2).ToString() + " GHz";
                    }

                    // CPU Cache Size (L3)
                    cpu_cache = MacOS.Helper.Zsh.Execute_Script("CPU_Information", false, "sysctl -n hw.l3cachesize").Trim();
                    if (long.TryParse(cpu_cache, out long cacheBytes))
                    {
                        double cacheMB = cacheBytes / 1_048_576.0; // Convert to MB
                        cpu_cache = Math.Round(cacheMB, 2).ToString() + " MB";
                    }

                    // Get CPU Usage
                    cpu_usage = CPU_Usage().ToString();

                    // Create JSON
                    CPU_Information cpuInfo = new CPU_Information
                    {
                        name = cpu_name,
                        socket_designation = "N/A",
                        processor_id = "N/A",
                        revision = "N/A",
                        usage = cpu_usage,
                        voltage = "N/A",
                        currentclockspeed = cpu_speed,
                        processes = GetSafeProcessCount(),
                        threads = GetSafeThreadCount(),
                        handles = GetSafeHandleCount(),
                        maxclockspeed = "N/A",
                        sockets = "N/A",
                        cores = cpu_cores,
                        logical_processors = cpu_threads,
                        virtualization = "N/A",
                        l1_cache = "N/A",
                        l2_cache = "N/A",
                        l3_cache = cpu_cache
                    };

                    cpu_information_json = JsonSerializer.Serialize(cpuInfo, new JsonSerializerOptions { WriteIndented = true });

                    // Create and log JSON array
                    Logging.Device_Information("Device_Information.Hardware.CPU_Information", "cpu_information_json", cpu_information_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.CPU_Information", "CPU_Information", "An error occurred while retrieving CPU information on macOS: " + ex.ToString());
                    return "{}";
                }
            }

            return cpu_information_json;
        }

        // Helper function to safely convert cache size
        private static string SafeCacheSize(object cacheSize)
        {
            try
            {
                if (cacheSize != null)
                {
                    return Math.Round(Convert.ToDouble(cacheSize) / 1024).ToString(); // Convert to MB
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.SafeCacheSize", "An error occurred while converting cache size: ", ex.ToString());
            }
            return "N/A"; // Fallback if the value is not available or conversion fails
        }

        // Helper functions to retrieve process information safely
        private static string GetSafeProcessCount()
        {
            try
            {
                return Process.GetProcesses().Length.ToString();
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.GetSafeProcessCount", "An error occurred while retrieving process count: ", ex.ToString());
            }
            return "0"; // Fallback
        }

        private static string GetSafeThreadCount()
        {
            try
            {
                return Process.GetCurrentProcess().Threads.Count.ToString();
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.GetSafeThreadCount", "An error occurred while retrieving thread count: ", ex.ToString());
            }
            return "0"; // Fallback
        }

        private static string GetSafeHandleCount()
        {
            try
            {
                return Process.GetCurrentProcess().HandleCount.ToString();
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.GetSafeHandleCount", "An error occurred while retrieving handle count: ", ex.ToString());
            }
            return "0"; // Fallback
        }

        public static int CPU_Usage()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // Create a new PerformanceCounter instance
                    using (PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                    {
                        // Initialize and discard the first measurement
                        cpuCounter.NextValue();
                        Thread.Sleep(1000);

                        // Get the actual CPU usage
                        float cpuUsage = cpuCounter.NextValue();

                        Logging.Device_Information("Device_Information.Hardware.CPU_Utilization", "Current CPU Usage (%)", cpuUsage.ToString("F2"));

                        return Convert.ToInt32(Math.Round(cpuUsage));
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.CPU_Utilization", "General error.", ex.ToString());
                    return 0;
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    // Function to read CPU stats from /proc/stat
                    static (long idle, long total) ReadCpuStats()
                    {
                        string[] cpuStats = File.ReadLines("/proc/stat")
                                                .First(line => line.StartsWith("cpu "))
                                                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        long user = long.Parse(cpuStats[1]);
                        long nice = long.Parse(cpuStats[2]);
                        long system = long.Parse(cpuStats[3]);
                        long idle = long.Parse(cpuStats[4]);
                        long iowait = long.Parse(cpuStats[5]);
                        long irq = long.Parse(cpuStats[6]);
                        long softirq = long.Parse(cpuStats[7]);
                        long steal = cpuStats.Length > 8 ? long.Parse(cpuStats[8]) : 0;

                        long total = user + nice + system + idle + iowait + irq + softirq + steal;

                        return (idle, total);
                    }

                    // Read initial CPU stats
                    var (idle1, total1) = ReadCpuStats();

                    // Wait for a short interval
                    Thread.Sleep(1000);

                    // Read CPU stats again
                    var (idle2, total2) = ReadCpuStats();

                    // Calculate CPU usage percentage
                    long idleDiff = idle2 - idle1;
                    long totalDiff = total2 - total1;
                    float cpuUsage = 100f * (1 - (float)idleDiff / totalDiff);

                    // Log and return the CPU usage
                    Logging.Device_Information("Device_Information.Hardware.CPU_Utilization", "Current CPU Usage (%)", cpuUsage.ToString("F2"));

                    return Convert.ToInt32(Math.Round(cpuUsage));
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.CPU_Utilization", "General error.", ex.ToString());
                    return 0;
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    // Execute the 'top' command to fetch CPU and memory usage
                    string topOutput = MacOS.Helper.Zsh.Execute_Script("cpu usage", false, "top -l 1 | grep -E \"^CPU|^Phys\"");

                    if (!string.IsNullOrWhiteSpace(topOutput))
                    {
                        // Split output into lines
                        string[] lines = topOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                        foreach (string line in lines)
                        {
                            if (line.StartsWith("CPU usage:"))
                            {
                                // Example line: "CPU usage: 62.2% user, 20.25% sys, 17.72% idle"
                                string[] cpuParts = line.Replace("CPU usage:", "").Split(',');

                                foreach (string part in cpuParts)
                                {
                                    if (part.Contains("user"))
                                    {
                                        // Extract and parse the "user" CPU usage
                                        string userUsage = part.Trim().Split('%')[0];
                                        if (double.TryParse(userUsage, out double userCpuUsage))
                                        {
                                            return Convert.ToInt32(Math.Round(userCpuUsage));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // If parsing fails, return 0
                    return 0;
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.CPU_Utilization", "Error while fetching CPU usage on macOS.", ex.ToString());
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        // needs rework some day to support gathering of multiple RAM sticks
        public static string RAM_Information()
        {
            string ram_information_json = string.Empty;

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    string name = string.Empty;
                    string available = string.Empty;
                    string assured = $"{((double)Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024 * 1024)):N1} / {((double)new PerformanceCounter("Memory", "Available MBytes").NextValue() / 1024):N1}"; //data is not correct. Either my knowledge is wrong here, or the calculations. Doesnt represent the numbers in task manager
                    string cache = string.Empty;
                    string outsourced_pool = string.Empty;
                    string not_outsourced_pool = string.Empty;
                    string speed = string.Empty;
                    string slots = new ManagementObjectSearcher("SELECT MemoryDevices FROM Win32_PhysicalMemoryArray").Get().Cast<ManagementBaseObject>().Select(obj => obj["MemoryDevices"].ToString()).FirstOrDefault();
                    int slots_used = 0;
                    string form_factor = string.Empty;
                    string hardware_reserved = string.Empty;

                    try
                    {
                        // Query for memory performance data
                        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PerfRawData_PerfOS_Memory"))
                        {
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                if (obj != null)
                                {
                                    cache = Math.Floor(Convert.ToDouble(obj["AvailableBytes"] ?? 0) / (1024 * 1024)).ToString(); // Placeholder for cache size
                                    outsourced_pool = Math.Floor(Convert.ToDouble(obj["PoolPagedBytes"] ?? 0) / (1024 * 1024)).ToString();
                                    not_outsourced_pool = Math.Floor(Convert.ToDouble(obj["PoolNonpagedBytes"] ?? 0) / (1024 * 1024)).ToString();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Device_Information.Hardware.RAM_Information", "RAM_Information", "An error occurred while querying for WMI data: " + ex.ToString());
                    }

                    try
                    {
                        // Query for OS memory info
                        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem"))
                        {
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                if (obj != null)
                                {
                                    ulong totalMemory = Convert.ToUInt64(obj["TotalVisibleMemorySize"] ?? 0); // Total memory in KB
                                    ulong freeMemory = Convert.ToUInt64(obj["FreePhysicalMemory"] ?? 0); // Free memory in KB
                                    ulong hardwareReservedMemory = totalMemory - freeMemory; // Reserved memory

                                    hardware_reserved = Math.Round(hardwareReservedMemory / 1024d).ToString(); // Convert to MB
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Device_Information.Hardware.RAM_Information", "RAM_Information", "An error occurred while querying for WMI data: " + ex.ToString());
                    }

                    try
                    {
                        // Query for physical memory info
                        using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PhysicalMemory"))
                        {
                            foreach (ManagementObject obj in searcher.Get())
                            {
                                if (obj != null)
                                {
                                    slots_used++;

                                    name = obj["Name"]?.ToString() ?? "Unknown"; // Check if Name exists
                                    available = (Convert.ToDouble(obj["Capacity"] ?? 0) / (1024 * 1024)).ToString(); // Convert Capacity to MB
                                    speed = obj["Speed"]?.ToString() ?? "Unknown"; // Check if Speed exists
                                    form_factor = obj["DeviceLocator"]?.ToString() ?? "Unknown"; // Check if DeviceLocator exists
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Device_Information.Hardware.RAM_Information", "RAM_Information", "An error occurred while querying for WMI data: " + ex.ToString());
                    }

                    // Create JSON
                    RAM_Information ramInfo = new RAM_Information
                    {
                        name = name,
                        available = available,
                        assured = assured,
                        cache = cache,
                        outsourced_pool = outsourced_pool,
                        not_outsourced_pool = not_outsourced_pool,
                        speed = speed,
                        slots = slots,
                        slots_used = slots_used.ToString(),
                        form_factor = form_factor,
                        hardware_reserved = hardware_reserved,
                    };

                    ram_information_json = JsonSerializer.Serialize(ramInfo, new JsonSerializerOptions { WriteIndented = true });

                    // Create and log JSON array
                    Logging.Device_Information("Device_Information.Hardware.RAM_Information", "Collected the following process information.", ram_information_json);

                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.RAM_Information", "RAM_Information", "An error occurred while querying for WMI data: " + ex.ToString());
                    return "{}";
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    string name = "N/A";
                    string available = "N/A";
                    string assured = "N/A";
                    string cache = "N/A";
                    string outsourced_pool = "N/A";
                    string not_outsourced_pool = "N/A";
                    string speed = "N/A";
                    string slots = "N/A";
                    int slots_used = 0;
                    string form_factor = "N/A";
                    string hardware_reserved = "N/A";

                    // Retrieving RAM information via /proc/meminfo
                    string[] memInfo = File.ReadAllLines("/proc/meminfo");
                    var memInfoDict = memInfo.ToDictionary(
                        line => line.Split(':')[0].Trim(),
                        line => line.Split(':')[1].Trim()
                    );

                    // Calculate the total and free memory
                    if (memInfoDict.ContainsKey("MemTotal") && memInfoDict.ContainsKey("MemFree"))
                    {
                        ulong totalMemory = Convert.ToUInt64(memInfoDict["MemTotal"].Split(' ')[0]); // in kB
                        ulong freeMemory = Convert.ToUInt64(memInfoDict["MemFree"].Split(' ')[0]); // in kB
                        ulong hardwareReservedMemory = totalMemory - freeMemory; // Reserved memory

                        hardware_reserved = (hardwareReservedMemory / 1024d).ToString(); // in MB

                        // round hardware_reserved
                        hardware_reserved = Math.Round(Convert.ToDouble(hardware_reserved)).ToString();
                    }

                    // Calculate 'Cache' based on 'Cached' and 'Buffers'
                    if (memInfoDict.ContainsKey("Cached") && memInfoDict.ContainsKey("Buffers"))
                    {
                        ulong cached = Convert.ToUInt64(memInfoDict["Cached"].Split(' ')[0]); // in kB
                        ulong buffers = Convert.ToUInt64(memInfoDict["Buffers"].Split(' ')[0]); // in kB
                        cache = ((cached + buffers) / 1024d).ToString(); // in MB

                        // round cache
                        cache = Math.Round(Convert.ToDouble(cache)).ToString();
                    }

                    // Calculation of the 'outsourced_pool' (swap)
                    if (memInfoDict.ContainsKey("SwapTotal") && memInfoDict.ContainsKey("SwapFree"))
                    {
                        string swapTotal = memInfoDict["SwapTotal"];
                        string swapFree = memInfoDict["SwapFree"];
                        string swapUsed = (Convert.ToUInt64(swapTotal.Split(' ')[0]) - Convert.ToUInt64(swapFree.Split(' ')[0])).ToString();
                        outsourced_pool = swapUsed; // als Beispiel
                    }

                    // Berechne 'available' und 'assured' aus MemAvailable
                    if (memInfoDict.ContainsKey("MemAvailable"))
                    {
                        available = (Convert.ToUInt64(memInfoDict["MemAvailable"].Split(' ')[0]) / 1024d).ToString(); // in MB

                        // round available
                        available = Math.Round(Convert.ToDouble(available)).ToString();
                    }

                    // 'assured' could be interpreted here as the difference between MemTotal and MemFree
                    assured = ((Convert.ToUInt64(memInfoDict["MemTotal"].Split(' ')[0]) - Convert.ToUInt64(memInfoDict["MemFree"].Split(' ')[0])) / 1024d).ToString("F2"); // in MB, Format mit 2 Dezimalstellen

                    // Calculation of 'not_outsourced_pool' (storage without swap)
                    if (memInfoDict.ContainsKey("MemFree") && memInfoDict.ContainsKey("Cached"))
                    {
                        ulong freeWithoutSwap = Convert.ToUInt64(memInfoDict["MemFree"].Split(' ')[0]) + Convert.ToUInt64(memInfoDict["Cached"].Split(' ')[0]);
                        not_outsourced_pool = (freeWithoutSwap / 1024d).ToString(); // in MB

                        // round not_outsourced_pool
                        not_outsourced_pool = Math.Round(Convert.ToDouble(not_outsourced_pool)).ToString();
                    }

                    // If there is information on RAM slots, use this
                    try
                    {
                        string[] dmidecodeOutput = File.ReadAllLines("/sys/devices/system/-memory/block0/dimm0");

                        foreach (var line in dmidecodeOutput)
                        {
                            if (line.Contains("Size"))
                            {
                                slots_used++;
                                name = "DIMM Slot"; // This could be replaced by more accurate information
                                available = line.Split(':')[1].Trim();
                                form_factor = "DIMM"; // Example value, could be replaced by an exact query
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Device_Information.Hardware.RAM_Information", "Linux RAM dmidecode", "Error reading dmidecode: " + ex.ToString());
                    }

                    // Create the JSON object with the collected data
                    RAM_Information ramInfo = new RAM_Information
                    {
                        name = name,
                        available = available,
                        assured = assured,
                        cache = cache,
                        outsourced_pool = outsourced_pool,
                        not_outsourced_pool = not_outsourced_pool,
                        speed = speed,
                        slots = slots,
                        slots_used = slots_used.ToString(),
                        form_factor = form_factor,
                        hardware_reserved = hardware_reserved,
                    };

                    ram_information_json = JsonSerializer.Serialize(ramInfo, new JsonSerializerOptions { WriteIndented = true });

                    Logging.Device_Information("Device_Information.Hardware.RAM_Information", "Collected the following RAM information.", ram_information_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.RAM_Information", "RAM_Information", "An error occurred while gathering Linux memory data: " + ex.ToString());
                    return "{}";
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    string name = "N/A";
                    string available = "N/A";
                    string assured = "N/A";
                    string cache = "N/A";
                    string outsourced_pool = "N/A";
                    string not_outsourced_pool = "N/A";
                    string speed = "N/A";
                    string slots = "N/A";
                    int slots_used = 0;
                    string form_factor = "N/A";
                    string hardware_reserved = "N/A";

                    // Execute vm_stat command to gather memory statistics
                    string vmStatOutput = MacOS.Helper.Zsh.Execute_Script("RAM_Information", false, "vm_stat");
                    if (!string.IsNullOrEmpty(vmStatOutput))
                    {
                        // Parse vm_stat output
                        var memStats = vmStatOutput.Split('\n')
                            .Where(line => line.Contains(":"))
                            .Select(line => line.Split(':'))
                            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim().Replace(".", ""));

                        if (memStats.TryGetValue("Pages free", out var pagesFree) &&
                            memStats.TryGetValue("Pages active", out var pagesActive) &&
                            memStats.TryGetValue("Pages inactive", out var pagesInactive) &&
                            memStats.TryGetValue("Pages speculative", out var pagesSpeculative))
                        {
                            const int pageSize = 4096; // macOS pages are 4KB
                            ulong freeMemory = ulong.Parse(pagesFree) * pageSize;
                            ulong activeMemory = ulong.Parse(pagesActive) * pageSize;
                            ulong inactiveMemory = ulong.Parse(pagesInactive) * pageSize;
                            ulong speculativeMemory = ulong.Parse(pagesSpeculative) * pageSize;

                            // Available memory = free + inactive + speculative
                            available = Math.Round((freeMemory + inactiveMemory + speculativeMemory) / (1024d * 1024d)).ToString();
                            assured = (activeMemory / (1024d * 1024d)).ToString("F2");
                        }
                    }

                    // Use sysctl to gather additional memory information
                    string sysctlOutput = MacOS.Helper.Zsh.Execute_Script("RAM_Information", false, "sysctl hw.memsize");
                    if (!string.IsNullOrEmpty(sysctlOutput))
                    {
                        // Extract total memory size
                        var match = Regex.Match(sysctlOutput, @"hw\.memsize: (\d+)");
                        if (match.Success)
                        {
                            ulong totalMemory = ulong.Parse(match.Groups[1].Value);

                            // Convert from bytes to MB
                            double totalMemoryInMB = totalMemory / (1024d * 1024d);
                            hardware_reserved = totalMemoryInMB.ToString();

                            // Optional: Adjust thousands separator and decimal point
                            hardware_reserved = hardware_reserved.Replace(".", ",");
                        }
                    }

                    // Prepare the RAM information object
                    RAM_Information ramInfo = new RAM_Information
                    {
                        name = name,
                        available = available,
                        assured = assured,
                        cache = cache,
                        outsourced_pool = outsourced_pool,
                        not_outsourced_pool = not_outsourced_pool,
                        speed = speed,
                        slots = slots,
                        slots_used = slots_used.ToString(),
                        form_factor = form_factor,
                        hardware_reserved = hardware_reserved,
                    };

                    ram_information_json = JsonSerializer.Serialize(ramInfo, new JsonSerializerOptions { WriteIndented = true });

                    Logging.Device_Information("Device_Information.Hardware.RAM_Information", "Collected the following RAM information.", ram_information_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.RAM_Information", "RAM_Information", "An error occurred while gathering macOS memory data: " + ex.ToString());
                    return "{}";
                }
            }



            return ram_information_json;
        }

        public static int RAM_Usage()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    ulong totalVisibleMemorySize = ulong.Parse(Windows.Helper.WMI.Search("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem", "TotalVisibleMemorySize"));
                    ulong freePhysicalMemory = ulong.Parse(Windows.Helper.WMI.Search("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem", "FreePhysicalMemory"));

                    float usedMemory = totalVisibleMemorySize - freePhysicalMemory;
                    float usedMemoryPercentage = ((float)usedMemory / totalVisibleMemorySize) * 100;

                    int usedMemoryPercentageInt = Convert.ToInt32(Math.Round(usedMemoryPercentage));

                    Logging.Device_Information("Device_Information.Hardware.RAM_Utilization", "Current RAM Usage (%)", usedMemoryPercentageInt.ToString());

                    return usedMemoryPercentageInt;
                }
                else if (OperatingSystem.IsLinux())
                {
                    // RAM utilisation under Linux
                    string ramUsage = Linux.Helper.Bash.Execute_Script("RAM_Usage", false, "free | grep Mem | awk '{print $3/$2 * 100}'");

                    // Parse the result into a double to ensure proper rounding
                    if (double.TryParse(ramUsage, out double usageValue))
                    {
                        int roundedUsage = (int)Math.Round(usageValue); // Round to the nearest integer
                        return roundedUsage;
                    }
                    else
                        return 0;
                }
                else if (OperatingSystem.IsMacOS())
                {
                    try
                    {
                        // Executes the vm_stat command and analyses the output
                        string vmStatOutput = MacOS.Helper.Zsh.Execute_Script(
                            "RAM_Usage",
                            false,
                            "vm_stat | perl -ne '/page size of (\\d+)/ and $size=$1; /Pages\\s+([^:]+)[^\\d]+(\\d+)/ and printf(\"%-16s %16.2f Mi\\n\", \"$1:\", $2 * $size / 1048576);'"
                        );

                        if (string.IsNullOrEmpty(vmStatOutput))
                        {
                            throw new InvalidOperationException("No output from vm_stat command.");
                        }

                        // Initialisation of variables for calculation
                        double freeMemory = 0;
                        double activeMemory = 0;
                        double inactiveMemory = 0;
                        double speculativeMemory = 0;
                        double wiredMemory = 0;
                        double cachedMemory = 0; // Corresponds to macOS Cached Files
                        double totalMemory = 0;

                        // Parse die Ausgabe
                        foreach (var line in vmStatOutput.Split('\n'))
                        {
                            if (line.StartsWith("free:"))
                            {
                                freeMemory = ExtractMemoryValue(line);
                            }
                            else if (line.StartsWith("active:"))
                            {
                                activeMemory = ExtractMemoryValue(line);
                            }
                            else if (line.StartsWith("inactive:"))
                            {
                                inactiveMemory = ExtractMemoryValue(line);
                            }
                            else if (line.StartsWith("speculative:"))
                            {
                                speculativeMemory = ExtractMemoryValue(line);
                            }
                            else if (line.StartsWith("wired down:"))
                            {
                                wiredMemory = ExtractMemoryValue(line);
                            }
                            else if (line.StartsWith("cached:"))
                            {
                                cachedMemory = ExtractMemoryValue(line); // Can be mapped on the basis of inactive/speculative
                            }
                        }

                        // Calculate total memory (in MiB)
                        totalMemory = freeMemory + activeMemory + inactiveMemory + speculativeMemory + wiredMemory;

                        // Memory used (only active + wired + optional speculative)
                        double usedMemory = activeMemory + wiredMemory;

                        // Calculate RAM utilisation in percent
                        double memoryUsagePercentage = (usedMemory / totalMemory) * 100;

                        // Output of the RAM utilisation in percent
                        return Convert.ToInt32(Math.Round(memoryUsagePercentage));
                    }
                    catch (Exception ex)
                    {
                        Logging.Error("Device_Information.Hardware.RAM_Usage", "Error calculating RAM usage.", ex.ToString());
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.RAM_Utilization", "General error.", ex.ToString());
                return 0;
            }
        }

        // Auxiliary method for extracting memory values (in MiB) for macOS
        private static double ExtractMemoryValue(string line)
        {
            var match = Regex.Match(line, @"([\d\.]+)\s+Mi");
            return match.Success ? double.Parse(match.Groups[1].Value) : 0;
        }

        public static string Disks()
        {
            string disks_json = String.Empty;

            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // Create a list of JSON strings for each process
                    List<string> disksJsonList = new List<string>();
                    List<string> collectedLettersList = new List<string>();

                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskDrive"))
                    {
                        // Parallel execution of the loop
                        Parallel.ForEach(searcher.Get().OfType<ManagementObject>(), obj =>
                        {
                            try
                            {
                                string letter = string.Empty;
                                string volume_label = string.Empty;
                                string DeviceID = obj["DeviceID"]?.ToString() ?? "N/A"; // Safely access properties
                                string PNPDeviceID = obj["PNPDeviceID"]?.ToString() ?? "N/A";

                                try
                                {
                                    using (ManagementObjectSearcher searcher1 = new ManagementObjectSearcher("root\\CIMV2", $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{DeviceID}'}} WHERE AssocClass = Win32_DiskDriveToDiskPartition"))
                                    {
                                        foreach (ManagementObject obj1 in searcher1.Get())
                                        {
                                            foreach (ManagementObject obj2 in new ManagementObjectSearcher("root\\CIMV2", $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{obj1["DeviceID"]?.ToString()}'}} WHERE AssocClass = Win32_LogicalDiskToPartition").Get())
                                            {
                                                letter = obj2["Name"]?.ToString() ?? "N/A";
                                                volume_label = new DriveInfo(letter)?.VolumeLabel ?? "N/A";
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logging.Device_Information("Device_Information.Hardware.Disks", "Failed to associate disk to partition.", ex.ToString());
                                }

                                try
                                {
                                    // Get disk capacity by letter
                                    DriveInfo driveInfo = new DriveInfo(letter);
                                    string totalCapacityGBString = ((double)driveInfo.TotalSize / (1024 * 1024 * 1024)).ToString("0.00");
                                    string usedCapacityPercentageString = $"{((double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize) * 100:F2}";

                                    // Add the letter to the list of collected letters
                                    collectedLettersList.Add(letter);
                                    Logging.Device_Information("Device_Information.Hardware.Disks", "Collected the following disk information.", $"Letter: {letter}, Volume Label: {volume_label}, DeviceID: {DeviceID}, PNPDeviceID: {PNPDeviceID}");

                                    // Create disk object
                                    Disks diskInfo = new Disks
                                    {
                                        letter = letter,
                                        label = volume_label,
                                        model = obj["Model"]?.ToString() ?? "N/A",
                                        firmware_revision = obj["FirmwareRevision"]?.ToString() ?? "N/A",
                                        serial_number = obj["SerialNumber"]?.ToString() ?? "N/A",
                                        interface_type = obj["InterfaceType"]?.ToString() ?? "N/A",
                                        drive_type = driveInfo.DriveType.ToString(),
                                        drive_format = driveInfo.DriveFormat,
                                        drive_ready = driveInfo.IsReady.ToString(),
                                        capacity = totalCapacityGBString,
                                        usage = usedCapacityPercentageString,
                                        status = obj["Status"]?.ToString() ?? "N/A",
                                    };

                                    // Serialize the disk object into a JSON string and add it to the list
                                    string disksJson = JsonSerializer.Serialize(diskInfo, new JsonSerializerOptions { WriteIndented = true });
                                    disksJsonList.Add(disksJson);
                                }
                                catch (Exception ex)
                                {
                                    Logging.Device_Information("Device_Information.Hardware.Disks", "Failed to gather disk capacity.", ex.ToString());
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.Device_Information("Device_Information.Hardware.Disks", "Failed to collect disk information.", ex.ToString());
                            }
                        });

                        // Attempt to gather basic disk information for each letter that failed
                        try
                        {
                            foreach (DriveInfo drive in DriveInfo.GetDrives())
                            {
                                string letter_short = drive.Name.Replace("\\", "");

                                try
                                {
                                    if (!collectedLettersList.Contains(letter_short))
                                    {
                                        Disks diskInfo = new Disks
                                        {
                                            letter = letter_short,
                                            label = string.IsNullOrEmpty(drive.VolumeLabel) ? "N/A" : drive.VolumeLabel,
                                            model = "N/A",
                                            firmware_revision = "N/A",
                                            serial_number = "N/A",
                                            interface_type = "N/A",
                                            drive_type = drive.DriveType.ToString(),
                                            drive_format = drive.DriveFormat,
                                            drive_ready = drive.IsReady.ToString(),
                                            capacity = ((double)drive.TotalSize / (1024 * 1024 * 1024)).ToString("0.00"),
                                            usage = $"{((double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize) * 100:F2}",
                                            status = "N/A",
                                        };

                                        // Serialize the disk object into a JSON string and add it to the list
                                        string disksJson = JsonSerializer.Serialize(diskInfo, new JsonSerializerOptions { WriteIndented = true });
                                        disksJsonList.Add(disksJson);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logging.Device_Information("Device_Information.Hardware.Disks", "Failed to gather basic disk information for letter.", ex.ToString());
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Device_Information("Device_Information.Hardware.Disks", "Failed to gather basic disk information for uncollected letters (general error).", ex.ToString());
                        }

                        // Create and log JSON array
                        disks_json = "[" + string.Join("," + Environment.NewLine, disksJsonList) + "]";
                        Logging.Device_Information("Device_Information.Hardware.Disks", "Collected the following disk information.", disks_json);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.Disks", "General error in Disks method.", ex.ToString());
                    disks_json = "[]";
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    List<string> disksJsonList = new List<string>();

                    // List of JSON data for each hard drive
                    DriveInfo[] allDrives = DriveInfo.GetDrives();

                    foreach (var drive in allDrives)
                    {
                        try
                        {
                            // Read out total size in bytes
                            long totalSizeInBytes = drive.TotalSize; // Assumption: TotalSize is a long
                            long freeSpaceInBytes = drive.TotalFreeSpace; // Free memory space in bytes

                            // Convert sizes to GB
                            double totalSizeInGB = totalSizeInBytes / (1024.0 * 1024.0 * 1024.0);

                            // Round the total size to two decimal places
                            string sizeInGB = totalSizeInGB.ToString("F2");

                            double freeSpaceInGB = freeSpaceInBytes / (1024.0 * 1024.0 * 1024.0);

                            // Calculation of utilisation in percent
                            double usagePercentage = (totalSizeInBytes > 0)
                                ? ((totalSizeInBytes - freeSpaceInBytes) / (double)totalSizeInBytes) * 100
                                : 0;

                            // Round the utilisation percentage to two decimal places
                            string usagePercent = usagePercentage.ToString("F2");

                            // Compile disk information
                            Disks diskInfo = new Disks
                            {
                                letter = drive.Name,  // In Linux there are no drive letters as in Windows, so we use the name.
                                label = "N/A", // drive.VolumeLabel is the same as drive.Name
                                model = "N/A",
                                firmware_revision = "N/A",  // Linux does not provide a firmware version via lsblk
                                serial_number = "N/A",
                                interface_type = "N/A",  // Cannot be retrieved in lsblk
                                drive_type = drive.DriveType.ToString(),
                                drive_format = drive.DriveFormat,
                                drive_ready = drive.IsReady ? "True" : "False",
                                capacity = sizeInGB,
                                usage = usagePercent,
                                status = "N/A",
                            };

                            // Serialize disk object and add to list
                            string disksJson = JsonSerializer.Serialize(diskInfo, new JsonSerializerOptions { WriteIndented = true });
                            disksJsonList.Add(disksJson);
                        }
                        catch (Exception ex)
                        {
                            Logging.Device_Information("Device_Information.Hardware.Disks", "Failed to collect disk information.", ex.ToString());
                        }
                    }

                    // JSON-Array erstellen und loggen
                    disks_json = "[" + string.Join("," + Environment.NewLine, disksJsonList) + "]";
                    Logging.Device_Information("Device_Information.Hardware.Disks", "Collected the following disk information.", disks_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.Disks", "General error in Disks method.", ex.ToString());
                    disks_json = "[]";
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    List<string> disksJsonList = new List<string>();

                    // List of JSON data for each hard drive
                    DriveInfo[] allDrives = DriveInfo.GetDrives();

                    foreach (var drive in allDrives)
                    {
                        try
                        {
                            // Read out total size in bytes
                            long totalSizeInBytes = drive.TotalSize; // Assumption: TotalSize is a long
                            long freeSpaceInBytes = drive.TotalFreeSpace; // Free memory space in bytes

                            // Convert sizes to GB
                            double totalSizeInGB = totalSizeInBytes / (1024.0 * 1024.0 * 1024.0);

                            // Round the total size to two decimal places
                            string sizeInGB = totalSizeInGB.ToString("F2");

                            double freeSpaceInGB = freeSpaceInBytes / (1024.0 * 1024.0 * 1024.0);

                            // Calculation of utilisation in percent
                            double usagePercentage = (totalSizeInBytes > 0)
                                ? ((totalSizeInBytes - freeSpaceInBytes) / (double)totalSizeInBytes) * 100
                                : 0;

                            // Round the utilisation percentage to two decimal places
                            string usagePercent = usagePercentage.ToString("F2");

                            // Compile disk information
                            Disks diskInfo = new Disks
                            {
                                letter = drive.Name,  // In Linux there are no drive letters as in Windows, so we use the name.
                                label = "N/A", // drive.VolumeLabel is the same as drive.Name
                                model = "N/A",
                                firmware_revision = "N/A",  // Linux does not provide a firmware version via lsblk
                                serial_number = "N/A",
                                interface_type = "N/A",  // Cannot be retrieved in lsblk
                                drive_type = drive.DriveType.ToString(),
                                drive_format = drive.DriveFormat,
                                drive_ready = drive.IsReady ? "True" : "False",
                                capacity = sizeInGB,
                                usage = usagePercent,
                                status = "N/A",
                            };

                            // Serialize disk object and add to list
                            string disksJson = JsonSerializer.Serialize(diskInfo, new JsonSerializerOptions { WriteIndented = true });
                            disksJsonList.Add(disksJson);
                        }
                        catch (Exception ex)
                        {
                            Logging.Device_Information("Device_Information.Hardware.Disks", "Failed to collect disk information.", ex.ToString());
                        }
                    }

// List of JSON data for each hard drive
/*List<string> disksJsonList = new List<string>();

// Retrieve all hard disc information with diskutil in plist format
string diskutilListOutput = MacOS.Helper.Zsh.Execute_Script("Disks", false, "diskutil list -plist");
Logging.Device_Information("Device_Information.Hardware.Disks", "diskutil-list-Output", diskutilListOutput);

// Parsing the plist output
var plist = MacOS.Helper.Plist.Parse(diskutilListOutput);
var allDisks = plist["AllDisksAndPartitions"] as IEnumerable<object>;

foreach (var diskObj in allDisks)
{
    try
    {
        var disk = diskObj as Dictionary<string, object>;

        string name = disk.GetValueOrDefault("DeviceIdentifier", "N/A").ToString();

        // Retrieve diskutil info for this device
        string diskutilInfoOutput = MacOS.Helper.Zsh.Execute_Script("Disks", false, $"diskutil info -plist {name}");
        var infoPlist = MacOS.Helper.Plist.Parse(diskutilInfoOutput);

        // Extract size details
        long totalSizeInBytes = infoPlist.GetValueOrDefault("TotalSize", 0L) is long totalSize ? totalSize : 0;
        long freeSpaceInBytes = infoPlist.GetValueOrDefault("FreeSpace", 0L) is long freeSpace ? freeSpace : 0;

        // Calculate usage percentage
        double usagePercentage = (totalSizeInBytes > 0)
            ? ((totalSizeInBytes - freeSpaceInBytes) / (double)totalSizeInBytes) * 100
            : 0;

        // Convert sizes to GB
        double totalSizeInGB = MacOS.Helper.MacOS.Disks_Convert_Size_To_GB_Two(totalSizeInBytes);
        double freeSpaceInGB = MacOS.Helper.MacOS.Disks_Convert_Size_To_GB_Two(freeSpaceInBytes);

        Console.WriteLine($"Total size: {totalSizeInGB} GB, Free space: {freeSpaceInGB} GB, Usage: {usagePercentage:F2}%");

        // Compile disk information
        Disks diskInfo = new Disks
        {
            letter = name,
            label = disk.GetValueOrDefault("VolumeName", "N/A").ToString(),
            model = infoPlist.GetValueOrDefault("MediaName", "N/A").ToString(),
            drive_type = infoPlist.GetValueOrDefault("SolidState", false).ToString() == "True" ? "SSD" : "HDD",
            drive_format = disk.GetValueOrDefault("FilesystemName", "N/A").ToString(),
            capacity = totalSizeInGB.ToString("F2") + " GB",
            usage = usagePercentage.ToString("F2") + " %", // Add usage in percentage
            status = infoPlist.GetValueOrDefault("Writable", false).ToString() == "True" ? "Online" : "Offline",
        };

        // Serialize disk object and add to list
        string disksJson = JsonSerializer.Serialize(diskInfo, new JsonSerializerOptions { WriteIndented = true });
        disksJsonList.Add(disksJson);
    }
    catch (Exception ex)
    {
        Logging.Device_Information("Device_Information.Hardware.Disks", "Failed to collect disk information.", ex.ToString());
    }
}*/

                    // JSON-Array erstellen und loggen
                    disks_json = "[" + string.Join("," + Environment.NewLine, disksJsonList) + "]";
                    Logging.Device_Information("Device_Information.Hardware.Disks", "Collected the following disk information.", disks_json);
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.Disks", "General error in Disks method.", ex.ToString());
                    disks_json = "[]";
                }
            }
            else
            {
                return "[]";
            }

            return disks_json;
        }

        public static string RAM_Total()
        {
            // Get RAM
            string _ram = string.Empty;

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    _ram = Windows.Helper.WMI.Search("root\\CIMV2", "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem", "TotalPhysicalMemory");
                    _ram = Math.Round(Convert.ToDouble(_ram) / 1024 / 1024 / 1024).ToString();
                }
                else if (OperatingSystem.IsLinux())
                {
                    _ram = Linux.Helper.Bash.Execute_Script("RAM_Total", false, "grep MemTotal /proc/meminfo | awk '{print $2}'");
                    _ram = Math.Round(Convert.ToDouble(_ram) / 1024 / 1024).ToString();
                }
                else if (OperatingSystem.IsMacOS())
                {
                    string systemProfilerOutput = MacOS.Helper.Zsh.Execute_Script("RAM_Total", false, "system_profiler SPHardwareDataType");
                    if (!string.IsNullOrEmpty(systemProfilerOutput))
                    {
                        foreach (string line in systemProfilerOutput.Split('\n'))
                        {
                            if (line.Contains("Memory:"))
                            {
                                _ram = line.Split(':')[1].Trim();
                                _ram = _ram.Replace(" GB", "");
                                _ram = _ram.Replace(".", ",");
                                break;
                            }
                        }
                    }
                }

                return _ram;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.RAM_Total", "General error.", ex.ToString());
                return "N/A";
            }
        }

        public static int Drive_Usage(int type, string drive_letter) // 0 = More than X GB occupied, 1 = Less than X GB free, 2 = More than X percent occupied, 3 = Less than X percent free
        {
            try
            {
                DriveInfo drive_info = new DriveInfo(drive_letter.ToString());

                if (drive_info.IsReady)
                {
                    // Available and total memory sizes in bytes
                    long availableFreeSpaceBytes = drive_info.AvailableFreeSpace;
                    long totalFreeSpaceBytes = drive_info.TotalFreeSpace;
                    long totalSizeBytes = drive_info.TotalSize;

                    // Conversion from bytes to gigabytes
                    double availableFreeSpaceGB = availableFreeSpaceBytes / (1024.0 * 1024.0 * 1024.0);
                    double totalFreeSpaceGB = totalFreeSpaceBytes / (1024.0 * 1024.0 * 1024.0);
                    double totalSizeGB = totalSizeBytes / (1024.0 * 1024.0 * 1024.0);

                    // Conversion from bytes to gigabytes
                    double usedSpaceGB = totalSizeGB - availableFreeSpaceGB;

                    // Calculation of the memory space used as a percentage
                    double usedSpacePercentage = 100 * (usedSpaceGB / totalSizeGB);
                    usedSpacePercentage = Math.Round(usedSpacePercentage, 2);

                    // Output of the results
                    Logging.Device_Information("Device_Information.Hardware.Drive_Usage", "Total memory GB", totalSizeGB.ToString());
                    Logging.Device_Information("Device_Information.Hardware.Drive_Usage", "Free memory GB", availableFreeSpaceGB.ToString());
                    Logging.Device_Information("Device_Information.Hardware.Drive_Usage", "Memory used GB", usedSpaceGB.ToString());
                    Logging.Device_Information("Device_Information.Hardware.Drive_Usage", "Memory used %", usedSpacePercentage.ToString());

                    if (type == 0) // More than X GB occupied
                        return Convert.ToInt32(Math.Round(usedSpaceGB));
                    else if (type == 1) // Less than X GB free
                        return Convert.ToInt32(Math.Round(availableFreeSpaceGB));
                    else if (type == 2) // More than X percent occupied
                        return Convert.ToInt32(Math.Round(usedSpacePercentage));
                    else if (type == 3) // Less than X percent free
                        return Convert.ToInt32(Math.Round(100 - usedSpacePercentage));
                    else
                        return 0;
                }
                else
                {
                    Logging.Device_Information("Device_Information.Hardware.Drive_Usage", "The drive is not ready", drive_letter.ToString());
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.Drive_Usage", "General error.", ex.ToString());
                return 0;
            }
        }

        private static double ConvertToGB(string space)
        {
            try
            {
                if (space.EndsWith("G"))
                {
                    return double.Parse(space.TrimEnd('G'));
                }
                else if (space.EndsWith("M"))
                {
                    return double.Parse(space.TrimEnd('M')) / 1024.0;
                }
                else if (space.EndsWith("K"))
                {
                    return double.Parse(space.TrimEnd('K')) / (1024.0 * 1024.0);
                }
                else
                {
                    Logging.Error("Device_Information.Hardware.ConvertToGB", "Unknown space unit.", space);
                    throw new InvalidOperationException("Unknown space unit.");
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.ConvertToGB", "General error.", ex.ToString());
                return 0;
            }
        }

        public static int Drive_Size_GB(string drivePath) // drivePath: "C:" für Windows, "/" für Linux | // 0 = More than X GB occupied, 1 = Less than X GB free, 2 = More than X percent occupied, 3 = Less than X percent free
        {
            try
            {
                DriveInfo driveInfo = new DriveInfo(drivePath);

                if (OperatingSystem.IsWindows())
                {
                    if (driveInfo.IsReady)
                    {
                        // Total memory size in bytes
                        long totalSizeBytes = driveInfo.TotalSize;

                        // Conversion from bytes to gigabytes
                        double totalSizeGB = totalSizeBytes / (1024.0 * 1024.0 * 1024.0);

                        // Logging the results
                        Logging.Device_Information("Device_Information.Hardware.Drive_Size", "Total memory GB", totalSizeGB.ToString());

                        return Convert.ToInt32(Math.Round(totalSizeGB));
                    }
                    else
                    {
                        Logging.Device_Information("Device_Information.Hardware.Drive_Size", "The drive is not ready", drivePath);
                        return 0;
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    if (driveInfo.IsReady)
                    {
                        // Total memory size in bytes
                        long totalSizeBytes = driveInfo.TotalSize;

                        // Conversion from bytes to gigabytes
                        double totalSizeGB = totalSizeBytes / (1024.0 * 1024.0 * 1024.0);

                        // Logging the results
                        Logging.Device_Information("Device_Information.Hardware.Drive_Size", "Total memory GB", totalSizeGB.ToString());

                        return Convert.ToInt32(Math.Round(totalSizeGB));
                    }
                    else
                    {
                        Logging.Device_Information("Device_Information.Hardware.Drive_Size", "The drive is not ready", drivePath);
                        return 0;
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    if (driveInfo.IsReady)
                    {
                        // Total memory size in bytes
                        long totalSizeBytes = driveInfo.TotalSize;

                        // Conversion from bytes to gigabytes
                        double totalSizeGB = totalSizeBytes / (1024.0 * 1024.0 * 1024.0);

                        // Logging the results
                        Logging.Device_Information("Device_Information.Hardware.Drive_Size", "Total memory GB", totalSizeGB.ToString());

                        return Convert.ToInt32(Math.Round(totalSizeGB));
                    }
                    else
                    {
                        Logging.Device_Information("Device_Information.Hardware.Drive_Size", "The drive is not ready", drivePath);
                        return 0;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.Drive_Size", "General error.", ex.ToString());
                return 0;
            }
        }

        public static int Drive_Free_Space_GB(string drivePath)
        {
            try
            {
                DriveInfo driveInfo = new DriveInfo(drivePath);

                if (OperatingSystem.IsWindows())
                {
                    if (driveInfo.IsReady)
                    {
                        // Free memory space in bytes
                        long freeSpaceBytes = driveInfo.AvailableFreeSpace;
                        
                        // Conversion from bytes to gigabytes
                        double freeSpaceGB = freeSpaceBytes / (1024.0 * 1024.0 * 1024.0);
                        
                        // Logging the results
                        Logging.Device_Information("Device_Information.Hardware.Drive_Free_Space", "Free space GB", freeSpaceGB.ToString());
                        
                        return Convert.ToInt32(Math.Round(freeSpaceGB));
                    }
                    else
                    {
                        Logging.Device_Information("Device_Information.Hardware.Drive_Free_Space", "The drive is not ready", drivePath);
                        return 0;
                    }
                }
                else if (OperatingSystem.IsLinux())
                {
                    if (driveInfo.IsReady)
                    {
                        // Free memory space in bytes
                        long freeSpaceBytes = driveInfo.AvailableFreeSpace;

                        // Conversion from bytes to gigabytes
                        double freeSpaceGB = freeSpaceBytes / (1024.0 * 1024.0 * 1024.0);

                        // Logging the results
                        Logging.Device_Information("Device_Information.Hardware.Drive_Free_Space", "Free space GB", freeSpaceGB.ToString());

                        return Convert.ToInt32(Math.Round(freeSpaceGB));
                    }
                    else
                    {
                        Logging.Device_Information("Device_Information.Hardware.Drive_Free_Space", "The drive is not ready", drivePath);
                        return 0;
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    if (driveInfo.IsReady)
                    {
                        // Free memory space in bytes
                        long freeSpaceBytes = driveInfo.AvailableFreeSpace;

                        // Conversion from bytes to gigabytes
                        double freeSpaceGB = freeSpaceBytes / (1024.0 * 1024.0 * 1024.0);

                        // Logging the results
                        Logging.Device_Information("Device_Information.Hardware.Drive_Free_Space", "Free space GB", freeSpaceGB.ToString());

                        return Convert.ToInt32(Math.Round(freeSpaceGB));
                    }
                    else
                    {
                        Logging.Device_Information("Device_Information.Hardware.Drive_Free_Space", "The drive is not ready", drivePath);
                        return 0;
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.Drive_Free_Space", "General error.", ex.ToString());
                return 0;
            }
        }

        public static string Mainboard_Name()
        {
            string _mainboard = "N/A";
            string mainboard_manufacturer = "N/A";

            if (OperatingSystem.IsWindows())
            {
                _mainboard = Windows.Helper.WMI.Search("root\\CIMV2", "SELECT Product FROM Win32_BaseBoard", "Product");
                mainboard_manufacturer = Windows.Helper.WMI.Search("root\\CIMV2", "SELECT Manufacturer FROM Win32_BaseBoard", "Manufacturer");
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    // Check paths to the mainboard information
                    string[] boardNamePaths = { "/sys/class/dmi/id/board_name", "/sys/devices/virtual/dmi/id/board_name" };
                    string[] boardVendorPaths = { "/sys/class/dmi/id/board_vendor", "/sys/devices/virtual/dmi/id/board_vendor" };

                    // Extract board name
                    foreach (var path in boardNamePaths)
                    {
                        if (File.Exists(path))
                        {
                            _mainboard = Linux.Helper.Bash.Execute_Script("Mainboard_Name", false, $"cat {path}").Trim();
                            break;
                        }
                    }

                    // Extract manufacturer
                    foreach (var path in boardVendorPaths)
                    {
                        if (File.Exists(path))
                        {
                            mainboard_manufacturer = Linux.Helper.Bash.Execute_Script("Mainboard_Name", false, $"cat {path}").Trim();
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.Mainboard_Name", "General error.", ex.ToString());
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    // Get the mainboard name
                    _mainboard = "N/A";
                    // Get the mainboard manufacturer
                    mainboard_manufacturer = "N/A";
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.Mainboard_Name", "General error.", ex.ToString());
                }
            }

            return $"{mainboard_manufacturer} ({_mainboard})";
        }

        public static string GPU_Name()
        {
            string gpu = "N/A";

            if (OperatingSystem.IsWindows())
            {
                // Get GPU
                gpu = Windows.Helper.WMI.Search("root\\CIMV2", "SELECT Name FROM Win32_VideoController", "Name");
                Logging.Debug("Online_Mode.Handler.Authenticate", "gpu", Device_Worker.gpu);
            }
            else if (OperatingSystem.IsLinux())
            {
                string output = Linux.Helper.Bash.Execute_Script("GPU_Name", false, "lshw -C display");
                if (!string.IsNullOrWhiteSpace(output))
                {
                    string product = null;
                    string vendor = null;

                    // Alle Zeilen durchsuchen
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("product:"))
                        {
                            product = trimmed.Substring("product:".Length).Trim();
                        }
                        else if (trimmed.StartsWith("vendor:"))
                        {
                            vendor = trimmed.Substring("vendor:".Length).Trim();
                        }

                        // If both are found, cancel
                        if (!string.IsNullOrEmpty(product) && !string.IsNullOrEmpty(vendor))
                        {
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(product) && !string.IsNullOrEmpty(vendor))
                    {
                        gpu = $"{vendor} {product}";
                    }
                    else if (!string.IsNullOrEmpty(product))
                    {
                        gpu = product;
                    }
                    else if (!string.IsNullOrEmpty(vendor))
                    {
                        gpu = vendor;
                    }
                    else
                    {
                        gpu = "N/A";
                    }
                }
                else
                {
                    gpu = "N/A";
                }

                Logging.Debug("Online_Mode.Handler.Authenticate", "gpu", gpu);
            }
            else if (OperatingSystem.IsMacOS())
            {
                // Get GPU
                gpu = "N/A";
                Logging.Debug("Online_Mode.Handler.Authenticate", "gpu", Device_Worker.gpu);
            }

            return gpu;
        }

        public static string TPM_Status()
        {
            if (OperatingSystem.IsWindows()) 
            {
                // Get TPM status
                string tpm_status = Windows.Helper.WMI.Search("root\\cimv2\\Security\\MicrosoftTpm", "SELECT IsEnabled_InitialValue FROM Win32_Tpm", "IsEnabled_InitialValue");
                return tpm_status;
            }
            else
            {
                return "N/A";
            }
        }
    }
}
