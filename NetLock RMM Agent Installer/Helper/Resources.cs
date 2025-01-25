using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_Agent_Installer
{
    internal class Resources
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

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

        public static string Read_Resource(string path)
        {
            // Convert the type and name to IntPtr
            IntPtr typePtr = Marshal.StringToHGlobalUni("CONFIG");
            IntPtr namePtr = Marshal.StringToHGlobalUni("SERVER_CONFIG");

            try
            {
                // Load the executable containing the resource
                IntPtr hModule = LoadLibrary(path);
                if (hModule == IntPtr.Zero)
                {
                    Logging.Handler.Error("Resources", "Read_Resource", "Error loading the file.");
                    return "No embedded server config found.";
                }

                // Find the custom resource
                IntPtr hResource = FindResource(hModule, namePtr, typePtr);
                if (hResource == IntPtr.Zero)
                {
                    Logging.Handler.Error("Resources", "Read_Resource", "Resource not found.");
                    
                    FreeLibrary(hModule);
                    return "No embedded server config found.";
                }

                // Get the size of the resource
                uint size = SizeofResource(hModule, hResource);
                if (size == 0)
                {
                    Logging.Handler.Error("Resources", "Read_Resource", "Error when determining the resource size.");
                    
                    FreeLibrary(hModule);
                    return "No embedded server config found.";
                }

                // Load the resource into memory
                IntPtr hGlobal = LoadResource(hModule, hResource);
                if (hGlobal == IntPtr.Zero)
                {
                    Logging.Handler.Error("Resources", "Read_Resource", "Error loading the resource.");
                    
                    FreeLibrary(hModule);
                    return "No embedded server config found.";
                }

                // Lock the resource to get a pointer to the data
                IntPtr pResource = LockResource(hGlobal);
                if (pResource == IntPtr.Zero)
                {
                    Logging.Handler.Error("Resources", "Read_Resource", "Error when locking the resource.");
                    
                    FreeLibrary(hModule);
                    return "No embedded server config found.";
                }

                // Copy the resource data to a byte array
                byte[] data = new byte[size];
                Marshal.Copy(pResource, data, 0, (int)size);

                // Convert the resource data to a string
                string secretData = Encoding.UTF8.GetString(data);

                Console.WriteLine(secretData);

                // Free the library
                FreeLibrary(hModule);

                return secretData;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Resources", "Read_Resource", ex.ToString());
                Console.WriteLine(ex.ToString());
                return ex.ToString();
            }
            finally
            {
                // Release the allocated memory for the IntPtr
                Marshal.FreeHGlobal(typePtr);
                Marshal.FreeHGlobal(namePtr);
            }
        }
    }
}
