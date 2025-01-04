using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using NetLock_RMM_Agent_Comm;
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
            string path = String.Empty;

            try
            {
                Global.Initialization.Health.Check_Directories();

                Logging.PowerShell("Helper.Powershell.Execute_Script", "Trying to execute command", type + "script:" + Environment.NewLine + script);

                if (String.IsNullOrEmpty(script))
                {
                    Logging.Error("Helper.Powershell.Execute_Script", "Script is empty", "");
                    return "-";
                }

                Random random = new Random();
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                string random_id = new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
                path = Application_Paths.program_data_scripts + @"\" + Randomizer.Standard(12) + ".ps1";

                //Decode script
                byte[] script_data = Convert.FromBase64String(script);
                string decoded_script = Encoding.UTF8.GetString(script_data);

                File.WriteAllText(path, decoded_script);

                Process cmd_process = new Process();
                cmd_process.StartInfo.UseShellExecute = false;
                cmd_process.StartInfo.RedirectStandardOutput = true;
                cmd_process.StartInfo.CreateNoWindow = true; 
                cmd_process.StartInfo.FileName = "powershell.exe";
                cmd_process.StartInfo.Arguments = "-executionpolicy bypass -file \"" + path + "\"";
                cmd_process.Start();
                string result = cmd_process.StandardOutput.ReadToEnd();
                cmd_process.WaitForExit(120000);

                Logging.PowerShell("Helper.Powershell.Execute_Script", "Command execution successfully", Environment.NewLine + " Result:" + result);
                return result;
            }
            catch (Exception ex)
            {
                Logging.Error("Helper.Powershell.Execute_Script", "Failed executing script. Type: " + type + " Script: " + script, ex.ToString());
                return "Error: " + ex.ToString();
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
