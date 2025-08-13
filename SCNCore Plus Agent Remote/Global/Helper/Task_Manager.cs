using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace Global.Helper
{
    internal class Task_Manager
    {
        public static async Task<string> Terminate_Process_Tree(int pid)
        {
            try
            {
                // Get the process by its PID
                Process parentProcess = Process.GetProcessById(pid);

                // Recursively terminate all child processes
                Terminate_Child_Processes(pid);

                // Terminate the parent process
                parentProcess.Kill();

                return "Process tree terminated.";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        private static void Terminate_Child_Processes(int parentId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Terminate_Child_Processes_Windows(parentId);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Terminate_Child_Processes_Unix(parentId);
            }
            else
            {
                throw new PlatformNotSupportedException("This method only supports Windows, Linux, and macOS.");
            }
        }

        private static void Terminate_Child_Processes_Windows(int parentId)
        {
            try
            {
                var searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessId={parentId}");
                foreach (var obj in searcher.Get())
                {
                    int childProcessId = Convert.ToInt32(obj["ProcessId"]);
                    try
                    {
                        Terminate_Child_Processes_Windows(childProcessId);
                        Process childProcess = Process.GetProcessById(childProcessId);
                        childProcess.Kill();
                    }
                    catch (Exception)
                    {
                        // Ignore exceptions for already terminated processes
                    }
                }
            }
            catch (Exception)
            {
                Logging.Error("Global.Helper.Task_Manager.Terminate_Child_Processes_Windows", "Error terminating child processes", "An error occurred while terminating child processes.");
                // Handle errors gracefully for unsupported systems
            }
        }

        private static void Terminate_Child_Processes_Unix(int parentId)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "pgrep",
                    Arguments = $"-P {parentId}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                using var process = Process.Start(processStartInfo);
                if (process == null) return;

                using var reader = process.StandardOutput;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (int.TryParse(line, out int childProcessId))
                    {
                        Terminate_Child_Processes_Unix(childProcessId);
                        try
                        {
                            Process childProcess = Process.GetProcessById(childProcessId);
                            childProcess.Kill();
                        }
                        catch (Exception)
                        {
                            // Ignore exceptions for already terminated processes
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Handle errors gracefully for unsupported systems
            }
        }
    }
}
