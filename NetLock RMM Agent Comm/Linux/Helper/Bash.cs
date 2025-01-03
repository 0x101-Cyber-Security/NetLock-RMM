using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Global.Helper;
using NetLock_RMM_Agent_Comm;

namespace Linux.Helper
{
    internal class Bash
    {
        public static string Execute_Command(string command)
        {
            try
            {
                // Create a new process
                Process process = new Process();
                // Set the process start information
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{command}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                // Start the process
                process.Start();
                // Read the output and error
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                // Wait for the process to exit
                process.WaitForExit();
                // Log and return the output
                if (!string.IsNullOrEmpty(error))
                {
                    //Console.Error.WriteLine($"Error executing command: {error}");
                    Logging.Error("Linux.Helper.Bash.Execute_Command", "Error executing command", error);
                    return "-";
                }
                else
                {
                    return output;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Linux.Helper.Bash.Execute_Command", "Error executing command", ex.ToString());
                //Console.Error.WriteLine($"Error executing command: {ex.ToString()}");
                return "-";
            }
        }

        public static string Execute_Script(string type, string script)
        {
            try
            {
                Logging.Debug("Linux.Helper.Bash.Execute_Script", "Executing script", "type: " + type + " script: " + script);

                // Create a new process
                Process process = new Process();
                // Set the process start information
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{Application_Paths.program_data_scripts}/{script}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                // Start the process
                process.Start();
                // Read the output and error
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                // Wait for the process to exit
                process.WaitForExit();
                // Log and return the output
                if (!string.IsNullOrEmpty(error))
                {
                    //Console.Error.WriteLine($"Error exec  uting command: {error}");
                    Logging.Error("Linux.Helper.Bash.Execute_Script", "Error executing script", error);
                    return "-";
                }
                else
                {
                    return output;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Linux.Helper.Bash.Execute_Script", "Error executing script", ex.ToString());
                //Console.Error.WriteLine($"Error executing command: {ex.ToString()}");
                return "-";
            }
        }
    }
}
