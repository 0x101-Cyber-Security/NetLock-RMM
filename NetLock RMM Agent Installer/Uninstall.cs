using Helper;
using MacOS.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Agent_Installer
{
    internal class Uninstall
    {
        public static void Clean()
        {
            // Stop services
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Stopping services.");
            if (OperatingSystem.IsWindows())
            {
                Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Agent_Comm");
                Helper.Service.Stop("NetLock_RMM_Agent_Comm");
                Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Agent_Remote");
                Helper.Service.Stop("NetLock_RMM_Agent_Remote");
                Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Agent_Health");
                Helper.Service.Stop("NetLock_RMM_Agent_Health");

                // For legacy installations (2.0.0.0)
                Logging.Handler.Debug("Main", "Stopping services (legacy).", "NetLock_RMM_Comm_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Comm_Agent_Windows");
                Logging.Handler.Debug("Main", "Stopping services (legacy).", "NetLock_RMM_Health_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Health_Agent_Windows");
                Logging.Handler.Debug("Main", "Stopping services (legacy).", "NetLock_RMM_Remote_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Remote_Agent_Windows");
            }
            else if (OperatingSystem.IsLinux())
            {
                Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-comm");
                Bash.Execute_Script("Stopping services", false, "systemctl stop netlock-rmm-agent-comm");
                Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-remote");
                Bash.Execute_Script("Stopping services", false, "systemctl stop netlock-rmm-agent-remote");
                Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-health");
                Bash.Execute_Script("Stopping services", false, "systemctl stop netlock-rmm-agent-health");
            }
            else if (OperatingSystem.IsMacOS())
            {
                Logging.Handler.Debug("Main", "Stopping services.", Application_Paths.program_files_comm_agent_service_name_osx);
                Zsh.Execute_Script("Stopping services", false, $"launchctl stop {Application_Paths.program_files_comm_agent_service_name_osx}");
                Logging.Handler.Debug("Main", "Stopping services.", Application_Paths.program_files_remote_agent_service_name_osx);
                Zsh.Execute_Script("Stopping services", false, $"launchctl stop {Application_Paths.program_files_remote_agent_service_name_osx}");
                Logging.Handler.Debug("Main", "Stopping services.", Application_Paths.program_files_health_agent_service_name_osx);
                Zsh.Execute_Script("Stopping services", false, $"launchctl stop {Application_Paths.program_files_health_agent_service_name_osx}");
            }
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services stopped.");

            // Kill processes
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminating processes.");
            if (OperatingSystem.IsWindows())
            {
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Comm.exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock_RMM_Agent_Comm.exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Remote.exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock_RMM_Agent_Remote.exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Health.exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock_RMM_Agent_Health.exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_User_Process.exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock_RMM_User_Process.exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM User Process.exe"); // kill legacy process
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM User Process.exe\""); // kill legacy process
                //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"yara64.exe\""); // yara64.exe is (currently) not used in the project, its part of a netlock legacy feature
                //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"devcon_x64.exe\""); // devcon_x64.exe is (currently) not used in the project, its part of a netlock legacy feature
            }
            else if (OperatingSystem.IsLinux())
            {
                Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-comm");
                Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-comm");
                Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-remote");
                Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-remote");
                Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-health");
                Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-health");
                Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-user-process");
                Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-user-process");
            }
            else if (OperatingSystem.IsMacOS())
            {
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Comm");
                Zsh.Execute_Script("Terminating processes", false, "pkill NetLock_RMM_Agent_Comm");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Remote");
                Zsh.Execute_Script("Terminating processes", false, "pkill NetLock_RMM_Agent_Remote");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Health");
                Zsh.Execute_Script("Terminating processes", false, "pkill NetLock_RMM_Agent_Health");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_User_Process");
                Zsh.Execute_Script("Terminating processes", false, "pkill NetLock_RMM_User_Process");
            }
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminated processes.");

            // Delete services
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting services.");
            if (OperatingSystem.IsWindows())
            {
                Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Agent_Comm");
                Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Agent_Comm");
                Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Agent_Remote");
                Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Agent_Remote");
                Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Agent_Health");
                Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Agent_Health");

                // Unregister user process
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Unregistering user process.");
                Logging.Handler.Debug("Main", "Unregistering user process", "NetLock RMM User Process");
                Windows.Helper.Registry.HKLM_Delete_Value(Application_Paths.hklm_run_directory_reg_path, "NetLock RMM User Process");
            }
            else if (OperatingSystem.IsLinux())
            {
                Logging.Handler.Debug("Main", "Deleting services.", Application_Paths.program_files_comm_agent_service_name_linux);
                Bash.Execute_Script("Stopping service", false, $"systemctl stop {Application_Paths.program_files_comm_agent_service_name_linux} || true");
                Bash.Execute_Script("Disabling service", false, $"systemctl disable {Application_Paths.program_files_comm_agent_service_name_linux} || true");
                Bash.Execute_Script("Removing service file", false, $"rm -f /etc/systemd/system/{Application_Paths.program_files_comm_agent_service_name_linux}.service");

                Logging.Handler.Debug("Main", "Deleting services.", Application_Paths.program_files_remote_agent_service_name_linux);
                Bash.Execute_Script("Stopping service", false, $"systemctl stop {Application_Paths.program_files_remote_agent_service_name_linux} || true");
                Bash.Execute_Script("Disabling service", false, $"systemctl disable {Application_Paths.program_files_remote_agent_service_name_linux} || true");
                Bash.Execute_Script("Removing service file", false, $"rm -f /etc/systemd/system/{Application_Paths.program_files_remote_agent_service_name_linux}.service");

                Logging.Handler.Debug("Main", "Deleting services.", Application_Paths.program_files_health_agent_service_name_linux);
                Bash.Execute_Script("Stopping service", false, $"systemctl stop {Application_Paths.program_files_health_agent_service_name_linux} || true");
                Bash.Execute_Script("Disabling service", false, $"systemctl disable {Application_Paths.program_files_health_agent_service_name_linux} || true");
                Bash.Execute_Script("Removing service file", false, $"rm -f /etc/systemd/system/{Application_Paths.program_files_health_agent_service_name_linux}.service");

                // Reload Systemd to remove the deleted services
                Bash.Execute_Script("Reloading systemd", false, "systemctl daemon-reload");
            }
            else if (OperatingSystem.IsMacOS())
            {
                // Unload the service
                Logging.Handler.Debug("Main", "Unload service.", Application_Paths.program_files_comm_agent_service_config_path_osx);
                Zsh.Execute_Script("Unload service", false, $"launchctl unload {Application_Paths.program_files_comm_agent_service_config_path_osx}");

                // Delete the service file
                Logging.Handler.Debug("Main", "Deleting service file.", Application_Paths.program_files_comm_agent_service_config_path_osx);
                Zsh.Execute_Script("Deleting service file", false, $"rm -f {Application_Paths.program_files_comm_agent_service_config_path_osx}");

                // Unload the service
                Logging.Handler.Debug("Main", "Unload service.", Application_Paths.program_files_remote_agent_service_config_path_osx);
                Zsh.Execute_Script("Unload service", false, $"launchctl unload {Application_Paths.program_files_remote_agent_service_config_path_osx}");

                // Delete the service file
                Logging.Handler.Debug("Main", "Deleting service file.", Application_Paths.program_files_remote_agent_service_config_path_osx);
                Zsh.Execute_Script("Deleting service file", false, $"rm -f {Application_Paths.program_files_remote_agent_service_config_path_osx}");

                // Unload the service
                Logging.Handler.Debug("Main", "Unload service.", Application_Paths.program_files_health_agent_service_config_path_osx);
                Zsh.Execute_Script("Unload service", false, $"launchctl unload {Application_Paths.program_files_health_agent_service_config_path_osx}");

                // Delete the service file
                Logging.Handler.Debug("Main", "Deleting service file.", Application_Paths.program_files_health_agent_service_config_path_osx);
                Zsh.Execute_Script("Deleting service file", false, $"rm -f {Application_Paths.program_files_health_agent_service_config_path_osx}");
            }
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services deleted.");

            // Delete directories & logs
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting directories.");
            Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_files_0x101_cyber_security_dir);
            Helper.IO.Delete_Directory(Application_Paths.program_files_0x101_cyber_security_dir);
            Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_0x101_cyber_security_dir);
            Helper.IO.Delete_Directory(Application_Paths.program_data_0x101_cyber_security_dir);

            // Delete logs
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // Delete comm agent service log
                Logging.Handler.Debug("Main", "Deleting comm agent service log.", Application_Paths.program_files_comm_agent_service_log_path_unix);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting comm agent service log: " + Application_Paths.program_files_comm_agent_service_log_path_unix);
                Bash.Execute_Script("Deleting comm agent service log", false, $"rm -f {Application_Paths.program_files_comm_agent_service_log_path_unix}");

                // Delete remote agent service log
                Logging.Handler.Debug("Main", "Deleting remote agent service log.", Application_Paths.program_files_remote_agent_service_log_path_unix);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting remote agent service log: " + Application_Paths.program_files_remote_agent_service_log_path_unix);
                Bash.Execute_Script("Deleting remote agent service log", false, $"rm -f {Application_Paths.program_files_remote_agent_service_log_path_unix}");

                // Delete health agent service log
                Logging.Handler.Debug("Main", "Deleting health agent service log.", Application_Paths.program_files_health_agent_service_log_path_unix);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting health agent service log: " + Application_Paths.program_files_health_agent_service_log_path_unix);
                Bash.Execute_Script("Deleting health agent service log", false, $"rm -f {Application_Paths.program_files_health_agent_service_log_path_unix}");
            }

            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Directories deleted.");
        }

        public static void Fix()
        {
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Fix mode.");

            // Stop services
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Stopping services.");
            if (OperatingSystem.IsWindows())
            {
                Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Agent_Comm");
                Helper.Service.Stop("NetLock_RMM_Agent_Comm");
                Logging.Handler.Debug("Main", "Stopping services.", "NetLock_RMM_Agent_Remote");
                Helper.Service.Stop("NetLock_RMM_Agent_Remote");

                // For legacy installations (2.0.0.0)
                Logging.Handler.Debug("Main", "Stopping services (legacy).", "NetLock_RMM_Comm_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Comm_Agent_Windows");
                Logging.Handler.Debug("Main", "Stopping services (legacy).", "NetLock_RMM_Health_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Health_Agent_Windows");
                Logging.Handler.Debug("Main", "Stopping services (legacy).", "NetLock_RMM_Remote_Agent_Windows");
                Helper.Service.Stop("NetLock_RMM_Remote_Agent_Windows");
            }
            else if (OperatingSystem.IsLinux())
            {
                Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-comm");
                Bash.Execute_Script("Stopping services", false, "systemctl stop netlock-rmm-agent-comm");
                Logging.Handler.Debug("Main", "Stopping services.", "netlock-rmm-agent-remote");
                Bash.Execute_Script("Stopping services", false, "systemctl stop netlock-rmm-agent-remote");
            }
            else if (OperatingSystem.IsMacOS())
            {
                Logging.Handler.Debug("Main", "Stopping services.", Application_Paths.program_files_comm_agent_service_name_osx);
                Zsh.Execute_Script("Stopping services", false, $"launchctl stop {Application_Paths.program_files_comm_agent_service_name_osx}");
                Logging.Handler.Debug("Main", "Stopping services.", Application_Paths.program_files_remote_agent_service_name_osx);
                Zsh.Execute_Script("Stopping services", false, $"launchctl stop {Application_Paths.program_files_remote_agent_service_name_osx}");
            }
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services stopped.");

            // Wait a little to allow service manager to release handles to prevent service marked for deletion error
            Thread.Sleep(5000);

            // Kill processes
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminating processes.");
            if (OperatingSystem.IsWindows())
            {
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Comm.exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock_RMM_Agent_Comm.exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Remote.exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock_RMM_Agent_Remote.exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_User_Process.exe");
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock_RMM_User_Process.exe\"");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock RMM User Process.exe"); // kill legacy process
                Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"NetLock RMM User Process.exe\""); // kill legacy process
                //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"yara64.exe\""); // yara64.exe is (currently) not used in the project, its part of a netlock legacy feature
                //Helper._Process.Start("cmd.exe", "/c taskkill /F /IM \"devcon_x64.exe\""); // devcon_x64.exe is (currently) not used in the project, its part of a netlock legacy feature
            }
            else if (OperatingSystem.IsLinux())
            {
                Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-comm");
                Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-comm");
                Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-agent-remote");
                Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-agent-remote");
                Logging.Handler.Debug("Main", "Terminating processes.", "netlock-rmm-user-process");
                Bash.Execute_Script("Terminating processes", false, "pkill netlock-rmm-user-process");
            }
            else if (OperatingSystem.IsMacOS())
            {
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Comm");
                Zsh.Execute_Script("Terminating processes", false, "pkill NetLock_RMM_Agent_Comm");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Remote");
                Zsh.Execute_Script("Terminating processes", false, "pkill NetLock_RMM_Agent_Remote");
                Logging.Handler.Debug("Main", "Terminating processes.", "NetLock_RMM_Agent_Health");
                Zsh.Execute_Script("Terminating processes", false, "pkill NetLock_RMM_Agent_Health");
            }
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Terminated processes.");

            // Delete services
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting services.");
            if (OperatingSystem.IsWindows())
            {
                Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Agent_Comm");
                Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Agent_Comm");
                Logging.Handler.Debug("Main", "Deleting services.", "NetLock_RMM_Agent_Remote");
                Helper._Process.Start("cmd.exe", "/c sc delete NetLock_RMM_Agent_Remote");
            }
            else if (OperatingSystem.IsLinux())
            {
                Logging.Handler.Debug("Main", "Deleting services.", Application_Paths.program_files_comm_agent_service_name_linux);
                Bash.Execute_Script("Stopping service", false, $"systemctl stop {Application_Paths.program_files_comm_agent_service_name_linux} || true");
                Bash.Execute_Script("Disabling service", false, $"systemctl disable {Application_Paths.program_files_comm_agent_service_name_linux} || true");
                Bash.Execute_Script("Removing service file", false, $"rm -f /etc/systemd/system/{Application_Paths.program_files_comm_agent_service_name_linux}.service");

                Logging.Handler.Debug("Main", "Deleting services.", Application_Paths.program_files_remote_agent_service_name_linux);
                Bash.Execute_Script("Stopping service", false, $"systemctl stop {Application_Paths.program_files_remote_agent_service_name_linux} || true");
                Bash.Execute_Script("Disabling service", false, $"systemctl disable {Application_Paths.program_files_remote_agent_service_name_linux} || true");
                Bash.Execute_Script("Removing service file", false, $"rm -f /etc/systemd/system/{Application_Paths.program_files_remote_agent_service_name_linux}.service");

                // Reload Systemd to remove the deleted services
                Bash.Execute_Script("Reloading systemd", false, "systemctl daemon-reload");
            }
            else if (OperatingSystem.IsMacOS())
            {
                // Unload the service
                Logging.Handler.Debug("Main", "Unload service.", Application_Paths.program_files_comm_agent_service_config_path_osx);
                Zsh.Execute_Script("Unload service", false, $"launchctl unload {Application_Paths.program_files_comm_agent_service_config_path_osx}");

                // Delete the service file
                Logging.Handler.Debug("Main", "Deleting service file.", Application_Paths.program_files_comm_agent_service_config_path_osx);
                Zsh.Execute_Script("Deleting service file", false, $"rm -f {Application_Paths.program_files_comm_agent_service_config_path_osx}");

                // Unload the service
                Logging.Handler.Debug("Main", "Unload service.", Application_Paths.program_files_remote_agent_service_config_path_osx);
                Zsh.Execute_Script("Unload service", false, $"launchctl unload {Application_Paths.program_files_remote_agent_service_config_path_osx}");

                // Delete the service file
                Logging.Handler.Debug("Main", "Deleting service file.", Application_Paths.program_files_remote_agent_service_config_path_osx);
                Zsh.Execute_Script("Deleting service file", false, $"rm -f {Application_Paths.program_files_remote_agent_service_config_path_osx}");
            }
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Services deleted.");

            // Delete files
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting files.");
            Logging.Handler.Debug("Main", "Deleting files.", Application_Paths.program_data_comm_agent_events_path);
            Helper.IO.Delete_File(Application_Paths.program_data_comm_agent_events_path);
            Logging.Handler.Debug("Main", "Deleting files.", Application_Paths.program_data_comm_agent_policies_path);
            Helper.IO.Delete_File(Application_Paths.program_data_comm_agent_policies_path);
            Logging.Handler.Debug("Main", "Deleting files.", Application_Paths.program_data_comm_agent_server_config);
            Helper.IO.Delete_File(Application_Paths.program_data_comm_agent_version_path);
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleted files.");

            // Delete directories
            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting directories.");
            Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_files_comm_agent_dir);
            Helper.IO.Delete_Directory(Application_Paths.program_files_comm_agent_dir);
            Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_files_remote_agent_dir);
            Helper.IO.Delete_Directory(Application_Paths.program_files_remote_agent_dir);
            Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_files_user_agent_dir);
            Helper.IO.Delete_Directory(Application_Paths.program_files_user_agent_dir);
            Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_comm_agent_dir);
            Helper.IO.Delete_Directory(Application_Paths.program_data_comm_agent_dir);
            Logging.Handler.Debug("Main", "Deleting directories.", Application_Paths.program_data_remote_agent_dir);
            Helper.IO.Delete_Directory(Application_Paths.program_data_remote_agent_dir);
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
            
            // Delete legacy directories
            Logging.Handler.Debug("Main", "Deleting directories (legacy).", Application_Paths.program_files_user_process_dir);
            Helper.IO.Delete_Directory(Application_Paths.program_files_user_process_dir);
            Logging.Handler.Debug("Main", "Deleting directories (legacy).", Application_Paths.program_data_user_process_dir);
            Helper.IO.Delete_Directory(Application_Paths.program_data_user_process_dir);

            // Delete logs
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // Delete comm agent service log
                Logging.Handler.Debug("Main", "Deleting comm agent service log.", Application_Paths.program_files_comm_agent_service_log_path_unix);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting comm agent service log: " + Application_Paths.program_files_comm_agent_service_log_path_unix);
                Bash.Execute_Script("Deleting comm agent service log", false, $"rm -f {Application_Paths.program_files_comm_agent_service_log_path_unix}");

                // Delete remote agent service log
                Logging.Handler.Debug("Main", "Deleting remote agent service log.", Application_Paths.program_files_remote_agent_service_log_path_unix);
                Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleting remote agent service log: " + Application_Paths.program_files_remote_agent_service_log_path_unix);
                Bash.Execute_Script("Deleting remote agent service log", false, $"rm -f {Application_Paths.program_files_remote_agent_service_log_path_unix}");
            }

            Console.WriteLine("[" + DateTime.Now + "] - [Main] -> Deleted directories.");
        }
    }
}
