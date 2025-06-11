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

        public static string Execute_Script(string type, string script)
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

                // Decode script from Base64
                byte[] script_data = Convert.FromBase64String(script);
                string decoded_script = Encoding.UTF8.GetString(script_data);

                // Normalize line endings to Windows style (optional)
                decoded_script = decoded_script.Replace("\r\n", "\n").Replace("\n", "\r\n");

                using Process cmd_process = new Process();

                cmd_process.StartInfo.FileName = "powershell.exe";
                // -Command - reads the script from StandardInput
                cmd_process.StartInfo.Arguments = "-ExecutionPolicy Bypass -Command -";

                cmd_process.StartInfo.UseShellExecute = false;
                cmd_process.StartInfo.RedirectStandardInput = true;
                cmd_process.StartInfo.RedirectStandardOutput = true;
                cmd_process.StartInfo.RedirectStandardError = true;
                cmd_process.StartInfo.CreateNoWindow = true;

                cmd_process.Start();

                // Skript per StandardInput an PowerShell senden
                using (StreamWriter writer = cmd_process.StandardInput)
                {
                    writer.Write(decoded_script);
                }

                // Read output and error (blocking until process ends)
                string output = cmd_process.StandardOutput.ReadToEnd();
                string error = cmd_process.StandardError.ReadToEnd();

                // Wait for process end, max. 1 day (86400000 ms)
                bool exited = cmd_process.WaitForExit(86400000);
                if (!exited)
                {
                    try { cmd_process.Kill(); } catch { }
                    throw new TimeoutException("The script took too long and was canceled.");
                }

                // Log the output and error
                if (!string.IsNullOrEmpty(error))
                {
                    Logging.PowerShell("Helper.Powershell.Execute_Script", "Script error output", error);
                    return "Output: " + Environment.NewLine + output + Environment.NewLine + Environment.NewLine + "Error output: " + Environment.NewLine + error;
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
