using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Helper
{
    internal class KeyboardControl
    {
        // P/Invoke für keybd_event
        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        // Konstanten für Keydown und Keyup
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        // Virtual-Key-Codes für spezielle Tasten
        private const byte VK_CONTROL = 0x11;
        private const byte VK_SHIFT = 0x10;
        private const byte VK_ALT = 0x12;
        private const byte VK_C = 0x43; // Beispiel für "C"
        private const byte VK_V = 0x56; // Beispiel für "V"
        private const byte VK_F4 = 0x73; // Beispiel für "F4"
        private const byte VK_X = 0x58; // Beispiel für "X"
        private const byte VK_Z = 0x5A; // Beispiel für "Z"
        private const byte VK_A = 0x41; // Beispiel für "A"
        private const byte VK_S = 0x53; // Beispiel für "S"
        private const byte VK_T = 0x54; // Beispiel für "T"
        private const byte VK_N = 0x4E; // Beispiel für "N"
        private const byte VK_P = 0x50; // Beispiel für "P"
        private const byte VK_F = 0x46; // Beispiel für "F"
        private const byte VK_Y = 0x59; // Beispiel für "Y"

        public static async Task SendKey(byte vk)
        {
            try
            {
                keybd_event(vk, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendCtrlC()
        {
            try
            {
                // Strg + C
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_C, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // C drücken
                keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // C loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }   
        }

        public static void SendCtrlV()
        {
            try
            {
                // Strg + V
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_V, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // V drücken
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // V loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendAltF4()
        {
            try
            {
                // Alt + F4
                keybd_event(VK_ALT, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);     // Alt drücken
                keybd_event(VK_F4, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);      // F4 drücken (VK_F4 = 0x73)
                keybd_event(VK_F4, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);        // F4 loslassen
                keybd_event(VK_ALT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);       // Alt loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Hinzufügen von weiteren Tastenkombinationen

        public static void SendCtrlX()
        {
            try
            {
                // Strg + X
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_X, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // X drücken
                keybd_event(VK_X, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // X loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendCtrlZ()
        {
            try
            {
                // Strg + Z
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_Z, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // Z drücken
                keybd_event(VK_Z, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // Z loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendCtrlY()
        {
            try
            {
                // Strg + Y
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_Y, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // Y drücken
                keybd_event(VK_Y, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // Y loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendCtrlA()
        {
            try
            {
                // Strg + A
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_A, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // A drücken
                keybd_event(VK_A, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // A loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendCtrlS()
        {
            try
            {
                // Strg + S
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_S, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // S drücken
                keybd_event(VK_S, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // S loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendCtrlShiftT()
        {
            try
            {
                // Strg + Shift + T
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);   // Shift drücken
                keybd_event(VK_T, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // T drücken
                keybd_event(VK_T, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // T loslassen
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);     // Shift loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendCtrlN()
        {
            try
            {
                // Strg + N
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_N, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // N drücken
                keybd_event(VK_N, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // N loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendCtrlP()
        {
            try
            {
                // Strg + P
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_P, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // P drücken
                keybd_event(VK_P, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // P loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendCtrlF()
        {
            try
            {
                // Strg + F
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Strg drücken
                keybd_event(VK_F, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);       // F drücken
                keybd_event(VK_F, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);         // F loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }
    }
}
