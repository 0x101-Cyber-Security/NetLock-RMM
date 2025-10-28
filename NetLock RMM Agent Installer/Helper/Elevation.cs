using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Helper
{
    internal class Elevation
    {

        public static bool IsElevated()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                    {
                        WindowsPrincipal principal = new WindowsPrincipal(identity);
                        return principal.IsInRole(WindowsBuiltInRole.Administrator) || identity.Name.Equals("NT AUTHORITY\\SYSTEM", StringComparison.OrdinalIgnoreCase);
                    }
                }
                else // Linux / macOS
                {
                    try
                    {
                        return geteuid() == 0;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking elevation: {ex.Message}");
                return false;
            }
        }

        public static bool TryElevate(string[] args)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows: Restart with "runas" verb
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName,
                        UseShellExecute = true,
                        Verb = "runas", // Request elevation
                        Arguments = string.Join(" ", args)
                    };

                    Process.Start(processInfo);
                    return true;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Linux/macOS: Restart with sudo
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "sudo",
                        Arguments = $"\"{Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName}\" {string.Join(" ", args)}",
                        UseShellExecute = false
                    };

                    Process.Start(processInfo);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error trying to elevate: {ex.Message}");
                return false;
            }
        }

        // Import native getuid-Funktion für Linux/macOS
        [DllImport("libc")]
        private static extern uint geteuid();
    }
}
