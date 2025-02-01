using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linux.Helper
{
    internal class Linux
    {
        // Helper method to create service files dynamically
        public static void CreateServiceFile(string serviceFilePath, string serviceName, string executablePath, string workingDirectory, string logFilePath)
        {
            // Wenn der Pfad Leerzeichen enthält, setzen wir ihn in doppelte Anführungszeichen
            string formattedExecutablePath = $"\"{executablePath}\"";

            string serviceContent = @$"[Unit]
Description={serviceName}
After=network.target

[Service]
ExecStart={formattedExecutablePath}
Restart=always
RestartSec=5s
User=root
WorkingDirectory={workingDirectory}
KillSignal=SIGINT
StandardOutput=append:{logFilePath}
StandardError=append:{logFilePath}
LimitNOFILE=65536

[Install]
WantedBy=multi-user.target
";

            // Write the service file
            System.IO.File.WriteAllText(serviceFilePath, serviceContent);

            // Set correct permissions
            Bash.Execute_Script("Setting correct permissions", false,
                $"chmod 644 {serviceFilePath}");
        }
    }
}
