using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using NetLock_RMM_Agent_Remote;
using Global.Initialization;
using Global.Helper;

namespace Windows.Helper
{
    internal class PowerShell
    {
        public static string Execute_Command(string type, string command, int timeout) // -1 = no timeout
        {
            try
            {
                Logging.PowerShell("Helper.Powershell.Execute_Command", "Trying to execute command", "type: " + type + " timeout: " + timeout + " command:" + Environment.NewLine + command);

                string result = "";

                Process cmd_process = new Process();
                cmd_process.StartInfo.UseShellExecute = false;
                cmd_process.StartInfo.RedirectStandardOutput = true;
                cmd_process.StartInfo.CreateNoWindow = true;
                cmd_process.StartInfo.FileName = "cmd.exe";
                cmd_process.StartInfo.Arguments = "/c powershell " + command;
                cmd_process.Start();
                result = cmd_process.StandardOutput.ReadToEnd();
                cmd_process.WaitForExit(timeout);

                Logging.PowerShell("Helper.Powershell.Execute_Command", "Command execution successfully", Environment.NewLine + " Result:" + result);

                return result;
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Powershell.Execute_Command", "Command execution failed", ex.ToString());
                return "Error.";
            }
        }

        public static string Execute_Script(string type, string script) // script must be base64 encoded
        {
            try
            {
                Health.Check_Directories();

                Logging.PowerShell("Helper.Powershell.Execute_Script", "Trying to execute command", type + " script length: " + script.Length);

                if (string.IsNullOrWhiteSpace(script))
                {
                    Logging.Error("Helper.Powershell.Execute_Script", "Script is empty", "");
                    return "-";
                }

                using Process cmdProcess = new Process();

                cmdProcess.StartInfo.FileName = "powershell.exe";
                cmdProcess.StartInfo.Arguments = $"-ExecutionPolicy Bypass -EncodedCommand {script}";

                cmdProcess.StartInfo.UseShellExecute = false;
                cmdProcess.StartInfo.RedirectStandardOutput = true;
                cmdProcess.StartInfo.RedirectStandardError = true;
                cmdProcess.StartInfo.RedirectStandardInput = true;
                cmdProcess.StartInfo.CreateNoWindow = true;

                cmdProcess.Start();

                // Close standard input immediately to prevent hanging on input requests
                cmdProcess.StandardInput.Close();

                // Capture the streams before they can be disposed
                var stdout = cmdProcess.StandardOutput;
                var stderr = cmdProcess.StandardError;

                // Use async reading with timeout to prevent hanging
                var outputTask = Task.Run(() => stdout.ReadToEnd());
                var errorTask = Task.Run(() => stderr.ReadToEnd());

                // Wait for process to exit with timeout (5 minutes)
                const int timeoutMs = 300000; // 5 minutes
                bool processExited = cmdProcess.WaitForExit(timeoutMs);

                string output = "";
                string error = "";

                if (processExited)
                {
                    // Process exited normally, get the results
                    try
                    {
                        if (outputTask.Wait(5000)) // Wait max 5 seconds for output reading
                            output = outputTask.Result;
                        else
                            output = "Output reading timed out";
                    }
                    catch (Exception)
                    {
                        output = "Error reading output";
                    }

                    try
                    {
                        if (errorTask.Wait(5000)) // Wait max 5 seconds for error reading
                            error = errorTask.Result;
                        else
                            error = "Error reading timed out";
                    }
                    catch (Exception)
                    {
                        error = "Error reading stderr";
                    }
                }
                else
                {
                    // Process didn't exit within timeout, kill it
                    try
                    {
                        cmdProcess.Kill();
                        cmdProcess.WaitForExit(5000); // Give it 5 seconds to clean up
                    }
                    catch (Exception)
                    {
                        // Ignore errors when killing the process
                    }

                    return "Error: Script execution timed out (5 minutes). The script may have been waiting for user input or was stuck in an infinite loop.";
                }

                // Log the output and error
                if (!string.IsNullOrEmpty(error))
                {
                    Logging.PowerShell("Helper.Powershell.Execute_Script", "Script error output", error);
                    return "Output: " + Environment.NewLine + output + Environment.NewLine + Environment.NewLine + "More output: " + Environment.NewLine + error;
                }
                else
                {
                    Logging.PowerShell("Helper.Powershell.Execute_Script", "Command executed successfully", Environment.NewLine + "Result:" + output);
                    return output;
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Powershell.Execute_Script", "Failed executing script. Type: " + type + " Script length: " + (script?.Length ?? 0), ex.ToString());
                return "Error: " + ex.Message;
            }
        }
    }
}
