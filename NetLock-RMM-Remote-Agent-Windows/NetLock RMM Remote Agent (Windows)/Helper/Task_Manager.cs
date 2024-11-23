using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Management;

namespace Helper
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
                TerminateChildProcesses(pid);

                // Terminate the parent process
                parentProcess.Kill();

                return "Process tree terminated.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static void TerminateChildProcesses(int parentId)
        {
            // Query all child processes based on parent process ID
            ManagementObjectSearcher searcher = new ManagementObjectSearcher($"Select * From Win32_Process Where ParentProcessId={parentId}");

            foreach (ManagementObject obj in searcher.Get())
            {
                int childProcessId = Convert.ToInt32(obj["ProcessId"]);
                try
                {
                    // Recursively terminate child processes
                    TerminateChildProcesses(childProcessId);

                    // Kill the child process
                    Process childProcess = Process.GetProcessById(childProcessId);
                    childProcess.Kill();
                }
                catch (Exception)
                {
                    // Ignore any exceptions for already terminated processes
                }
            }
        }
    }
}
