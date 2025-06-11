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

                    // Write the cleaned script to the process's standard input
                    using (StreamWriter writer = process.StandardInput)
                    {
                        writer.Write(script);
                    }

                    // Read the output and error
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Wait for process end, max. 1 day (86400000 ms)
                    bool exited = process.WaitForExit(86400000);
                    if (!exited)
                    {
                        try { process.Kill(); } catch { }
                        throw new TimeoutException("The script took too long and was canceled.");
                    }

                    // Log the output and error
                    if (!string.IsNullOrEmpty(error))
                    {
                        Logging.PowerShell("Linux.Helper.Bash.Execute_Script", "Script error output", error);
                        return "Output: " + Environment.NewLine + output + Environment.NewLine + Environment.NewLine + "Error output: " + Environment.NewLine + error;
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
