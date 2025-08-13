using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

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

        // Import native getuid-Funktion für Linux/macOS
        [DllImport("libc")]
        private static extern uint geteuid();
    }
}
