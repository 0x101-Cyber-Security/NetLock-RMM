using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Base64
{
    internal class Handler
    {
        public static async Task<string> Encode(string content)
        {
            byte[] data = Encoding.UTF8.GetBytes(content);
            return await Task.Run(() => Convert.ToBase64String(data));
        }

        public static async Task<string> Decode(string encodedContent)
        {
            try
            {
                byte[] data = Convert.FromBase64String(encodedContent);
                return await Task.Run(() => Encoding.UTF8.GetString(data));
            }
            catch (Exception ex)
            {
                //Logging.Handler.Error("Base64.Handler.Decode", "", ex.Message);
                return "";
            }
        }

        public static string Decode_Syncron(string encodedContent)
        {
            try
            {
                byte[] data = Convert.FromBase64String(encodedContent);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                //Logging.Handler.Error("Base64.Handler.Decode", "", ex.Message);
                return "";
            }
        }


        /*public static void Write_File(string path, string base64)
        {
            try
            {
                File.WriteAllText(path, DecodeAsync(base64));
            }
            catch (Exception ex)
            {
                Logging.Handler.Error("Helper.Base64.Write_File", "failed", "Details: " + Environment.NewLine +
                "Path: " + path + Environment.NewLine +
                "Base64: " + base64 + Environment.NewLine +
                "Error: " + ex.Message);
            }
        }*/
    }
}
