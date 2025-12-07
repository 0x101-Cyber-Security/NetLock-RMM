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
        
        public static string Execute_Script(string type, string script, int timeout = 360) // timeout in minutes
        {
            string path = String.Empty;
            Process cmd_process = null;

            try
            {
                Global.Initialization.Health.Check_Directories();

                Logging.PowerShell("Helper.Powershell.Execute_Script", "Trying to execute script", type);

                if (String.IsNullOrEmpty(script))
                {
                    Logging.Error("Helper.Powershell.Execute_Script", "Script is empty", "");
                    return "Error: Script is empty";
                }

                // Validate base64
                byte[] script_data;
                string decoded_script;
                
                try
                {
                    script_data = Convert.FromBase64String(script);
                    decoded_script = Encoding.UTF8.GetString(script_data);
                }
                catch (FormatException ex)
                {
                    Logging.Error("Helper.Powershell.Execute_Script", "Invalid Base64 script", ex.Message);
                    return "Error: Invalid Base64 encoding";
                }

                if (String.IsNullOrWhiteSpace(decoded_script))
                {
                    Logging.Error("Helper.Powershell.Execute_Script", "Decoded script is empty", "");
                    return "Error: Decoded script is empty";
                }

                // Create temporary script file
                path = Path.Combine(Application_Paths.program_data_scripts, Guid.NewGuid() + ".ps1");
                File.WriteAllText(path, decoded_script);

                // Execute script
                cmd_process = new Process();
                cmd_process.StartInfo.UseShellExecute = false;
                cmd_process.StartInfo.RedirectStandardOutput = true;
                cmd_process.StartInfo.RedirectStandardError = true;
                cmd_process.StartInfo.CreateNoWindow = true; 
                cmd_process.StartInfo.FileName = "powershell.exe";
                cmd_process.StartInfo.Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + path + "\"";
                
                // Use StringBuilder for better performance when reading large outputs
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                // Asynchronous output reading to avoid deadlocks
                cmd_process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        output.AppendLine(e.Data);
                };

                cmd_process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        error.AppendLine(e.Data);
                };

                cmd_process.Start();
                cmd_process.BeginOutputReadLine();
                cmd_process.BeginErrorReadLine();

                // Handle timeout
                timeout = timeout * 1000 * 60; // Convert minutes to milliseconds
                
                bool exited = cmd_process.WaitForExit(timeout);

                if (!exited)
                {
                    // Process didn't finish in time, kill it
                    try
                    {
                        cmd_process.Kill(true); // Kill entire process tree
                        Logging.Error("Helper.Powershell.Execute_Script", "Script execution timed out", $"Timeout: {timeout}ms");
                        return $"Error: Script execution timed out after {timeout}ms";
                    }
                    catch (Exception killEx)
                    {
                        Logging.Error("Helper.Powershell.Execute_Script", "Failed to kill timed out process", killEx.ToString());
                    }
                }

                // Wait for async output reading to complete
                cmd_process.WaitForExit();

                string result = output.ToString();
                string errorOutput = error.ToString();

                if (!String.IsNullOrWhiteSpace(errorOutput))
                {
                    Logging.Error("Helper.Powershell.Execute_Script", "Script produced error output", errorOutput);
                    result += Environment.NewLine + "STDERR: " + errorOutput;
                }

                int exitCode = cmd_process.ExitCode;
                if (exitCode != 0)
                {
                    Logging.Error("Helper.Powershell.Execute_Script", $"Script exited with code {exitCode}", errorOutput);
                    result += Environment.NewLine + $"Exit Code: {exitCode}";
                }
                else
                {
                    Logging.PowerShell("Helper.Powershell.Execute_Script", "Script execution successful", $"Exit code: {exitCode}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Powershell.Execute_Script", "Failed executing script. Type: " + type, ex.ToString());
                return "Error: " + ex.Message;
            }
            finally
            {
                // Ensure process is disposed
                if (cmd_process != null)
                {
                    try
                    {
                        if (!cmd_process.HasExited)
                            cmd_process.Kill(true);
                        
                        cmd_process.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }

                // Clean up temporary script file
                if (!String.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception deleteEx)
                    {
                        Logging.Error("Helper.Powershell.Execute_Script", "Failed to delete temporary script file", deleteEx.ToString());
                    }
                }
            }
        }
    }
}
