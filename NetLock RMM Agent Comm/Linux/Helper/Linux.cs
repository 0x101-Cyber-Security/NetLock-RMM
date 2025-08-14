using Global.Helper;
using NetLock_RMM_Agent_Comm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linux.Helper
{
    internal class Linux
    {
        public static string Get_Linux_Distribution()
        {
            try
            {
                // Read /etc/os-release file
                var osReleasePath = "/etc/os-release";
                if (File.Exists(osReleasePath))
                {
                    var lines = File.ReadAllLines(osReleasePath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("ID="))
                        {
                            // Extract the distribution ID
                            var distro = line.Substring(3).Trim('"');

                            Logging.Debug("Linux.Helper.Linux.Get_Linux_Distribution", "Distro", distro);

                            return distro;
                        }
                    }
                }

                Logging.Debug("Linux.Helper.Linux.Get_Linux_Distribution", "Distro", "Unknown");

                return "Could not determine Linux distribution.";
            }
            catch (Exception ex)
            {
                Logging.Error("Linux.Helper.Linux.Get_Linux_Distribution", "", ex.ToString());
                return $"Error reading /etc/os-release: {ex.Message}";
            }
        }

        public static double Disks_Convert_Size_To_GB(string size)
        {
            try
            {
                double sizeInGB = 0;

                // Check for size in GB
                if (size.EndsWith("G", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("G", "").Trim());
                }
                // Check for size in MB (convert to GB)
                else if (size.EndsWith("M", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("M", "").Trim()) / 1024; // Convert MB to GB
                }
                // Check for size in TB (convert to GB)
                else if (size.EndsWith("T", StringComparison.OrdinalIgnoreCase))
                {
                    sizeInGB = double.Parse(size.Replace("T", "").Trim()) * 1024; // Convert TB to GB
                }
                // Handle case without unit (assuming size is in GB)
                else if (double.TryParse(size, out double sizeValue))
                {
                    sizeInGB = sizeValue;
                }
                else
                {
                    throw new ArgumentException("Invalid size format.");
                }

                // Round to 2 decimal places for better display
                return Math.Round(sizeInGB, 2);
            }
            catch (Exception ex)
            {
                Logging.Error("Linux.Helper.Linux.Disks_Convert_Size_To_GB", "", ex.ToString());
                return 0;
            }
        }

        // Helper method to create the installer service
        public static void CreateInstallerService()
        {
            // Set permissions
            Logging.Debug("Main", "Argument", $"sudo chmod +x \"{Application_Paths.c_temp_installer_path}\"");
            Bash.Execute_Script("Installer Permissions", false, $"sudo chmod +x \"{Application_Paths.c_temp_installer_path}\"");

            // Compose systemd service file content
            string serviceContent = @$"[Unit]
Description=netlock-rmm-agent-installer
After=network.target

[Service]
ExecStart=""/tmp/netlock rmm/installer/NetLock_RMM_Agent_Installer"" ""fix"" ""/var/0x101 Cyber Security/NetLock RMM/Comm Agent/server_config.json""
Restart=no
RestartSec=5s
User=root
WorkingDirectory=/tmp/netlock rmm/installer
KillSignal=SIGINT
StandardOutput=append:/var/log/netlock-rmm-agent-installer.log
StandardError=append:/var/log/netlock-rmm-agent-installer.log
LimitNOFILE=65536

[Install]
WantedBy=multi-user.target
";

            // Write the service file
            System.IO.File.WriteAllText("/etc/systemd/system/netlock-rmm-agent-installer.service", serviceContent);

            // Set correct permissions on the service file
            Bash.Execute_Script("Set service file permissions", false, $"chmod 644 \"/etc/systemd/system/netlock-rmm-agent-installer.service\"");

            // Reload daemon
            Bash.Execute_Script("Reload services", false, "systemctl daemon-reload");
        }
    }
}
