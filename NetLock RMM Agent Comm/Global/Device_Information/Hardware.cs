﻿using System;
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
                    cpu_usage = CPU_Usage();
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

        public static string CPU_Usage()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    // CPU utilisation under Windows
                    PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    cpuCounter.NextValue(); // Ignore first measurement
                    Thread.Sleep(1000); // Wait 1 second

                    int cpuUsage = Convert.ToInt32(Math.Round(cpuCounter.NextValue()));

                    return cpuUsage.ToString();
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.CPU_Utilization", "General error.", ex.ToString());
                    return "0";
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    // CPU utilisation under Linux
                    string cpuUsage = File.ReadAllText("/proc/loadavg").Split(' ')[0];
                    return cpuUsage;
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.CPU_Utilization", "General error.", ex.ToString());
                    return "0";
                }
            }
            else
            {
                return "0";
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

                                    hardware_reserved = (hardwareReservedMemory / 1024d).ToString(); // Convert to MB
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

            return ram_information_json;
        }

        public static string RAM_Usage()
        {
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    ulong totalVisibleMemorySize = ulong.Parse(Windows.Helper.WMI.Search("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem", "TotalVisibleMemorySize"));
                    ulong freePhysicalMemory = ulong.Parse(Windows.Helper.WMI.Search("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem", "FreePhysicalMemory"));

                    float usedMemory = totalVisibleMemorySize - freePhysicalMemory;
                    float usedMemoryPercentage = ((float)usedMemory / totalVisibleMemorySize) * 100;

                    int usedMemoryPercentageInt = Convert.ToInt32(Math.Round(usedMemoryPercentage));

                    Logging.Device_Information("Device_Information.Hardware.RAM_Utilization", "Current RAM Usage (%)", usedMemoryPercentageInt.ToString());

                    return usedMemoryPercentageInt.ToString();
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.RAM_Utilization", "General error.", ex.ToString());
                    return "0";
                }
            }
            else if (OperatingSystem.IsLinux())    
            {
                try
                {
                    // RAM utilisation under Linux
                    string ramUsage = Linux.Helper.Bash.Execute_Command("free | grep Mem | awk '{print $3/$2 * 100}'");
                    return ramUsage;
                }
                catch (Exception ex)
                {
                    Logging.Error("Device_Information.Hardware.RAM_Utilization", "General error.", ex.ToString());
                    return "0";
                }
            }
            else
            {
                return "0";
            }
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
                    // Create a list of JSON strings for each hard drive
                    List<string> disksJsonList = new List<string>();
                    List<string> collectedLettersList = new List<string>();

                    // Retrieve all hard drives
                    string lsblkOutput = Linux.Helper.Bash.Execute_Command("lsblk -J -o NAME,SIZE,TYPE,MODEL,SERIAL,TRAN,ROTA,FSTYPE,UUID,STATE");

                    Logging.Device_Information("Device_Information.Hardware.Disks", "lsblk-Output", lsblkOutput);

                    // Deserialising the lsblk output
                    var lsblkJson = JsonSerializer.Deserialize<JsonDocument>(lsblkOutput);
                    var blockdevices = lsblkJson.RootElement.GetProperty("blockdevices").EnumerateArray();

                    foreach (var device in blockdevices)
                    {
                        try
                        {
                            string name = device.GetProperty("name").GetString() ?? "N/A";
                            string size = device.GetProperty("size").GetString() ?? "N/A";
                            string type = device.GetProperty("type").GetString() ?? "N/A";
                            string model = device.GetProperty("model").GetString() ?? "N/A";
                            string serial = device.GetProperty("serial").GetString() ?? "N/A";
                            string fstype = device.GetProperty("fstype").GetString() ?? "N/A";
                            string uuid = device.GetProperty("uuid").GetString() ?? "N/A";
                            string state = device.GetProperty("state").GetString() ?? "N/A";

                            // Calculate the capacity in GB (if available)
                            double sizeInGB = Linux.Helper.Linux.Disks_Convert_Size_To_GB(size);

                            // Use the df command to determine the current usage of the file system
                            string dfOutput = Linux.Helper.Bash.Execute_Command($"df -h /dev/{name} | tail -n 1");
                            string[] dfParts = dfOutput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            string usagePercent = dfParts.Length > 4 ? dfParts[4] : "N/A"; // Percent usage

                            // Remove the percentage sign from the usage
                            usagePercent = usagePercent.Replace("%", "");

                            // Die Information für jedes Laufwerk
                            Disks diskInfo = new Disks
                            {
                                letter = name,  // In Linux there are no drive letters as in Windows, so we use the name.
                                label = model,
                                model = model,
                                firmware_revision = "N/A",  // Linux does not provide a firmware version via lsblk
                                serial_number = serial,
                                interface_type = "N/A",  // Cannot be retrieved in lsblk
                                drive_type = type,
                                drive_format = fstype,
                                drive_ready = state == "running" ? "True" : "False",
                                capacity = sizeInGB.ToString(),
                                usage = usagePercent,
                                status = state,
                            };

                            // Serialize the disk object into a JSON string and add it to the list
                            string disksJson = JsonSerializer.Serialize(diskInfo, new JsonSerializerOptions { WriteIndented = true });
                            disksJsonList.Add(disksJson);
                        }
                        catch (Exception ex)
                        {
                            Logging.Device_Information("Device_Information.Hardware.Disks", "Failed to collect disk information from lsblk output.", ex.ToString());
                        }
                    }

                    // Create and log JSON array
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

        public static int CPU_Utilization()
        {
            try
            {
                // Create a new PerformanceCounter instance
                using (PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    // Give the counter some time to initialize
                    Thread.Sleep(1000);

                    // Get the current value of the CPU usage
                    float cpuUsage = cpuCounter.NextValue();

                    // Print the CPU usage to the console
                    //Logging.Handler.Device_Information("Device_Information.Hardware.CPU_Utilization", "Current CPU Usage (%)", cpuUsage.ToString());

                    // To get more accurate results, wait for a short period and take another reading
                    Thread.Sleep(1000);
                    cpuUsage = cpuCounter.NextValue();

                    Logging.Device_Information("Device_Information.Hardware.CPU_Utilization", "Current CPU Usage (%)", cpuUsage.ToString());

                    return Convert.ToInt32(Math.Round(cpuUsage));
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.CPU_Utilization", "General error.", ex.ToString());
                return 0;
            }
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
                    _ram = Linux.Helper.Bash.Execute_Command("grep MemTotal /proc/meminfo | awk '{print $2}'");
                    _ram = Math.Round(Convert.ToDouble(_ram) / 1024 / 1024).ToString();
                }

                return _ram;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.RAM_Total", "General error.", ex.ToString());
                return "N/A";
            }
        }

        public static int RAM_Utilization()
        {
            try
            {
                ulong totalVisibleMemorySize = ulong.Parse(Windows.Helper.WMI.Search("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem", "TotalVisibleMemorySize"));
                ulong freePhysicalMemory = ulong.Parse(Windows.Helper.WMI.Search("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem", "FreePhysicalMemory"));

                float usedMemory = totalVisibleMemorySize - freePhysicalMemory;
                float usedMemoryPercentage = ((float)usedMemory / totalVisibleMemorySize) * 100;

                int usedMemoryPercentageInt = Convert.ToInt32(Math.Round(usedMemoryPercentage));

                Logging.Device_Information("Device_Information.Hardware.RAM_Utilization", "Current RAM Usage (%)", usedMemoryPercentageInt.ToString());

                return usedMemoryPercentageInt;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.RAM_Utilization", "General error.", ex.ToString());
                return 0;
            }
        }



        public static int Drive_Usage(int type, char drive_letter) // 0 = More than X GB occupied, 1 = Less than X GB free, 2 = More than X percent occupied, 3 = Less than X percent free
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

                    // Ausgabe der Ergebnisse
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
                    Logging.Device_Information("Device_Information.Hardware.Drive_Usage", "The drive is not ready", drive_letter.ToString());

                return 0;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.Drive_Usage", "General error.", ex.ToString());
                return 0;
            }
        }

        public static int Drive_Size(char drive_letter) // 0 = More than X GB occupied, 1 = Less than X GB free, 2 = More than X percent occupied, 3 = Less than X percent free
        {
            try
            {
                DriveInfo drive_info = new DriveInfo(drive_letter.ToString());

                if (drive_info.IsReady)
                {
                    // Available and total memory sizes in bytes
                    long totalSizeBytes = drive_info.TotalSize;

                    // Conversion from bytes to gigabytes
                    double totalSizeGB = totalSizeBytes / (1024.0 * 1024.0 * 1024.0);

                    // Ausgabe der Ergebnisse
                    Logging.Device_Information("Device_Information.Hardware.Drive_Usage", "Total memory GB", totalSizeGB.ToString());

                    return Convert.ToInt32(Math.Round(totalSizeGB));
                }
                else
                    Logging.Device_Information("Device_Information.Hardware.Drive_Usage", "The drive is not ready", drive_letter.ToString());

                return 0;
            }
            catch (Exception ex)
            {
                Logging.Error("Device_Information.Hardware.Drive_Usage", "General error.", ex.ToString());
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
                            _mainboard = Linux.Helper.Bash.Execute_Command($"cat {path}").Trim();
                            break;
                        }
                    }

                    // Extract manufacturer
                    foreach (var path in boardVendorPaths)
                    {
                        if (File.Exists(path))
                        {
                            mainboard_manufacturer = Linux.Helper.Bash.Execute_Command($"cat {path}").Trim();
                            break;
                        }
                    }
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
                // Get GPU
                gpu = Linux.Helper.Bash.Execute_Command("lshw -C display");

                var match_product = Regex.Match(gpu, $@"product:\s*(.+)");
                var match_vendor = Regex.Match(gpu, $@"vendor:\s*(.+)");

                if (!string.IsNullOrWhiteSpace(gpu))
                {
                    // GPU-Informationen aus der Ausgabe extrahieren
                    string product = match_product.Success ? match_product.Groups[1].Value : string.Empty;
                    string vendor = match_vendor.Success ? match_vendor.Groups[1].Value : string.Empty;

                    if (!string.IsNullOrWhiteSpace(product) && !string.IsNullOrWhiteSpace(vendor))
                    {
                        gpu = $"{vendor.Trim()} {product.Trim()}";
                    }
                    else if (!string.IsNullOrWhiteSpace(product))
                    {
                        gpu = product.Trim();
                    }
                    else if (!string.IsNullOrWhiteSpace(vendor))
                    {
                        gpu = vendor.Trim();
                    }
                }

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
            else if (OperatingSystem.IsLinux())
            {
                return "N/A";
            }
            else
            {
                return "N/A";
            }
        }
    }
}
