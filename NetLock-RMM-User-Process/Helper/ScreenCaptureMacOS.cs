using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using NetLock_RMM_User_Process;

public class ScreenCaptureMacOS
{
    /// <summary>
    /// This code was just playing around. It's not used in the project. In Q3 I will revisit this and try to realize screen capturing through native MacOS APIs. Connected with the planned Support Mode.
    /// </summary>
    /// <param name="screenIndex"></param>
    /// <returns></returns>
    public static async Task<string> CaptureScreenToBase64(int screenIndex)
    {
        try
        {
            // Screenshot-Pfad
            string screenshotPath = "screenshot.png";

            // Screenshot mit Terminal-Befehl aufnehmen
            string command = $"screencapture '{screenshotPath}'";
            string output = MacOS.Helper.Zsh.Execute_Script("test", false, command);

            Console.WriteLine($"Screenshot command output: {output}");

            // Überprüfen, ob das Screenshot erstellt wurde
            if (!File.Exists(screenshotPath))
            {
                Console.WriteLine("Screenshot not found.");
                return null;
            }

            // Screenshot als Base64 kodieren
            byte[] screenshotBytes = await File.ReadAllBytesAsync(screenshotPath);
            string base64Screenshot = Convert.ToBase64String(screenshotBytes);

            return base64Screenshot;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error capturing screen: {ex.ToString()}");
            return null;
        }
    }

    private static ImageCodecInfo GetEncoder(ImageFormat format)
    {
        try
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting encoder: {ex.ToString()}");
            return null;
        }
    }
}
