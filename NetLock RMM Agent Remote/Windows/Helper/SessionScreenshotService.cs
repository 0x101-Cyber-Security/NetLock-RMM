using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Global.Helper;
using Microsoft.Win32.SafeHandles;
using Windows.Helper.ScreenControl;

public class SessionScreenshotService
{
    private const int SW_SHOW = 5;

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint SessionId, out IntPtr Token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(
        IntPtr hExistingToken,
        uint dwDesiredAccess,
        IntPtr lpTokenAttributes,
        int ImpersonationLevel,
        int TokenType,
        out IntPtr phNewToken);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool DestroyEnvironmentBlock(IntPtr lpEnvironment);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessAsUser(
        IntPtr hToken,
        string lpApplicationName,
        string lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
        int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    private const int GENERIC_ALL_ACCESS = 0x10000000;
    private const int TOKEN_DUPLICATE = 0x0002;
    private const int TOKEN_QUERY = 0x0008;
    private const int TOKEN_ASSIGN_PRIMARY = 0x0001;

    private const int SecurityImpersonation = 2;
    private const int TokenPrimary = 1;

    private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;

    [StructLayout(LayoutKind.Sequential)]
    private struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    public static void Run()
    {
        while (true)
        {
            try
            {
                uint sessionId = 0; // Immer Session 0 für Login Screen

                // Kein User-Token nötig, da Dienst als SYSTEM in Session 0 läuft
                // Direkt Screenshot machen
                MakeScreenshot(IntPtr.Zero, sessionId);
            }
            catch (Exception ex)
            {
                Logging.Error("SessionScreenshotService", "Fehler beim Screenshot machen", ex.ToString());
                Console.WriteLine("Fehler: " + ex.Message);
            }

            Thread.Sleep(10000); // alle 10 Sekunden wiederholen
        }
    }


    public static void MakeScreenshot(IntPtr userToken, uint sessionId)
    {
        try
        {
            /*var result = Win32Interop.CreateInteractiveSystemProcess(
                  _rcBinaryPath +
                      $" --mode Unattended" +
                      $" --host {_connectionInfo.Host}" +
                      $" --requester-name \"{requesterName}\"" +
                      $" --org-name \"{orgName}\"" +
                      $" --org-id \"{orgId}\"" +
                      $" --session-id \"{sessionId}\"" +
                      $" --access-key \"{accessKey}\"",
                  targetSessionId: targetSessionId,
                  hiddenWindow: false,
                  out _);
            */

            int width = 1920;  // Bildschirmgröße ggf. dynamisch ermitteln
            int height = 1080;

            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
            IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);
            IntPtr hOld = SelectObject(hdcMem, hBitmap);

            bool success = BitBlt(hdcMem, 0, 0, width, height, hdcScreen, 0, 0, SRCCOPY);

            if (success)
            {
                try
                {
                    Bitmap bmp = Image.FromHbitmap(hBitmap);

                    string filePath = Path.Combine("C:\\temp",
                     $"screenshot_session{sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    bmp.Save(filePath, ImageFormat.Png);
                }
                catch (Exception ex)
                { 
                    Logging.Error("SessionScreenshotService", "Fehler beim Screenshot speichern", ex.ToString());
                    Console.WriteLine("Fehler: " + ex.Message);
                }
                finally
                {
                    // Clean up
                    SelectObject(hdcMem, hOld);
                    DeleteObject(hBitmap);
                    DeleteDC(hdcMem);
                    ReleaseDC(IntPtr.Zero, hdcScreen);
                }
            }
            else
            {
                Console.WriteLine("Fehler beim Screenshot machen");
                Logging.Error("SessionScreenshotService", "Fehler beim Screenshot machen", "BitBlt failed");
            }
        }
        catch (Exception ex)
        {
            Logging.Error("SessionScreenshotService", "Fehler beim Screenshot machen", ex.ToString());
            Console.WriteLine("Fehler: " + ex.Message);
        }
    }

    // GDI und Kernel32 Funktionen:

    private const int SRCCOPY = 0x00CC0020;

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);
}
