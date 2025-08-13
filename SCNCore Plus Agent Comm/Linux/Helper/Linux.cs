using Global.Helper;
using SCNCore_Plus_Agent_Comm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linux.Helper
{
    internal class Linux
    {
        public static string Get_Last_Boot_Time()
        {
            try
            {
                // Read uptime from /proc/uptime
                string uptimeContent = File.ReadAllText("/proc/uptime");
                string[] parts = uptimeContent.Split(' ');
                double uptimeSeconds = double.Parse(parts[0]);

                // Calculate the last boot time
                DateTime lastBootTime = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(uptimeSeconds));

                // Format the last boot time
                string formattedBootTime = lastBootTime.ToString("dd.MM.yyyy HH:mm:ss");

                // Log and return the result
                return formattedBootTime;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error retrieving last boot time: {ex.ToString()}");
                return null;
            }
        }

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
Description=scncore-plus-agent-installer
After=network.target

[Service]
ExecStart=""/tmp/scncore rmm/installer/SCNCore_Plus_Agent_Installer"" ""fix"" ""/var/0x101 Cyber Security/SCNCore Plus/Comm Agent/server_config.json""
Restart=no
RestartSec=5s
User=root
WorkingDirectory=/tmp/scncore rmm/installer
KillSignal=SIGINT
StandardOutput=append:/var/log/scncore-plus-agent-installer.log
StandardError=append:/var/log/scncore-plus-agent-installer.log
LimitNOFILE=65536

[Install]
WantedBy=multi-user.target
";

            // Write the service file
            System.IO.File.WriteAllText("/etc/systemd/system/scncore-plus-agent-installer.service", serviceContent);

            // Set correct permissions on the service file
            Bash.Execute_Script("Set service file permissions", false, $"chmod 644 \"/etc/systemd/system/scncore-plus-agent-installer.service\"");

            // Reload daemon
            Bash.Execute_Script("Reload services", false, "systemctl daemon-reload");
        }
    }
}
