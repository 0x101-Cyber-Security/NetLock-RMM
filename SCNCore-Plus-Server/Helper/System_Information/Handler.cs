using System.IO;
using System.Diagnostics;
using System.Management;

namespace Helper.System_Information
{
    public class Handler
    {
        public static async Task<int> Get_CPU_Usage()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Check whether the operating system is Windows
                    if (OperatingSystem.IsWindows())
                    {
                        // CPU-Nutzung unter Windows
                        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                        cpuCounter.NextValue(); // Ignore first measurement
                        Thread.Sleep(1000); // Wait 1 second
                        return (int)Math.Round(cpuCounter.NextValue());
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        // CPU utilisation under Linux
                        return GetLinuxCpuUsage();
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    return 0;
                }
            });
        }

        // Auxiliary method for retrieving the CPU utilisation under Linux
        private static int GetLinuxCpuUsage()
        {
            try
            {
                var lines = System.IO.File.ReadAllLines("/proc/stat");
                if (lines.Length > 0)
                {
                    var cpuInfo = lines[0].Split(' ');
                    var user = Convert.ToDouble(cpuInfo[2]);
                    var nice = Convert.ToDouble(cpuInfo[3]);
                    var system = Convert.ToDouble(cpuInfo[4]);
                    var idle = Convert.ToDouble(cpuInfo[5]);

                    var totalCpuTime = user + nice + system + idle;
                    var idleCpuTime = idle;

                    var cpuUsage = (totalCpuTime - idleCpuTime) / totalCpuTime * 100;
                    return (int)Math.Round(cpuUsage); // Umwandlung in int
                }
            }
            catch (Exception ex)
            {
                return 0;
            }

            return 0;
        }

        public static async Task<int> Get_RAM_Usage()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Check whether the operating system is Windows
                    if (OperatingSystem.IsWindows())
                    {
                        // Get RAM usage in percentage
                        var totalMemory = GetTotalMemory();
                        var availableMemory = GetAvailableMemory();
                        var usedMemory = totalMemory - availableMemory;

                        var ramUsage = usedMemory / totalMemory * 100;

                        return (int)Math.Round(ramUsage);
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        // RAM-Nutzung unter Linux
                        return GetLinuxRamUsage();
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    // Fehlerbehandlung
                    return 0;
                }
            });
        }

        // Auxiliary method for retrieving the total RAM under Windows
        private static double GetTotalMemory()
        {
            try
            {
                var query = new ObjectQuery("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                var searcher = new ManagementObjectSearcher(query);
                var collection = searcher.Get();

                foreach (var item in collection)
                {
                    return Convert.ToDouble(item["TotalVisibleMemorySize"]);
                }
            }
            catch (Exception ex)
            {
                return 0;
            }

            return 0;
        }

        // Auxiliary method for retrieving the available RAM under Windows
        private static double GetAvailableMemory()
        {
            try
            {
                var query = new ObjectQuery("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
                var searcher = new ManagementObjectSearcher(query);
                var collection = searcher.Get();

                foreach (var item in collection)
                {
                    return Convert.ToDouble(item["FreePhysicalMemory"]);
                }
            }
            catch (Exception ex)
            {
                return 0;
            }

            return 0;
        }

        // Auxiliary method for retrieving the RAM utilisation under Linux
        private static int GetLinuxRamUsage()
        {
            try
            {
                var lines = System.IO.File.ReadAllLines("/proc/meminfo");
                if (lines.Length > 0)
                {
                    var totalMemory = Convert.ToDouble(lines[0].Split(' ')[1]);
                    var freeMemory = Convert.ToDouble(lines[1].Split(' ')[1]);
                    var usedMemory = totalMemory - freeMemory;

                    var ramUsage = usedMemory / totalMemory * 100;
                    return (int)Math.Round(ramUsage);
                }
            }
            catch (Exception ex)
            {
                return 0;
            }

            return 0;
        }

        public static async Task<int> Get_Disk_Usage()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Check whether the operating system is Windows
                    if (OperatingSystem.IsWindows())
                    {
                        // Get disk usage in percentage
                        var totalDiskSpace = GetTotalDiskSpace();
                        var availableDiskSpace = GetAvailableDiskSpace();
                        var usedDiskSpace = totalDiskSpace - availableDiskSpace;

                        var diskUsage = usedDiskSpace / totalDiskSpace * 100;

                        return (int)Math.Round(diskUsage);
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        // Festplatten-Nutzung unter Linux
                        return GetLinuxDiskUsage();
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    // Fehlerbehandlung
                    return 0;
                }
            });
        }

        // Auxiliary method for retrieving the total disk space under Windows
        private static double GetTotalDiskSpace()
        {
            try
            {
                var query = new ObjectQuery("SELECT Size FROM Win32_LogicalDisk WHERE DeviceID = 'C:'");
                var searcher = new ManagementObjectSearcher(query);
                var collection = searcher.Get();

                foreach (var item in collection)
                {
                    return Convert.ToDouble(item["Size"]);
                }
            }
            catch (Exception ex)
            {
                return 0;
            }

            return 0;
        }

        // Auxiliary method for retrieving the available disk space under Windows
        private static double GetAvailableDiskSpace()
        {
            try
            {
                var query = new ObjectQuery("SELECT FreeSpace FROM Win32_LogicalDisk WHERE DeviceID = 'C:'");
                var searcher = new ManagementObjectSearcher(query);
                var collection = searcher.Get();

                foreach (var item in collection)
                {
                    return Convert.ToDouble(item["FreeSpace"]);
                }
            }
            catch (Exception ex)
            {
                return 0;
            }

            return 0;
        }

        // Auxiliary method for retrieving the disk utilisation under Linux
        private static int GetLinuxDiskUsage()
        {
            try
            {
                var lines = System.IO.File.ReadAllLines("/proc/mounts");
                if (lines.Length > 0)
                {
                    var rootPartition = lines.FirstOrDefault(l => l.Contains(" / "));
                    if (rootPartition != null)
                    {
                        var rootPartitionPath = rootPartition.Split(' ')[1];
                        var rootPartitionInfo = new DriveInfo(rootPartitionPath);

                        var totalDiskSpace = rootPartitionInfo.TotalSize;
                        var availableDiskSpace = rootPartitionInfo.AvailableFreeSpace;
                        var usedDiskSpace = totalDiskSpace - availableDiskSpace;

                        double diskUsage = (double)usedDiskSpace / totalDiskSpace * 100; // Explicitly cast to double
                        return (int)Math.Round(diskUsage); // Ensure Math.Round treats it as double
                    }
                }
            }
            catch (Exception ex)
            {
                // Optional: Log the exception if needed
                return 0;
            }

            return 0;
        }
    }
}
