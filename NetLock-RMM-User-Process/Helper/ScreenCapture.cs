using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace Helper
{
    internal class ScreenCapture
    {
        // Native API to query the number of screens and their properties
        [DllImport("User32.dll")]

        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("Gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        private const int HORZRES = 8; // Width of the screen
        private const int VERTRES = 10; // Height of the screen

        // Method to capture the screen and return it as a Base64 string
        public static async Task<string> CaptureScreenToBase64(int screenIndex)
        {
            Console.WriteLine("Capturing screen index: " + screenIndex);
            try
            {
                Rect monitorRect = new Rect();
                int currentScreenIndex = 0;

                // Callback function for EnumDisplayMonitors
                MonitorEnumDelegate callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
                {
                    if (currentScreenIndex == screenIndex)
                    {
                        monitorRect = lprcMonitor;
                        return false; // Stop enumeration
                    }
                    currentScreenIndex++;
                    return true; // Continue enumeration
                };

                // Enumerate all monitors and find the one with the specified index
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

                // Check if we have found the correct monitor
                if (monitorRect.Right == 0 && monitorRect.Bottom == 0)
                {
                    Console.WriteLine("Monitor not found.");
                    throw new Exception("Monitor not found.");
                }

                // Desktop-DC holen
                IntPtr desktopDC = GetDC(IntPtr.Zero);

                // Determine screen size from the selected monitor
                int screenWidth = monitorRect.Right - monitorRect.Left;
                int screenHeight = monitorRect.Bottom - monitorRect.Top;

                // Creating a bitmap for the screenshot
                using (Bitmap bmpScreenshot = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb))
                {
                    // Create graphic object for drawing
                    using (Graphics gfxScreenshot = Graphics.FromImage(bmpScreenshot))
                    {
                        // Take a screenshot of the selected screen
                        gfxScreenshot.CopyFromScreen(monitorRect.Left, monitorRect.Top, 0, 0, new Size(screenWidth, screenHeight), CopyPixelOperation.SourceCopy);
                    }

                    // Bitmap in einen MemoryStream speichern als JPEG
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Qualität der JPEG-Kompression einstellen
                        var encoderParameters = new EncoderParameters(1);
                        encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L); // Qualität 0-100

                        // JPEG-Encoder auswählen
                        ImageCodecInfo jpegCodec = GetEncoder(ImageFormat.Jpeg);
                        bmpScreenshot.Save(ms, jpegCodec, encoderParameters);
                        byte[] imageBytes = ms.ToArray(); // Receive the image as a byte array

                        // In Base64 kodieren
                        return Convert.ToBase64String(imageBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing screen: {ex.Message}");
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
                Console.WriteLine($"Error getting encoder: {ex.Message}");
                return null;
            }
        }

        // Delegate to process each monitor
        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        // Struct for monitor information
        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // P/Invoke to enumerate display monitors
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        // P/Invoke to get monitor info
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);


       
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MonitorInfo
        {
            public int cbSize;
            public Rect rcMonitor;
            public Rect rcWork;
            public uint dwFlags;
        }

        public static int Get_Screen_Indexes()
        {
            int screenIndex = 0;

            try
            {
                MonitorEnumDelegate callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
                {
                    // Retrieve the monitor information
                    MonitorInfo mi = new MonitorInfo();
                    mi.cbSize = Marshal.SizeOf(mi);
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                        Console.WriteLine($"Screen {screenIndex}: {mi.rcMonitor.Left}, {mi.rcMonitor.Top}, {mi.rcMonitor.Right}, {mi.rcMonitor.Bottom}");
                        screenIndex++;
                    }
                    return true; // Continue enumeration
                };

                // Enumerate all monitors
                if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero))
                {
                    Console.WriteLine("Error enumerating monitors.");
                }
                else
                {
                    Console.WriteLine($"Number of screens: {screenIndex}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting screen indexes: {ex.Message}");
            }

            return screenIndex;
        }
    }
}