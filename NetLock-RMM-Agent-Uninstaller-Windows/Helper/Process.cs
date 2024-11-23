using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;


namespace NetLock_RMM_Agent_Uninstaller_Windows.Helper
{
    internal class _Process
    {
        public static bool Start(string file_name, string command)
        {
            //Start process
            try
            {
                Logging.Handler.Debug("Helper._Process.Start", "Start process.", command);
                Console.WriteLine("[" + DateTime.Now + "] - [Helper._Process.Start] -> Starting process: " + command);

                Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = file_name; // example: cmd.exe  
                process.StartInfo.Arguments = command; // example: /c taskkill /F /IM "NetLock RMM Comm Agent (Windows).exe"
                process.Start();
                process.WaitForExit();

                Logging.Handler.Debug("Helper._Process.Start", "Start process.", "Done.");
                Console.WriteLine("[" + DateTime.Now + "] - [Helper._Process.Start] -> Start process: Done.");

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper._Process.Start", "Starting process failed: " + file_name + " -> " + command, ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Helper._Process.Start] -> Starting process failed: " + ex.Message);
                return false;
            }
        }
    }
}
