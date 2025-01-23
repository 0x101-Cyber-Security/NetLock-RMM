
using System.Runtime.InteropServices;
using System.Text;

namespace NetLock_RMM_Server.Helper.Compiler
{
    public class Ressource_Manipulation
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, byte[] lpData, uint cbData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern IntPtr FindResource(IntPtr hModule, int lpName, int lpType);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern bool FreeLibrary(IntPtr hModule);


        private static async Task<string> Write_Ressource (string content)
        {
            try
            {
                // Start the update
                IntPtr hUpdate = BeginUpdateResource(@"C:\temp\test.exe", false);

                if (hUpdate == IntPtr.Zero)
                {
                    Console.WriteLine("Fehler beim Starten des Ressourcen-Updates.");
                    return String.Empty;
                }

                // Erstellen der neuen benutzerdefinierten Ressource
                byte[] customResource = Encoding.UTF8.GetBytes(content);

                // Aktualisieren der benutzerdefinierten Ressource (Hier verwenden wir 'CUSTOM' als Typ und 'SECRET' als Name)
                IntPtr typePtr = Marshal.StringToHGlobalUni("CONFIG");
                IntPtr namePtr = Marshal.StringToHGlobalUni("SERVER_CONFIG");

                bool updateSuccess = UpdateResource(hUpdate, typePtr, namePtr, 0, customResource, (uint)customResource.Length); if (!updateSuccess)
                {
                    Console.WriteLine("Fehler beim Aktualisieren der Ressource.");
                    EndUpdateResource(hUpdate, true); // Rollback bei Fehler
                    return String.Empty;
                }

                // Beenden des Updates
                bool endSuccess = EndUpdateResource(hUpdate, false);
                if (!endSuccess)
                {
                    Console.WriteLine("Fehler beim Beenden des Ressourcen-Updates.");
                }
                else
                {
                    Console.WriteLine("Ressourcen erfolgreich aktualisiert.");
                }

                return content;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("NetLock_RMM_Server.Helper.Compiler.Ressource_Manipulation.Write_Ressource", "General error", ex.ToString());
                return String.Empty;
            }
        }
    }
}
