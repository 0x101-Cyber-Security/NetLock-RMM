using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Global.Helper;
using System.Diagnostics;
using Windows.Helper;
using NetLock_RMM_Agent_Comm;

namespace Global.Initialization
{
    internal class Health
    {
        // Check if the directories are in place
        public static void Check_Directories()
        {
            try
            {
                // Program Data
                if (!Directory.Exists(Application_Paths.program_data))
                    Directory.CreateDirectory(Application_Paths.program_data);

                // Logs
                if (!Directory.Exists(Application_Paths.program_data_logs))
                    Directory.CreateDirectory(Application_Paths.program_data_logs);
                
                // Installer
                if (!Directory.Exists(Application_Paths.program_data_installer))
                    Directory.CreateDirectory(Application_Paths.program_data_installer);

                // Updates
                if (!Directory.Exists(Application_Paths.program_data_updates))
                    Directory.CreateDirectory(Application_Paths.program_data_updates);

                // NetLock Temp
                if (!Directory.Exists(Application_Paths.program_data_temp))
                    Directory.CreateDirectory(Application_Paths.program_data_temp);

                // Jobs
                if (!Directory.Exists(Application_Paths.program_data_jobs))
                    Directory.CreateDirectory(Application_Paths.program_data_jobs);

                // Scripts
                if (!Directory.Exists(Application_Paths.program_data_scripts))
                    Directory.CreateDirectory(Application_Paths.program_data_scripts);

                // Sensors
                if (!Directory.Exists(Application_Paths.program_data_sensors))
                    Directory.CreateDirectory(Application_Paths.program_data_sensors);
                
                // Tray Icon
                if (!Directory.Exists(Application_Paths.tray_icon_dir))
                    Directory.CreateDirectory(Application_Paths.tray_icon_dir);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Logging.Error("Global.Initialization.Health.Check_Directories", "", ex.ToString());
            }
        }

