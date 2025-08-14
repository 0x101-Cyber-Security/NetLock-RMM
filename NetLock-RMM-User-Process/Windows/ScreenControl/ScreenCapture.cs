using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace NetLock_RMM_User_Process.Windows.ScreenControl
{
    internal class ScreenCapture
    {
        // Native API to query the number of screens and their properties
        [DllImport("User32.dll")]

        private static extern nint GetDC(nint hWnd);

        [DllImport("Gdi32.dll")]
        private static extern int GetDeviceCaps(nint hdc, int nIndex);

        private const int HORZRES = 8; // Width of the screen
        private const int VERTRES = 10; // Height of the screen

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(nint hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, nint hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

        // Method to capture the screen and return it as a Base64 string
        public static async Task<string> CaptureScreenToBase64(int screenIndex, int maxFileSizeKB = 100)
        {
            Console.WriteLine("Capturing screen index: " + screenIndex);
            try
            {
                // Monitorbereich ermitteln
                Rect monitorRect = new Rect();
                int currentScreenIndex = 0;

                MonitorEnumDelegate callback = (nint hMonitor, nint hdcMonitor, ref Rect lprcMonitor, nint dwData) =>
                {
                    if (currentScreenIndex == screenIndex)
                    {
                        monitorRect = lprcMonitor;
                        return false; // stop enumeration
                    }
                    currentScreenIndex++;
                    return true;
                };

                EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);

                if (monitorRect.Right == 0 && monitorRect.Bottom == 0)
                    throw new Exception("Monitor not found.");

                int screenWidth = monitorRect.Right - monitorRect.Left;
                int screenHeight = monitorRect.Bottom - monitorRect.Top;

                Console.WriteLine("screenWidth: " + screenWidth);
                Console.WriteLine("screenHeight: " + screenHeight);

                using Bitmap bmp = CaptureWithBitBlt(monitorRect);

                // JPEG-Qualität per Binärsuche bestimmen
                int minQ = 10, maxQ = 90;
                byte[] bestBytes = null;
                int bestQuality = minQ;

                while (minQ <= maxQ)
                {
                    int midQ = (minQ + maxQ) / 2;
                    using var ms = new MemoryStream();
                    using var encoderParams = CreateEncoderParams(midQ);

                    bmp.Save(ms, jpegEncoder, encoderParams);
                    var currentBytes = ms.ToArray();

                    if (currentBytes.Length <= maxFileSizeKB * 1024)
                    {
                        bestBytes = currentBytes;
                        bestQuality = midQ;
                        minQ = midQ + 1; // Versuche höhere Qualität
                    }
                    else
                    {
                        maxQ = midQ - 1; // Zu groß
                    }
                }

                if (bestBytes == null)
                    throw new Exception("Unable to compress image under max size limit.");

                // Optional Debug-Speichern
                /*
                string filePath = Path.Combine("C:\\temp", $"screenshot_{screenIndex}.jpg");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                await File.WriteAllBytesAsync(filePath, bestBytes);
                Console.WriteLine($"Screenshot saved to: {filePath} ({bestBytes.Length / 1024} KB, Quality: {bestQuality}%)");
                */

                return Convert.ToBase64String(bestBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing screen: {ex.Message}");
                return null;
            }
        }

        private static readonly ImageCodecInfo jpegEncoder = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);

        private static EncoderParameters CreateEncoderParams(int quality)
        {
            var p = new EncoderParameters(1);
            p.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            return p;
        }

        public static Bitmap CaptureWithBitBlt(Rect monitorRect)
        {
            var bmp = new Bitmap(monitorRect.Width, monitorRect.Height, PixelFormat.Format32bppArgb);
            using var gDest = Graphics.FromImage(bmp);
            using var gSrc = Graphics.FromHwnd(nint.Zero);

            nint hdcDest = gDest.GetHdc();
            nint hdcSrc = gSrc.GetHdc();

            BitBlt(hdcDest, 0, 0, monitorRect.Width, monitorRect.Height,
                   hdcSrc, monitorRect.Left, monitorRect.Top, CopyPixelOperation.SourceCopy);

            gDest.ReleaseHdc(hdcDest);
            gSrc.ReleaseHdc(hdcSrc);

            return bmp;
        }

        private static byte[] EncodeJpeg(Bitmap bmp, int quality)
        {
            using var ms = new MemoryStream();
            using var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            bmp.Save(ms, jpegEncoder, encoderParams);
            return ms.ToArray();
        }



        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == format.Guid);
        }

        // Delegate to process each monitor
        private delegate bool MonitorEnumDelegate(nint hMonitor, nint hdcMonitor, ref Rect lprcMonitor, nint dwData);

        // Struct for monitor information
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        // P/Invoke to enumerate display monitors
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumDisplayMonitors(nint hdc, nint lprcClip, MonitorEnumDelegate lpfnEnum, nint dwData);

        // P/Invoke to get monitor info
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(nint hMonitor, ref MonitorInfo lpmi);


       
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
                MonitorEnumDelegate callback = (nint hMonitor, nint hdcMonitor, ref Rect lprcMonitor, nint dwData) =>
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
                if (!EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero))
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