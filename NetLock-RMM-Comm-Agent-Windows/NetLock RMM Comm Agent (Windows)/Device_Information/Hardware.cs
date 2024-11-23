using NetLock_RMM_Comm_Agent_Windows.Helper;
using Newtonsoft.Json;
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
using static NetLock_RMM_Comm_Agent_Windows.Online_Mode.Handler;

namespace NetLock_RMM_Comm_Agent_Windows.Device_Information
{
    internal class Hardware
    {
        public static string CPU_Information()
        {
            string cpu_information_json = string.Empty;

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
                            Logging.Handler.Error("Device_Information.Hardware.CPU_Information", "CPU_Information", "An error occurred while querying for WMI data: " + ex.ToString());
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
                            cpu_information_json = JsonConvert.SerializeObject(cpuInfo, Formatting.Indented);
                            Logging.Handler.Device_Information("Device_Information.Hardware.CPU_Information", "cpu_information_json", cpu_information_json);
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Error("Device_Information.Hardware.CPU_Information", "CPU_Information", "An error occurred while querying for WMI data: " + ex.ToString());
                        }
                    }
                }

                return cpu_information_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Hardware.CPU_Information", "CPU_Information", "An error occurred while querying for WMI data: " + ex.ToString());
                return "{}";
            }
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
                Logging.Handler.Error("Device_Information.Hardware.SafeCacheSize", "An error occurred while converting cache size: ", ex.ToString());
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
                Logging.Handler.Error("Device_Information.Hardware.GetSafeProcessCount", "An error occurred while retrieving process count: ", ex.ToString());
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
                Logging.Handler.Error("Device_Information.Hardware.GetSafeThreadCount", "An error occurred while retrieving thread count: ", ex.ToString());
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
                Logging.Handler.Error("Device_Information.Hardware.GetSafeHandleCount", "An error occurred while retrieving handle count: ", ex.ToString());
            }
            return "0"; // Fallback
        }

        public static string CPU_Usage()
        {
            try
            {
                // CPU-Nutzung unter Windows
                PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                cpuCounter.NextValue(); // Ignore first measurement
                Thread.Sleep(1000); // Wait 1 second

                int cpuUsage = Convert.ToInt32(Math.Round(cpuCounter.NextValue()));

                return cpuUsage.ToString();
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Hardware.CPU_Utilization", "General error.", ex.ToString());
                return "0";
            }
        }

        // needs rework some day to support gathering of multiple RAM sticks
        public static string RAM_Information()
        {
            string ram_information_json = string.Empty;

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
                    Logging.Handler.Error("Device_Information.Hardware.RAM_Information", "RAM_Information", "An error occurred while querying for WMI data: " + ex.ToString());
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
                    Logging.Handler.Error("Device_Information.Hardware.RAM_Information", "RAM_Information", "An error occurred while querying for WMI data: " + ex.ToString());
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
                    Logging.Handler.Error("Device_Information.Hardware.RAM_Information", "RAM_Information", "An error occurred while querying for WMI data: " + ex.ToString());
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

                ram_information_json = JsonConvert.SerializeObject(ramInfo, Formatting.Indented);

                // Create and log JSON array
                Logging.Handler.Device_Information("Device_Information.Hardware.RAM_Information", "Collected the following process information.", ram_information_json);

                return ram_information_json;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Hardware.RAM_Information", "RAM_Information", "An error occurred while querying for WMI data: " + ex.ToString());
                return "{}";
            }
        }

        public static string RAM_Usage()
        {
            try
            {
                ulong totalVisibleMemorySize = ulong.Parse(WMI.Search("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem", "TotalVisibleMemorySize"));
                ulong freePhysicalMemory = ulong.Parse(WMI.Search("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem", "FreePhysicalMemory"));

                float usedMemory = totalVisibleMemorySize - freePhysicalMemory;
                float usedMemoryPercentage = ((float)usedMemory / totalVisibleMemorySize) * 100;

                int usedMemoryPercentageInt = Convert.ToInt32(Math.Round(usedMemoryPercentage));

                Logging.Handler.Device_Information("Device_Information.Hardware.RAM_Utilization", "Current RAM Usage (%)", usedMemoryPercentageInt.ToString());

                return usedMemoryPercentageInt.ToString();
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Hardware.RAM_Utilization", "General error.", ex.ToString());
                return "0";
            }
        }

        public static string Disks()
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
                                Logging.Handler.Device_Information("Device_Information.Hardware.Disks", "Failed to associate disk to partition.", ex.ToString());
                            }

                            try
                            {
                                // Get disk capacity by letter
                                DriveInfo driveInfo = new DriveInfo(letter);
                                string totalCapacityGBString = ((double)driveInfo.TotalSize / (1024 * 1024 * 1024)).ToString("0.00");
                                string usedCapacityPercentageString = $"{((double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize) * 100:F2}";

                                // Add the letter to the list of collected letters
                                collectedLettersList.Add(letter);
                                Logging.Handler.Device_Information("Device_Information.Hardware.Disks", "Collected the following disk information.", $"Letter: {letter}, Volume Label: {volume_label}, DeviceID: {DeviceID}, PNPDeviceID: {PNPDeviceID}");

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
                                string disksJson = JsonConvert.SerializeObject(diskInfo, Formatting.Indented);
                                disksJsonList.Add(disksJson);
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Device_Information("Device_Information.Hardware.Disks", "Failed to gather disk capacity.", ex.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Handler.Device_Information("Device_Information.Hardware.Disks", "Failed to collect disk information.", ex.ToString());
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
                                    string disksJson = JsonConvert.SerializeObject(diskInfo, Formatting.Indented);
                                    disksJsonList.Add(disksJson);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.Handler.Device_Information("Device_Information.Hardware.Disks", "Failed to gather basic disk information for letter.", ex.ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.Handler.Device_Information("Device_Information.Hardware.Disks", "Failed to gather basic disk information for uncollected letters (general error).", ex.ToString());
                    }

                    // Create and log JSON array
                    string disks_json = "[" + string.Join("," + Environment.NewLine, disksJsonList) + "]";
                    Logging.Handler.Device_Information("Device_Information.Hardware.Disks", "Collected the following disk information.", disks_json);
                    return disks_json;
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Hardware.Disks", "General error in Disks method.", ex.ToString());
                return "[]";
            }
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

                    Logging.Handler.Device_Information("Device_Information.Hardware.CPU_Utilization", "Current CPU Usage (%)", cpuUsage.ToString());

                    return Convert.ToInt32(Math.Round(cpuUsage));
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Hardware.CPU_Utilization", "General error.", ex.ToString());
                return 0;
            }
        }

        public static int RAM_Utilization()
        {
            try
            {
                ulong totalVisibleMemorySize = ulong.Parse(WMI.Search("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem", "TotalVisibleMemorySize"));
                ulong freePhysicalMemory = ulong.Parse(WMI.Search("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem", "FreePhysicalMemory"));

                float usedMemory = totalVisibleMemorySize - freePhysicalMemory;
                float usedMemoryPercentage = ((float)usedMemory / totalVisibleMemorySize) * 100;

                int usedMemoryPercentageInt = Convert.ToInt32(Math.Round(usedMemoryPercentage));

                Logging.Handler.Device_Information("Device_Information.Hardware.RAM_Utilization", "Current RAM Usage (%)", usedMemoryPercentageInt.ToString());           

                return usedMemoryPercentageInt;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Hardware.RAM_Utilization", "General error.", ex.ToString());
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
                    Logging.Handler.Device_Information("Device_Information.Hardware.Drive_Usage", "Total memory GB", totalSizeGB.ToString());
                    Logging.Handler.Device_Information("Device_Information.Hardware.Drive_Usage", "Free memory GB", availableFreeSpaceGB.ToString());
                    Logging.Handler.Device_Information("Device_Information.Hardware.Drive_Usage", "Memory used GB", usedSpaceGB.ToString());
                    Logging.Handler.Device_Information("Device_Information.Hardware.Drive_Usage", "Memory used %", usedSpacePercentage.ToString());

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
                    Logging.Handler.Device_Information("Device_Information.Hardware.Drive_Usage", "The drive is not ready", drive_letter.ToString());

                return 0;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Hardware.Drive_Usage", "General error.", ex.ToString());
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
                    Logging.Handler.Device_Information("Device_Information.Hardware.Drive_Usage", "Total memory GB", totalSizeGB.ToString());

                    return Convert.ToInt32(Math.Round(totalSizeGB));
                }
                else
                    Logging.Handler.Device_Information("Device_Information.Hardware.Drive_Usage", "The drive is not ready", drive_letter.ToString());

                return 0;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Device_Information.Hardware.Drive_Usage", "General error.", ex.ToString());
                return 0;
            }
        }
    }
}
