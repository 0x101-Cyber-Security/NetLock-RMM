using System;
using System.Runtime.InteropServices;
using Global.Helper;

namespace Windows.Helper.ScreenControl;
// Credits for https://github.com/immense/Remotely for already doing most of the work. That really helped me saving time on this. I will rebuild the classes on a sooner date.

public static class WTSAPI32
{
    public static nint WTS_CURRENT_SERVER_HANDLE = nint.Zero;

    public enum WTS_CONNECTSTATE_CLASS
    {
        WTSActive,
        WTSConnected,
        WTSConnectQuery,
        WTSShadow,
        WTSDisconnected,
        WTSIdle,
        WTSListen,
        WTSReset,
        WTSDown,
        WTSInit
    }

    public enum WTS_INFO_CLASS
    {
        WTSInitialProgram,
        WTSApplicationName,
        WTSWorkingDirectory,
        WTSOEMId,
        WTSSessionId,
        WTSUserName,
        WTSWinStationName,
        WTSDomainName,
        WTSConnectState,
        WTSClientBuildNumber,
        WTSClientName,
        WTSClientDirectory,
        WTSClientProductId,
        WTSClientHardwareId,
        WTSClientAddress,
        WTSClientDisplay,
        WTSClientProtocolType,
        WTSIdleTime,
        WTSLogonTime,
        WTSIncomingBytes,
        WTSOutgoingBytes,
        WTSIncomingFrames,
        WTSOutgoingFrames,
        WTSClientInfo,
        WTSSessionInfo
    }


    [DllImport("wtsapi32.dll", SetLastError = true)]
    public static extern int WTSEnumerateSessions(
        nint hServer,
        int Reserved,
        int Version,
        ref nint ppSessionInfo,
        ref int pCount);

    [DllImport("wtsapi32.dll", ExactSpelling = true, SetLastError = false)]
    public static extern void WTSFreeMemory(nint memory);

    [DllImport("Wtsapi32.dll")]
    public static extern bool WTSQuerySessionInformation(nint hServer, uint sessionId, WTS_INFO_CLASS wtsInfoClass, out nint ppBuffer, out uint pBytesReturned);

    [DllImport("wtsapi32.dll", SetLastError = true)]
    static extern nint WTSOpenServer(string pServerName);

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    public static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool RevertToSelf();

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, out IntPtr DuplicateTokenHandle);

    public const int SecurityImpersonation = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct WTS_SESSION_INFO
    {
        public uint SessionID;
        [MarshalAs(UnmanagedType.LPStr)]
        public string pWinStationName;
        public WTS_CONNECTSTATE_CLASS State;
    }

    public static IntPtr? TryGetUserToken()
    {
        try
        {
            uint sessionId = WTSGetActiveConsoleSessionId();
            if (sessionId == 0xFFFFFFFF)
            {
                Logging.Debug("Windows.Helper.ScreenControl.WTSAPI32.TryGetUserToken", "TryGetUserToken", "WTSGetActiveConsoleSessionId() returned 0xFFFFFFFF");
                Console.WriteLine("❌ Keine aktive Konsole gefunden.");
                return null;
            }

            if (WTSQueryUserToken(sessionId, out var token))
            {
                return token;
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                Logging.Debug("Windows.Helper.ScreenControl.WTSAPI32.TryGetUserToken", "TryGetUserToken", $"WTSQueryUserToken({sessionId}) failed with error {error}");
                Console.WriteLine($"❌ WTSQueryUserToken({sessionId}) failed with {error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Logging.Debug("Windows.Helper.ScreenControl.WTSAPI32.TryGetUserToken", "TryGetUserToken", $"Exception: {ex.Message}");
            Console.WriteLine("Fehler beim Tokenholen: " + ex);
            return null;
        }
    }
}
