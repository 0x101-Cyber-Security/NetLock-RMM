using Global.Helper;
using Helper;
using NetLock_RMM_Agent_Health;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linux.Helper
{
    internal class Linux
    {
        // Helper method to create the installer service
        public static void CreateInstallerService()
        {
            // Set permissions
            Logging.Debug("Main", "Argument", $"sudo chmod +x \"{Application_Paths.c_temp_netlock_installer_path}\"");
            Bash.Execute_Script("Installer Permissions", false, $"sudo chmod +x \"{Application_Paths.c_temp_netlock_installer_path}\"");

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
