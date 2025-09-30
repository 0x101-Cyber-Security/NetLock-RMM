using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Global.Helper;
using Global.Initialization;

namespace Linux.Helper
{
    internal class Bash
    {
        public static string Execute_Script(string type, bool decode, string script)
        {
            try
            {
                Health.Check_Directories();

                Logging.Debug("Linux.Helper.Bash.Execute_Script", "Executing script", $"type: {type}, script length: {script.Length}");

                if (String.IsNullOrEmpty(script))
                {
                    Logging.Error("Linux.Helper.Bash.Execute_Script", "Script is empty", "");
                    return "-";
                }

                // Decode the script from Base64
                if (decode)
                {
                    byte[] script_data = Convert.FromBase64String(script);
                    string decoded_script = Encoding.UTF8.GetString(script_data);

                    // Convert Windows line endings (\r\n) to Unix line endings (\n)
                    script = decoded_script.Replace("\r\n", "\n");

                    Logging.Debug("Linux.Helper.Bash.Execute_Script", "Decoded script", script);
                }

                // Create a new process
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = "-s"; // Read script from standard input
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    // Start the process
                    process.Start();

                    // Write the cleaned script to the process's standard input and close it immediately
                    using (StreamWriter writer = process.StandardInput)
                    {
                        writer.Write(script);
                    }
                    // StandardInput is automatically closed when the StreamWriter is disposed

                    // Capture the streams before they can be disposed
                    var stdout = process.StandardOutput;
                    var stderr = process.StandardError;

                    // Use async reading with timeout to prevent hanging
                    var outputTask = Task.Run(() => stdout.ReadToEnd());
                    var errorTask = Task.Run(() => stderr.ReadToEnd());

                    // Wait for process to exit with timeout (5 minutes instead of 1 day)
                    const int timeoutMs = 300000; // 5 minutes
                    bool processExited = process.WaitForExit(timeoutMs);

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
                            process.Kill();
                            process.WaitForExit(5000); // Give it 5 seconds to clean up
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
                        Logging.PowerShell("Linux.Helper.Bash.Execute_Script", "Script error output", error);
                        return "Output: " + Environment.NewLine + output + Environment.NewLine + Environment.NewLine + "More output: " + Environment.NewLine + error;
                    }
                    else
                    {
                        Logging.PowerShell("Linux.Helper.Bash.Execute_Script", "Command executed successfully", Environment.NewLine + "Result:" + output);
                        return output;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Error("Linux.Helper.Bash.Execute_Script", "Error executing script", ex.ToString());
                return ex.Message;
            }
        }
    }
}
