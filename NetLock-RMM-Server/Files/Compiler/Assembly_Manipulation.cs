using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace NetLock_RMM_Server.Files.Compiler
{
    public class Assembly_Manipulation
    {
        public static async Task<bool> Embedd_Server_Config(string path, string content)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write))
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    writer.Write(Encoding.UTF8.GetBytes("SERVERCONFIGMARKER")); // Marker schreiben
                    writer.Write(Encoding.UTF8.GetBytes(await Base64.Handler.Encode(content))); // Base64-String anhängen
                }

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
