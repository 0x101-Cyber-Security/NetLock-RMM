using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace NetLock_RMM_User_Process.Helper.ScreenControl
{
    internal class Sas_Diagnostics
    {
        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);

        public static void LogContext()
        {
            uint sessionId = WTSGetActiveConsoleSessionId();
            Logging.Handler.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"ActiveConsoleSessionId: {sessionId}");
            Console.WriteLine($"[SAS] ActiveConsoleSessionId = {sessionId}");

            bool isElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            Logging.Handler.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"IsElevated/Administrator: {isElevated}");
            Console.WriteLine($"[SAS] IsElevated/Administrator = {isElevated}");

            bool isLocalSystem = WindowsIdentity.GetCurrent().Name.Equals("NT AUTHORITY\\SYSTEM", StringComparison.OrdinalIgnoreCase);
            Logging.Handler.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"IsLocalSystem: {isLocalSystem}");
            Console.WriteLine($"[SAS] Running as LocalSystem = {isLocalSystem}");

            bool gotToken = WTSQueryUserToken(sessionId, out var userToken);
            Logging.Handler.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"WTSQueryUserToken({sessionId}) -> {gotToken}");
            Console.WriteLine($"[SAS] WTSQueryUserToken({sessionId}) -> {gotToken}");
            if (gotToken) CloseHandle(userToken);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);
    }
}
