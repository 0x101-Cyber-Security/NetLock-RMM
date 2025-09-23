using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        [DllImport("User32.dll")]
        private static extern nint ReleaseDC(nint hWnd, nint hDC);

        [DllImport("Gdi32.dll")]
        private static extern int GetDeviceCaps(nint hdc, int nIndex);

        private const int HORZRES = 8; // Width of the screen
        private const int VERTRES = 10; // Height of the screen

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(nint hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, nint hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

        // Cache for monitor information to avoid repeated enumeration
        private static readonly Dictionary<int, Rect> MonitorCache = new Dictionary<int, Rect>();
        private static readonly object CacheLock = new object();
        private static DateTime LastCacheUpdate = DateTime.MinValue;
        private const int CacheValiditySeconds = 30; // Cache valid for 30 seconds

        // Track scaling factors for mouse coordinate translation
        private static readonly Dictionary<int, ScalingInfo> ScalingFactors = new Dictionary<int, ScalingInfo>();

        // Maximum allowed image dimension before automatic downscaling
        private const int MAX_IMAGE_DIMENSION = 1600;
        
        // Lower default quality for initial compression guess
        private const int DEFAULT_QUALITY = 60;
        
        // Maximum attempts for resizing/compression to meet size limit
        private const int MAX_COMPRESSION_ATTEMPTS = 4;

        // Class to store scaling information for coordinate translation
        public class ScalingInfo
        {
            public double ScaleX { get; set; } = 1.0;
            public double ScaleY { get; set; } = 1.0;
            public int OriginalWidth { get; set; }
            public int OriginalHeight { get; set; }
            public int ScaledWidth { get; set; }
            public int ScaledHeight { get; set; }
        }

        // Method to get scaling information for a screen
        public static ScalingInfo GetScalingInfo(int screenIndex)
        {
            lock (CacheLock)
            {
                if (ScalingFactors.TryGetValue(screenIndex, out ScalingInfo info))
                {
                    return info;
                }
                
                // If no scaling info exists, return default 1:1 mapping
                return new ScalingInfo { ScaleX = 1.0, ScaleY = 1.0 };
            }
        }

        // Method to convert web console coordinates to actual screen coordinates
        public static (int x, int y) TranslateCoordinates(int screenIndex, int x, int y)
        {
            var scalingInfo = GetScalingInfo(screenIndex);
            
            // Convert from scaled image coordinates back to original screen coordinates
            int actualX = (int)Math.Round(x / scalingInfo.ScaleX);
            int actualY = (int)Math.Round(y / scalingInfo.ScaleY);
            
            return (actualX, actualY);
        }

        // Method to capture the screen and return it as a Base64 string
        public static async Task<string> CaptureScreenToBase64(int screenIndex, int maxFileSizeKB = 150)
        {
            Console.WriteLine($"Capturing screen index: {screenIndex}, max size: {maxFileSizeKB}KB");

            try
            {
                // Get monitor rectangle with caching
                Rect monitorRect = GetMonitorRect(screenIndex);

                int screenWidth = monitorRect.Width;
                int screenHeight = monitorRect.Height;

                Console.WriteLine($"Screen dimensions: {screenWidth}x{screenHeight}");

                // Capture the screen
                using Bitmap original = CaptureWithBitBlt(monitorRect);
                
                // Skip processing on null bitmap
                if (original == null)
                {
                    Console.WriteLine("Failed to capture screen bitmap");
                    return null;
                }

                // Initialize scaling info
                var scalingInfo = new ScalingInfo 
                { 
                    ScaleX = 1.0, 
                    ScaleY = 1.0,
                    OriginalWidth = screenWidth,
                    OriginalHeight = screenHeight,
                    ScaledWidth = screenWidth,
                    ScaledHeight = screenHeight
                };

                // Process the image for network transmission
                string base64Result = await Task.Run(() => ProcessImageForTransmission(original, scalingInfo, screenIndex, maxFileSizeKB));
                
                if (string.IsNullOrEmpty(base64Result))
                {
                    Console.WriteLine("Image processing failed, couldn't meet size requirements");
                }
                else 
                {
                    int base64Length = base64Result.Length;
                    Console.WriteLine($"Processed image size: ~{base64Length / 1024}KB (Base64)");
                }
                
                return base64Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error capturing screen: {ex.Message}");
                return null;
            }
        }

        // Separate method to handle image processing for network transmission
        private static string ProcessImageForTransmission(Bitmap original, ScalingInfo scalingInfo, int screenIndex, int maxFileSizeKB)
        {
            Bitmap processedImage = null;
            bool needsDispose = false;
            
            try
            {
                int targetWidth = original.Width;
                int targetHeight = original.Height;
                
                // More aggressive downscaling for large screens
                if (original.Width > MAX_IMAGE_DIMENSION || original.Height > MAX_IMAGE_DIMENSION)
                {
                    double scale = Math.Min(
                        (double)MAX_IMAGE_DIMENSION / original.Width, 
                        (double)MAX_IMAGE_DIMENSION / original.Height);
                    
                    targetWidth = (int)(original.Width * scale);
                    targetHeight = (int)(original.Height * scale);
                    
                    // Update scaling info
                    scalingInfo.ScaleX = (double)targetWidth / original.Width;
                    scalingInfo.ScaleY = (double)targetHeight / original.Height;
                    scalingInfo.ScaledWidth = targetWidth;
                    scalingInfo.ScaledHeight = targetHeight;
                    
                    // Store scaling info for coordinate translation
                    lock (CacheLock)
                    {
                        ScalingFactors[screenIndex] = scalingInfo;
                    }
                    
                    Console.WriteLine($"Downscaling to: {targetWidth}x{targetHeight}, scale factors: X={scalingInfo.ScaleX:F2}, Y={scalingInfo.ScaleY:F2}");
                    
                    // Resize the image
                    processedImage = ResizeBitmap(original, targetWidth, targetHeight);
                    needsDispose = true;
                }
                else
                {
                    // Use the original image
                    processedImage = original;
                    
                    // Update cache with 1:1 mapping
                    lock (CacheLock)
                    {
                        ScalingFactors[screenIndex] = scalingInfo;
                    }
                }
                
                // Start with adaptive compression based on image size
                int initialQuality = CalculateInitialQuality(processedImage.Width, processedImage.Height);
                Console.WriteLine($"Using initial quality: {initialQuality}");
                
                // Try to compress the image
                byte[] compressedBytes = CompressImageToTargetSize(processedImage, maxFileSizeKB, initialQuality);
                
                if (compressedBytes == null)
                {
                    Console.WriteLine("Initial compression failed, trying progressive fallback...");
                    compressedBytes = FallbackCompression(processedImage, maxFileSizeKB);
                }
                
                if (compressedBytes == null)
                {
                    Console.WriteLine("All compression attempts failed");
                    return null;
                }
                
                // Convert to Base64
                return Convert.ToBase64String(compressedBytes);
            }
            finally
            {
                // Clean up the resized image if we created one
                if (needsDispose && processedImage != null && processedImage != original)
                {
                    processedImage.Dispose();
                }
            }
        }

        // Calculate initial quality based on image dimensions
        private static int CalculateInitialQuality(int width, int height)
        {
            int pixelCount = width * height;
            
            // For very large images, start with lower quality
            if (pixelCount > 1920 * 1080)
                return 40;
            else if (pixelCount > 1280 * 720)
                return 50;
            else
                return DEFAULT_QUALITY;
        }

        // Progressive fallback for challenging compression cases
        private static byte[] FallbackCompression(Bitmap image, int maxFileSizeKB)
        {
            // Try progressively more aggressive approaches
            
            // 1. Try with very low quality
            byte[] result = CompressWithQuality(image, 15);
            if (result != null && result.Length <= maxFileSizeKB * 1024)
                return result;
                
            // 2. Try with extreme downscaling if still too large
            using (var halfSized = ResizeBitmap(image, image.Width / 2, image.Height / 2))
            {
                result = CompressWithQuality(halfSized, 40);
                if (result != null && result.Length <= maxFileSizeKB * 1024)
                    return result;
                    
                // 3. Last resort: extreme downscaling + low quality
                result = CompressWithQuality(halfSized, 15);
                if (result != null && result.Length <= maxFileSizeKB * 1024)
                    return result;
            }
            
            // Nothing worked within the size limit
            return null;
        }
        
        // Helper to compress with specific quality
        private static byte[] CompressWithQuality(Bitmap image, int quality)
        {
            try
            {
                using var ms = new MemoryStream();
                using var encoderParams = CreateEncoderParams(quality);
                image.Save(ms, JpegEncoder, encoderParams);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error compressing image: {ex.Message}");
                return null;
            }
        }

        // Optimized method to get monitor rectangle with caching
        private static Rect GetMonitorRect(int screenIndex)
        {
            lock (CacheLock)
            {
                // Check if cache is still valid (30 seconds)
                if ((DateTime.Now - LastCacheUpdate).TotalSeconds > CacheValiditySeconds)
                {
                    MonitorCache.Clear();
                }

                // Return from cache if available
                if (MonitorCache.TryGetValue(screenIndex, out Rect cachedRect))
                {
                    return cachedRect;
                }
            }

            // If not in cache, enumerate monitors
            Rect monitorRect = new Rect();
            int currentScreenIndex = 0;
            bool monitorFound = false;

            MonitorEnumDelegate callback = (nint hMonitor, nint hdcMonitor, ref Rect lprcMonitor, nint dwData) =>
            {
                if (currentScreenIndex == screenIndex)
                {
                    monitorRect = lprcMonitor;
                    monitorFound = true;
                    return false; // stop enumeration
                }
                currentScreenIndex++;
                return true;
            };

            EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero);

            if (!monitorFound || monitorRect.Width == 0)
                throw new ArgumentException($"Monitor with index {screenIndex} not found.");

            // Update cache
            lock (CacheLock)
            {
                MonitorCache[screenIndex] = monitorRect;
                LastCacheUpdate = DateTime.Now;
            }

            return monitorRect;
        }

        // Cached and lazy-loaded JPEG encoder
        private static readonly Lazy<ImageCodecInfo> LazyJpegEncoder = new Lazy<ImageCodecInfo>(() => 
            ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid));
        
        private static ImageCodecInfo JpegEncoder => LazyJpegEncoder.Value;

        private static EncoderParameters CreateEncoderParams(int quality)
        {
            var p = new EncoderParameters(1);
            p.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            return p;
        }

        // Improved compression algorithm with target size
        private static byte[] CompressImageToTargetSize(Bitmap image, int maxFileSizeKB, int initialQuality = DEFAULT_QUALITY)
        {
            int targetSize = maxFileSizeKB * 1024;
            int minQ = 10;
            int maxQ = 90;
            int initialQ = Math.Min(Math.Max(initialQuality, minQ), maxQ);
            
            byte[] bestBytes = null;
            int attempts = 0;
            
            // First try with initial quality
            using (var ms = new MemoryStream())
            {
                using var encoderParams = CreateEncoderParams(initialQ);
                try
                {
                    image.Save(ms, JpegEncoder, encoderParams);
                    var bytes = ms.ToArray();
                    
                    if (bytes.Length <= targetSize)
                    {
                        // We're under the limit, try binary search for better quality
                        bestBytes = bytes;
                        minQ = initialQ;
                    }
                    else
                    {
                        // Too big, adjust maxQ down
                        maxQ = initialQ;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Initial compression failed: {ex.Message}");
                    // Continue to binary search
                }
            }
            
            // Binary search for optimal quality
            int lastMidQ = -1;
            while (minQ <= maxQ && attempts < MAX_COMPRESSION_ATTEMPTS)
            {
                attempts++;
                int midQ = (minQ + maxQ) / 2;
                
                // Avoid repeating the same quality level
                if (midQ == lastMidQ)
                    break;
                
                lastMidQ = midQ;
                
                using var ms = new MemoryStream();
                using var encoderParams = CreateEncoderParams(midQ);
                
                try
                {
                    image.Save(ms, JpegEncoder, encoderParams);
                    var currentBytes = ms.ToArray();
                    
                    Console.WriteLine($"Compression attempt {attempts}: Quality {midQ}, Size: {currentBytes.Length / 1024}KB (Limit: {maxFileSizeKB}KB)");
                    
                    if (currentBytes.Length <= targetSize)
                    {
                        bestBytes = currentBytes;
                        minQ = midQ + 1; // Try for better quality
                    }
                    else
                    {
                        maxQ = midQ - 1; // Too big, try lower quality
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Compression error at quality {midQ}: {ex.Message}");
                    maxQ = midQ - 1; // Try lower quality
                }
            }
            
            return bestBytes;
        }

        // Optimized BitBlt capture with better error handling
        public static Bitmap CaptureWithBitBlt(Rect monitorRect)
        {
            nint hdcSrc = nint.Zero;
            nint hdcDest = nint.Zero;
            Graphics gDest = null;
            Graphics gSrc = null;
            Bitmap bmp = null;

            try
            {
                // Use 24bpp instead of 32bpp - smaller memory footprint
                bmp = new Bitmap(monitorRect.Width, monitorRect.Height, PixelFormat.Format24bppRgb);
                
                gDest = Graphics.FromImage(bmp);
                gSrc = Graphics.FromHwnd(nint.Zero);

                hdcDest = gDest.GetHdc();
                hdcSrc = gSrc.GetHdc();

                // Capture screen - if this fails, handle it properly
                bool success = BitBlt(hdcDest, 0, 0, monitorRect.Width, monitorRect.Height,
                       hdcSrc, monitorRect.Left, monitorRect.Top, CopyPixelOperation.SourceCopy);

                if (!success)
                {
                    Console.WriteLine("BitBlt operation failed");
                    bmp.Dispose();
                    return null;
                }

                return bmp;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CaptureWithBitBlt: {ex.Message}");
                bmp?.Dispose();
                return null;
            }
            finally
            {
                // Properly release resources in all cases
                if (hdcDest != nint.Zero && gDest != null)
                    gDest.ReleaseHdc(hdcDest);
                
                if (hdcSrc != nint.Zero && gSrc != null)
                    gSrc.ReleaseHdc(hdcSrc);
                
                gDest?.Dispose();
                gSrc?.Dispose();
            }
        }

        // Optimized bitmap resizing for better performance
        private static Bitmap ResizeBitmap(Bitmap source, int maxWidth, int maxHeight)
        {
            if (source == null) return null;
            
            // Calculate aspect ratio and new dimensions
            double ratioX = (double)maxWidth / source.Width;
            double ratioY = (double)maxHeight / source.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = Math.Max(1, (int)(source.Width * ratio));
            int newHeight = Math.Max(1, (int)(source.Height * ratio));

            // Use 24bpp for better compression performance
            var dest = new Bitmap(newWidth, newHeight, PixelFormat.Format24bppRgb);

            try
            {
                using var g = Graphics.FromImage(dest);
                // Performance-optimized settings
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

                // Use a faster drawing method with rectangle specification
                var destRect = new Rectangle(0, 0, newWidth, newHeight);
                var srcRect = new Rectangle(0, 0, source.Width, source.Height);
                g.DrawImage(source, destRect, srcRect, GraphicsUnit.Pixel);

                return dest;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resizing bitmap: {ex.Message}");
                dest.Dispose();
                return null;
            }
        }

        // Delegate to process each monitor
        private delegate bool MonitorEnumDelegate(nint hMonitor, nint hdcMonitor, ref Rect lprcMonitor, nint dwData);

        // Struct for monitor information - aligned with MouseControl.cs
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

        // Updated to match the MouseControl.cs implementation
        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public uint cbSize;  // Changed from int to uint to match MouseControl.cs
            public Rect rcMonitor; // Changed from rcMonitor to Monitor to match MouseControl.cs
            public Rect rcWork;    // Changed from rcWork to Work to match MouseControl.cs
            public uint dwFlags;
        }

        // Method to get available screen indices
        public static int Get_Screen_Indexes()
        {
            int screenCount = 0;
            var screens = new List<Rect>();

            try
            {
                MonitorEnumDelegate callback = (nint hMonitor, nint hdcMonitor, ref Rect lprcMonitor, nint dwData) =>
                {
                    // Retrieve the monitor information
                    MonitorInfo mi = new MonitorInfo();
                    mi.cbSize = (uint)Marshal.SizeOf(mi);
                    
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                        screens.Add(lprcMonitor);
                        Console.WriteLine($"Screen {screenCount}: {lprcMonitor.Left}, {lprcMonitor.Top}, {lprcMonitor.Right}, {lprcMonitor.Bottom}");
                        screenCount++;

                        // Update the cache
                        lock (CacheLock)
                        {
                            MonitorCache[screenCount - 1] = lprcMonitor;
                        }
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
                    Console.WriteLine($"Number of screens: {screenCount}");
                    LastCacheUpdate = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting screen indexes: {ex.Message}");
            }

            return screenCount;
        }
    }
}