        public static void CleanTempScripts()
        {
            try
            {
                // Check if files exist in the scripts temp directory (comm agent)
                if (Directory.Exists(Application_Paths.program_data_scripts))
                {
                    string[] temp_files = Directory.GetFiles(Application_Paths.program_data_scripts);

                    foreach (string file in temp_files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("Global.Initialization.Health.CleanThings",
                                "Failed to delete temp script: " + file, ex.ToString());
                        }
                    }
                }
                    
                // Check if files exist in the scripts temp directory (remote agent)
                if (Directory.Exists(Application_Paths.program_data_remote_agent_scripts))
                {
                    string[] temp_files = Directory.GetFiles(Application_Paths.program_data_remote_agent_scripts);

                    foreach (string file in temp_files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("Global.Initialization.Health.CleanThings",
                                "Failed to delete remote agent temp script: " + file, ex.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Error("Global.Initialization.Health.CleanThings", "", e.ToString());
            }
        }

        // Check if the registry keys are in place
        public static void Check_Registry()
        { 
            try
            {
                // Check if netlock key exists
                if (!Windows.Helper.Registry.HKLM_Key_Exists(Application_Paths.netlock_reg_path))
                    Windows.Helper.Registry.HKLM_Create_Key(Application_Paths.netlock_reg_path);

                // Check if msdav key exists
                if (!Windows.Helper.Registry.HKLM_Key_Exists(Application_Paths.netlock_microsoft_defender_antivirus_reg_path))
                    Windows.Helper.Registry.HKLM_Create_Key(Application_Paths.netlock_microsoft_defender_antivirus_reg_path);
            }
            catch (Exception ex)
            {
                Logging.Error("Global.Initialization.Health.Check_Registry", "", ex.ToString());
            }
        }

        // Check if the firewall rules are in place
        public static void Check_Firewall()
        {
            // Dev setup moved to linux. Com is not available on linux. Need to look for a alternative to set firewall rules and verify them
            /*
            Windows.Microsoft_Defender_Firewall.Handler.NetLock_RMM_Comm_Agent_Rule_Inbound();
            Windows.Microsoft_Defender_Firewall.Handler.NetLock_RMM_Comm_Agent_Rule_Outbound();
            Windows.Microsoft_Defender_Firewall.Handler.NetLock_RMM_Health_Service_Rule();
            Windows.Microsoft_Defender_Firewall.Handler.NetLock_Installer_Rule();
            Windows.Microsoft_Defender_Firewall.Handler.NetLock_Uninstaller_Rule();
            */
        }

        // Check if the databases are in place
        public static void Check_Databases()
        {
            // Check if the databases are in place
            if (!File.Exists(Application_Paths.program_data_netlock_policy_database))
                Database.NetLock_Data_Setup();

            // Check if the events database is in place
            if (!File.Exists(Application_Paths.program_data_netlock_events_database))
                Database.NetLock_Events_Setup();
        }

        public static void Clean_Service_Restart()
        {
            Logging.Debug("Global.Initialization.Health.Clean_Service_Restart", "Starting.", "");

            Process cmd_process = new Process();
            cmd_process.StartInfo.UseShellExecute = true;
            cmd_process.StartInfo.CreateNoWindow = true;
            cmd_process.StartInfo.FileName = "cmd.exe";
            cmd_process.StartInfo.Arguments = "/c powershell" + " Stop-Service 'NetLock_RMM_Agent_Comm'; Remove-Item 'C:\\ProgramData\\0x101 Cyber Security\\NetLock RMM\\Comm Agent\\policy.nlock'; Remove-Item 'C:\\ProgramData\\0x101 Cyber Security\\NetLock RMM\\Comm Agent\\events.nlock'; Start-Service 'NetLock_RMM_Agent_Comm'";
            cmd_process.Start();
            cmd_process.WaitForExit();

            Logging.Error("Global.Initialization.Health.Clean_Service_Restart", "Stopping.", "");
        }

        public static void Setup_Events_Virtual_Datatable()
        {
            try
            {
                Device_Worker.events_data_table.Columns.Clear();
                Device_Worker.events_data_table.Columns.Add("severity");
                Device_Worker.events_data_table.Columns.Add("reported_by");
                Device_Worker.events_data_table.Columns.Add("event");
                Device_Worker.events_data_table.Columns.Add("description");
                Device_Worker.events_data_table.Columns.Add("type");
                Device_Worker.events_data_table.Columns.Add("language");
                Device_Worker.events_data_table.Columns.Add("notification_json");

                Logging.Debug("Global.Initialization.Health.Setup_Events_Virtual_Datatable", "Create datatable", "Done.");
            }
            catch (Exception ex)
            {
                Logging.Error("Global.Initialization.Health.Setup_Events_Virtual_Datatable", "Create datatable", ex.ToString());
            }
        }
        
        public static void User_Processes()
        {
            if (OperatingSystem.IsWindows())
            {
                // Delete old NetLock RMM User Agent from the registry, if it exists
                Registry.HKLM_Delete_Value(Application_Paths.hklm_run_directory_reg_path, "NetLock RMM User Process");
        
                // Write the NetLock RMM User Process to the registry, if it does not exist
                Logging.Debug("Initialization.Health.User_Process", "Write to registry", "NetLock RMM User Agent");
                Registry.HKLM_Write_Value(Application_Paths.hklm_run_directory_reg_path, "NetLock RMM User Agent", Application_Paths.netlock_user_process_uac_exe);
                
                // Write the NetLock RMM Tray Icon to the registry, if it does not exist
                Logging.Debug("Initialization.Health.User_Process", "Write to registry", "NetLock RMM Tray Icon");
                Registry.HKLM_Write_Value(Application_Paths.hklm_run_directory_reg_path, "NetLock RMM Tray Icon", Application_Paths.tray_icon_icon_exe);
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    string autostartDir = "/etc/xdg/autostart";
                    string desktopFile = Path.Combine(autostartDir, "netlock-tray-icon.desktop");
        
                    // Ensure directory exists
                    if (!Directory.Exists(autostartDir))
                    {
                        Directory.CreateDirectory(autostartDir);
                    }
        
                    // Create .desktop file content
                    string desktopContent = $@"[Desktop Entry]
        Type=Application
        Name=NetLock RMM Tray Icon
        Exec={Application_Paths.tray_icon_icon_exe}
        Hidden=false
        NoDisplay=false
        X-GNOME-Autostart-enabled=true
        ";
        
                    Logging.Debug("Initialization.Health.User_Process", "Write autostart file", desktopFile);
                    File.WriteAllText(desktopFile, desktopContent);
                    
                    // Set permissions (readable by all)
                    var chmod = System.Diagnostics.Process.Start("chmod", $"644 {desktopFile}");
                    chmod?.WaitForExit();
                }
                catch (Exception ex)
                {
                    Logging.Error("Initialization.Health.User_Process", "Failed to create autostart entry", ex.Message);
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                try
                {
                    string launchAgentsDir = "/Library/LaunchAgents";
                    string plistFile = Path.Combine(launchAgentsDir, "com.netlock.rmm.trayicon.plist");

                    if (!Directory.Exists(launchAgentsDir))
                    {
                        Directory.CreateDirectory(launchAgentsDir);
                    }

                    string plistContent = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>Label</key>
    <string>com.netlock.rmm.trayicon</string>
    <key>ProgramArguments</key>
    <array>
        <string>{Application_Paths.tray_icon_icon_exe}</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <false/>
</dict>
</plist>
";

                    Logging.Debug("Initialization.Health.User_Process", "Write LaunchAgent plist", plistFile);
                    File.WriteAllText(plistFile, plistContent);

                    var chmod = Process.Start("chmod", $"644 {plistFile}");
                    chmod?.WaitForExit();

                    var chown = Process.Start("chown", $"root:wheel {plistFile}");
                    chown?.WaitForExit();
                }
                catch (Exception ex)
                {
                    Logging.Error("Initialization.Health.User_Process", "Failed to create LaunchAgent", ex.Message);
                }
            }
        }
    }
}
