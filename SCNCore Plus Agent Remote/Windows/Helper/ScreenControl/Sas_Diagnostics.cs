using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;
using Global.Helper;

namespace Windows.Helper.ScreenControl
{
    internal class Sas_Diagnostics
    {
        [DllImport("kernel32.dll")]
        static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSQueryUserToken(uint SessionId, out IntPtr phToken);


        public static void LogContext()
        {
            try
            {
                uint sessionId = WTSGetActiveConsoleSessionId();
                Logging.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"ActiveConsoleSessionId: {sessionId}");
                Console.WriteLine($"[SAS] ActiveConsoleSessionId = {sessionId}");

                bool isElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
                Logging.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"IsElevated/Administrator: {isElevated}");
                Console.WriteLine($"[SAS] IsElevated/Administrator = {isElevated}");

                bool isLocalSystem = WindowsIdentity.GetCurrent().Name.Equals("NT AUTHORITY\\SYSTEM", StringComparison.OrdinalIgnoreCase);
                Logging.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"IsLocalSystem: {isLocalSystem}");
                Console.WriteLine($"[SAS] Running as LocalSystem = {isLocalSystem}");

                bool gotToken = WTSQueryUserToken(sessionId, out var userToken);
                Logging.Debug("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "LogContext", $"WTSQueryUserToken({sessionId}) -> {gotToken}");
                Console.WriteLine($"[SAS] WTSQueryUserToken({sessionId}) -> {gotToken}");
                if (gotToken) Kernel32.CloseHandle(userToken);
            }
            catch (Exception ex) 
            {
                Logging.Error("Windows.Helper.ScreenControl.Sas_Diagnostics.LogContext", "General error", ex.ToString());

            }
        }
    }
}
