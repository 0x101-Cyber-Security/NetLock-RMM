using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Helper
{
    internal class MouseControl
    {
        // P/Invoke für SetCursorPos
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        // P/Invoke für mouse_event
        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        // Konstanten für die Mausereignisse
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        // P/Invoke für Monitorinformationen
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MonitorInfo
        {
            public uint Size;
            public Rect Monitor;
            public Rect Work;
            public uint Flags;
        }

        public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

        // Methode zum Abrufen aller Bildschirme
        public static Rect[] GetAllScreens()
        {
            var screens = new System.Collections.Generic.List<Rect>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
            {
                MonitorInfo mi = new MonitorInfo();
                mi.Size = (uint)Marshal.SizeOf(mi);
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    screens.Add(mi.Monitor);
                }
                return true;
            }, IntPtr.Zero);

            return screens.ToArray();
        }

        // Angepasste Methode zum Bewegen der Maus auf dem richtigen Bildschirm
        public static async Task MoveMouse(int x, int y, int screenIndex)
        {
            try
            {
                var screens = GetAllScreens();

                if (screenIndex >= screens.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(screenIndex), "Ungültiger Bildschirmindex.");
                }

                var screen = screens[screenIndex];

                // Berechne die absoluten Koordinaten für den angegebenen Bildschirm
                int absoluteX = screen.Left + x;
                int absoluteY = screen.Top + y;

                // Setze den Mauszeiger auf die berechneten absoluten Koordinaten
                SetCursorPos(absoluteX, absoluteY);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to move mouse: {ex.Message}");
            }
        }

        public static async Task LeftClickMouse()
        {
            try
            {
                // Simuliere einen Mausklick
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0); // Linke Maustaste drücken
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);   // Linke Maustaste loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to left click mouse: {ex.Message}");
            }
        }

        // Right click
        public static async Task RightClickMouse()
        {
            try
            {
                // Simuliere einen rechten Mausklick
                mouse_event(0x0008, 0, 0, 0, 0); // Rechte Maustaste drücken
                mouse_event(0x0010, 0, 0, 0, 0); // Rechte Maustaste loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to right click mouse: {ex.Message}");
            }
        }
    }

}
