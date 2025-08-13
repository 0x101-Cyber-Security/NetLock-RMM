using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace SCNCore_Plus_User_Process.Windows.Helper
{
    internal class Sas_Diagnostics
    {
        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSQueryUserToken(uint SessionId, out nint phToken);

        public static void LogContext()
        {
            uint sessionId = WTSGetActiveConsoleSessionId();
            Handler.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"ActiveConsoleSessionId: {sessionId}");
            Console.WriteLine($"[SAS] ActiveConsoleSessionId = {sessionId}");

            bool isElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            Handler.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"IsElevated/Administrator: {isElevated}");
            Console.WriteLine($"[SAS] IsElevated/Administrator = {isElevated}");

            bool isLocalSystem = WindowsIdentity.GetCurrent().Name.Equals("NT AUTHORITY\\SYSTEM", StringComparison.OrdinalIgnoreCase);
            Handler.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"IsLocalSystem: {isLocalSystem}");
            Console.WriteLine($"[SAS] Running as LocalSystem = {isLocalSystem}");

            bool gotToken = WTSQueryUserToken(sessionId, out var userToken);
            Handler.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"WTSQueryUserToken({sessionId}) -> {gotToken}");
            Console.WriteLine($"[SAS] WTSQueryUserToken({sessionId}) -> {gotToken}");
            if (gotToken) CloseHandle(userToken);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(nint hObject);
    }
}
