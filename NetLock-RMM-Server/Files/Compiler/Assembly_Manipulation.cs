using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace NetLock_RMM_Server.Files.Compiler
{
    public class Assembly_Manipulation
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern nint BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool UpdateResource(nint hUpdate, nint lpType, nint lpName, ushort wLanguage, byte[] lpData, uint cbData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool EndUpdateResource(nint hUpdate, bool fDiscard);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern nint LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern nint FindResource(nint hModule, int lpName, int lpType);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern uint SizeofResource(nint hModule, nint hResInfo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern nint LoadResource(nint hModule, nint hResInfo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern nint LockResource(nint hResData);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]

        static extern bool FreeLibrary(nint hModule);


        public static async Task<bool> Write_Ressource(string path, string content)
        {
            try
            {
                // Start the update
                nint hUpdate = BeginUpdateResource(path, false);

                if (hUpdate == nint.Zero)
                {
                    Console.WriteLine("Fehler beim Starten des Ressourcen-Updates.");
                    return false;
                }

                // Create the new user-defined resource
                byte[] customResource = Encoding.UTF8.GetBytes(content);

                // Update the custom resource (here we use 'CUSTOM' as the type and 'SECRET' as the name)
                nint typePtr = Marshal.StringToHGlobalUni("CONFIG");
                nint namePtr = Marshal.StringToHGlobalUni("SERVER_CONFIG");

                bool updateSuccess = UpdateResource(hUpdate, typePtr, namePtr, 0, customResource, (uint)customResource.Length); if (!updateSuccess)
                {
                    //Console.WriteLine("Fehler beim Aktualisieren der Ressource.");
                    EndUpdateResource(hUpdate, true); // Rollback bei Fehler
                    return false;
                }

                // Terminate the update
                bool endSuccess = EndUpdateResource(hUpdate, false);
                if (!endSuccess)
                {
                    //Console.WriteLine("Fehler beim Beenden des Ressourcen-Updates.");
                    Logging.Handler.Debug("NetLock_RMM_Server.Helper.Compiler.Ressource_Manipulation.Write_Ressource", "Error", "Error when ending the resource update.");
                }
                else
                {
                    //Console.WriteLine("Ressourcen erfolgreich aktualisiert.");
                    Logging.Handler.Debug("NetLock_RMM_Server.Helper.Compiler.Ressource_Manipulation.Write_Ressource", "Success", "Ressource successfully updated.");
                }

                // Release the memory
                Marshal.FreeHGlobal(typePtr);
                Marshal.FreeHGlobal(namePtr);

                // Release file
                FreeLibrary(hUpdate);

                return true;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("NetLock_RMM_Server.Helper.Compiler.Ressource_Manipulation.Write_Ressource", "General error", ex.ToString());
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
