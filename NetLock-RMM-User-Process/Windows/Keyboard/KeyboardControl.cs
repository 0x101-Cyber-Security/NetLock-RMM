using NetLock_RMM_User_Process.Windows.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetLock_RMM_User_Process.Helper.Keyboard
{
    internal class KeyboardControl
    {
        // P/Invoke for keybd_event
        [DllImport("user32.dll", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, nuint dwExtraInfo);

        // Constants for keydown and keyup
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        // Virtual key codes for special keys
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

        public static async Task SendKey(byte vk, bool shift)
        {
            try
            {
                if (shift)
                {
                    // Shift gedrückt halten
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYDOWN, nuint.Zero);
                }

                // Taste drücken
                keybd_event(vk, 0, KEYEVENTF_KEYDOWN, nuint.Zero);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, nuint.Zero);

                if (shift)
                {
                    // Shift loslassen
                    keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, nuint.Zero);
                }
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
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_C, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // C drücken
                keybd_event(VK_C, 0, KEYEVENTF_KEYUP, nuint.Zero);         // C loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        public static void SendCtrlV(string content)
        {
            try
            {
                //Console.WriteLine("Sending Ctrl + V with content: " + content);

                if (!string.IsNullOrEmpty(content))
                    User32.SetClipboardText(content); // Places the content on the clipboard

                // Strg + V
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_V, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // V drücken
                keybd_event(VK_V, 0, KEYEVENTF_KEYUP, nuint.Zero);         // V loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
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
                keybd_event(VK_ALT, 0, KEYEVENTF_KEYDOWN, nuint.Zero);     // Alt drücken
                keybd_event(VK_F4, 0, KEYEVENTF_KEYDOWN, nuint.Zero);      // F4 drücken (VK_F4 = 0x73)
                keybd_event(VK_F4, 0, KEYEVENTF_KEYUP, nuint.Zero);        // F4 loslassen
                keybd_event(VK_ALT, 0, KEYEVENTF_KEYUP, nuint.Zero);       // Alt loslassen
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
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_X, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // X drücken
                keybd_event(VK_X, 0, KEYEVENTF_KEYUP, nuint.Zero);         // X loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
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
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_Z, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // Z drücken
                keybd_event(VK_Z, 0, KEYEVENTF_KEYUP, nuint.Zero);         // Z loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
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
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_Y, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // Y drücken
                keybd_event(VK_Y, 0, KEYEVENTF_KEYUP, nuint.Zero);         // Y loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
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
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_A, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // A drücken
                keybd_event(VK_A, 0, KEYEVENTF_KEYUP, nuint.Zero);         // A loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
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
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_S, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // S drücken
                keybd_event(VK_S, 0, KEYEVENTF_KEYUP, nuint.Zero);         // S loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
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
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYDOWN, nuint.Zero);   // Shift drücken
                keybd_event(VK_T, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // T drücken
                keybd_event(VK_T, 0, KEYEVENTF_KEYUP, nuint.Zero);         // T loslassen
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, nuint.Zero);     // Shift loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
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
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_N, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // N drücken
                keybd_event(VK_N, 0, KEYEVENTF_KEYUP, nuint.Zero);         // N loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
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
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_P, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // P drücken
                keybd_event(VK_P, 0, KEYEVENTF_KEYUP, nuint.Zero);         // P loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
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
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Press Ctrl
                keybd_event(VK_F, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // F press
                keybd_event(VK_F, 0, KEYEVENTF_KEYUP, nuint.Zero);         // F release
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Release Ctrl
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Send ctrl+alt+delete
        public static void SendCtrlAltDelete()
        {
            try
            {
                //ScreenControl.User32.SendSAS(true);
                //Thread.Sleep(100); // Wait for the SAS to be sent
                //ScreenControl.User32.SendSAS(AsUser: false);
             
                Console.WriteLine("Ctrl+Alt+Delete sent successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Send ctrl+backspace
        public static void SendCtrlBackspace()
        {
            try
            {
                // Strg + Backspace
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(0x08, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // Backspace drücken (VK_BACK = 0x08)
                keybd_event(0x08, 0, KEYEVENTF_KEYUP, nuint.Zero);         // Backspace loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Send ctrl+arrow left
        public static void SendCtrlArrowLeft()
        {
            try
            {
                // Strg + Pfeil links
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(0x25, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // Pfeil links drücken (VK_LEFT = 0x25)
                keybd_event(0x25, 0, KEYEVENTF_KEYUP, nuint.Zero);         // Pfeil links loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Send ctrl+arrow right
        public static void SendCtrlArrowRight()
        {
            try
            {
                // Strg + Pfeil rechts
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(0x27, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // Pfeil rechts drücken (VK_RIGHT = 0x27)
                keybd_event(0x27, 0, KEYEVENTF_KEYUP, nuint.Zero);         // Pfeil rechts loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Send ctrl+arrow up
        public static void SendCtrlArrowUp()
        {
            try
            {
                // Strg + Pfeil hoch
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(0x26, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // Pfeil hoch drücken (VK_UP = 0x26)
                keybd_event(0x26, 0, KEYEVENTF_KEYUP, nuint.Zero);         // Pfeil hoch loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Send ctrl+arrow down
        public static void SendCtrlArrowDown()
        {
            try
            {
                // Strg + Pfeil runter
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(0x28, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // Pfeil runter drücken (VK_DOWN = 0x28)
                keybd_event(0x28, 0, KEYEVENTF_KEYUP, nuint.Zero);         // Pfeil runter loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Send ctrl+shift+arrow left
        public static void SendCtrlShiftArrowLeft()
        {
            try
            {
                // Strg + Shift + Pfeil links
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYDOWN, nuint.Zero);   // Shift drücken
                keybd_event(0x25, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // Pfeil links drücken (VK_LEFT = 0x25)
                keybd_event(0x25, 0, KEYEVENTF_KEYUP, nuint.Zero);         // Pfeil links loslassen
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, nuint.Zero);     // Shift loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Send ctrl+shift+arrow right
        public static void SendCtrlShiftArrowRight()
        {
            try
            {
                // Strg + Shift + Pfeil rechts
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYDOWN, nuint.Zero);   // Shift drücken
                keybd_event(0x27, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // Pfeil rechts drücken (VK_RIGHT = 0x27)
                keybd_event(0x27, 0, KEYEVENTF_KEYUP, nuint.Zero);         // Pfeil rechts loslassen
                keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, nuint.Zero);     // Shift loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Send ctrl+keyr (reload page)
        public static void SendCtrlR()
        {
            try
            {
                // Strg + R
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, nuint.Zero); // Strg drücken
                keybd_event(0x52, 0, KEYEVENTF_KEYDOWN, nuint.Zero);       // R drücken (VK_R = 0x52)
                keybd_event(0x52, 0, KEYEVENTF_KEYUP, nuint.Zero);         // R loslassen
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, nuint.Zero);   // Strg loslassen
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send key: {ex.Message}");
            }
        }

        // Send text
    }
}
