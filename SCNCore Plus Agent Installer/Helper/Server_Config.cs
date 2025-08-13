using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    internal class Server_Config
    {
        // Read the base64 config from the file
        public static string ReadBase64Config(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    // Find markers
                    byte[] markerBytes = Encoding.UTF8.GetBytes("SERVERCONFIGMARKER");
                    byte[] fileBytes = reader.ReadBytes((int)fs.Length);
                    int markerIndex = IndexOf(fileBytes, markerBytes);

                    // Read the base64 string
                    byte[] base64Bytes = new byte[fileBytes.Length - markerIndex - markerBytes.Length];
                    Array.Copy(fileBytes, markerIndex + markerBytes.Length, base64Bytes, 0, base64Bytes.Length);

                    // Return the decoded base64 string
                    return Encoding.UTF8.GetString(Convert.FromBase64String(Encoding.UTF8.GetString(base64Bytes)));
                }
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Server_Config.ReadBase64Config", "Error while reading base64 config.", ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Server_Config.ReadBase64Config] -> Error while reading base64 config: " + ex.Message);
                return null;
            }
        }

        // Find the index of a subarray in a larger array
        private static int IndexOf(byte[] array, byte[] subArray)
        {
            try
            {
                for (int i = 0; i < array.Length - subArray.Length; i++)
                {
                    bool found = true;
                    for (int j = 0; j < subArray.Length; j++)
                    {
                        if (array[i + j] != subArray[j])
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found)
                        return i;
                }
                return -1;
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Server_Config.IndexOf", "Error while searching for subarray.", ex.ToString());
                Console.WriteLine("[" + DateTime.Now + "] - [Server_Config.IndexOf] -> Error while searching for subarray: " + ex.Message);
                return -1;
            }
        }
    }
}
