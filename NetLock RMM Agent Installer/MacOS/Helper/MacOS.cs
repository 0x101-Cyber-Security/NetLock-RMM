using Helper;
using MacOS.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Agent_Installer.MacOS.Helper
{
    internal class MacOS
    {
        public static void CreateMacServiceFile(string serviceFilePath, string serviceName, string executablePath, string logFilePath)
        {
            string serviceContent = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
    <dict>
        <key>Label</key>
        <string>{serviceName}</string>

        <key>ProgramArguments</key>
        <array>
            <string>{executablePath}</string>
        </array>

        <key>RunAtLoad</key>
        <true/>

        <key>KeepAlive</key>
        <true/>

        <key>StandardOutPath</key>
        <string>{logFilePath}</string>

        <key>StandardErrorPath</key>
        <string>{logFilePath}</string>
    </dict>
</plist>";

            try
            {
                // Service-Datei schreiben
                File.WriteAllText(serviceFilePath, serviceContent);
                Console.WriteLine($"Plist-Datei erstellt: {serviceFilePath}");

                // Berechtigungen setzen
                Zsh.Execute_Script("Set correct permissions", false, $"sudo chmod 644 {serviceFilePath}");
                Zsh.Execute_Script("Set ownership", false, $"sudo chown root:wheel {serviceFilePath}");

                // Prüfen, ob die ausführbare Datei existiert und Berechtigungen setzen
                if (File.Exists(executablePath))
                {
                    Zsh.Execute_Script("Set executable permissions", false, $"sudo chmod +x {executablePath}");
                }
                else
                {
                    Console.WriteLine($"WARNUNG: Die ausführbare Datei {executablePath} existiert nicht!");
                }

                // Service entladen, falls bereits aktiv
                Zsh.Execute_Script("Unload the service (if running)", false, $"sudo launchctl unload {serviceFilePath}");

                // Service laden & starten
                Zsh.Execute_Script("Load the service", false, $"sudo launchctl load {serviceFilePath}");
                Zsh.Execute_Script("Start the service", false, $"sudo launchctl start {serviceName}");

                // Status prüfen
                Zsh.Execute_Script("Check service status", false, $"launchctl list | grep {serviceName}");

                Console.WriteLine("macOS-Dienst erfolgreich eingerichtet.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim Erstellen des Dienstes: {ex.Message}");
            }
        }
    }
}
