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
        public static void CreateServiceFile(string serviceFilePath, string serviceName, string executablePath)
        {
            string serviceContent = @$"[Unit]
Description={serviceName}
After=network.target

[Service]
ExecStart={executablePath}
Restart=always
RestartSec=5s
User=root

[Install]
WantedBy=multi-user.target
";

            // Write the service file
            System.IO.File.WriteAllText(serviceFilePath, serviceContent);
            Bash.Execute_Script("Setting correct permissions", false,
                $"chmod 644 {serviceFilePath}");
        }
    }
}
