using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace NetLock_RMM_Agent_Uninstaller_Windows
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool fix = false;
            bool clean = false;

            //Check if it should be fixed
            try
            {
                if (args[0].Contains("fix"))
                    fix = true;
                else if (args[0].Contains("clean"))
                    clean = true;
            }
            catch
            {

            }

            Console.Title = "NetLock RMM Agent Uninstaller Windows";
            Console.ForegroundColor = ConsoleColor.Red;
            string title = @"//
  _   _      _   _                _      _____  __  __ __  __ 
 | \ | |    | | | |              | |    |  __ \|  \/  |  \/  |
 |  \| | ___| |_| |     ___   ___| | __ | |__) | \  / | \  / |
 | . ` |/ _ \ __| |    / _ \ / __| |/ / |  _  /| |\/| | |\/| |
 | |\  |  __/ |_| |___| (_) | (__|   <  | | \ \| |  | | |  | |
 |_| \_|\___|\__|______\___/ \___|_|\_\ |_|  \_\_|  |_|_|  |_|                                                              
";

            Console.WriteLine(title);
            Console.ForegroundColor = ConsoleColor.White;

            if (fix)
            {
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Fix mode.");

                // Stop services
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Stopping services.");
                Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Comm_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Comm_Agent_Windows");
                Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Remote_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Remote_Agent_Windows");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services stopped.");

                // Kill processes
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminating processes.");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM Comm Agent (Windows).exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM Comm Agent (Windows).exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM Remote Agent (Windows).exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM Remote Agent (Windows).exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM User Process.exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM User Process.exe\"");

                //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"yara64.exe\""); // yara64.exe is (currently) not used in the project, its part of a netlock legacy feature
                //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"devcon_x64.exe\""); // devcon_x64.exe is (currently) not used in the project, its part of a netlock legacy feature
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminated processes.");

                // Delete services
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting services.");
                Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Comm_Agent_Windows");
                Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Comm_Agent_Windows");
                Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Remote_Agent_Windows");
                Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Remote_Agent_Windows");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services deleted.");

                // Delete files
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting files.");
                Logging.Handler.Debug("Main", "Deleting files.", Application_Paths.program_data_comm_agent_events_path);
                Helper.IO.Delete_File(Application_Paths.program_data_comm_agent_events_path);
                Logging.Handler.Debug("Main", "Deleting files.", Application_Paths.program_data_comm_agent_policies_path);
                Helper.IO.Delete_File(Application_Paths.program_data_comm_agent_policies_path);
                Logging.Handler.Debug("Main", "Deleting files.", Application_Paths.program_data_comm_agent_server_config_json_path);
                Helper.IO.Delete_File(Application_Paths.program_data_comm_agent_version_path);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleted files.");

                // Delete directories
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting directories.");
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_files_comm_agent_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_files_comm_agent_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_logs_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_logs_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_jobs_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_jobs_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_msdav_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_msdav_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_scripts_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_scripts_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_backups_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_backups_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_sensors_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_sensors_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_dumps_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_dumps_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_temp_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_temp_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_files_remote_agent_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_files_remote_agent_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_remote_agent_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_remote_agent_dir);

                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleted directories.");
            }
            else if (clean)
            {
                // Stop services
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Stopping services.");
                Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Comm_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Comm_Agent_Windows");
                Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Remote_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Remote_Agent_Windows");
                Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Health_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Health_Agent_Windows");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services stopped.");

                // Kill processes
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminating processes.");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM Comm Agent (Windows).exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM Comm Agent (Windows).exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM Remote Agent (Windows).exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM Remote Agent (Windows).exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM Health Agent (Windows).exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM Health Agent (Windows).exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM User Process.exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM User Process.exe\"");
                //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"yara64.exe\""); // yara64.exe is (currently) not used in the project, its part of a netlock legacy feature
                //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"devcon_x64.exe\""); // devcon_x64.exe is (currently) not used in the project, its part of a netlock legacy feature
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminated processes.");

                // Delete services
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting services.");
                Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Comm_Agent_Windows");
                Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Comm_Agent_Windows");
                Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Remote_Agent_Windows");
                Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Remote_Agent_Windows");
                Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Health_Agent_Windows");
                Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Health_Agent_Windows");
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services deleted.");

                // Delete directories
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting directories.");
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_files_0x101_cyber_security_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_files_0x101_cyber_security_dir);
                Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_0x101_cyber_security_dir);
                Helper.IO.Delete_Directory(Application_Paths.program_data_0x101_cyber_security_dir);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Directories deleted.");
            }
            else
            {
                Console.WriteLine("Invalid argument. Please pass either fix or clean. See in docs.");
                Thread.Sleep(5000);
            }

            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Done.");
        }
    }
}
