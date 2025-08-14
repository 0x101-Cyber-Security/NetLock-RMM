using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Global.Helper
{
    public static class UserSessionHelper
    {
        private const int WTS_CURRENT_SERVER_HANDLE = 0;

        private enum WTS_CONNECTSTATE_CLASS
        {
            Active,
            Connected,
            ConnectQuery,
            Shadow,
            Disconnected,
            Idle,
            Listen,
            Reset,
            Down,
            Init
        }

        private enum WTS_INFO_CLASS
        {
            WTSUserName = 5,
            WTSDomainName = 7
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WTS_SESSION_INFO
        {
            public int SessionId;
            public string pWinStationName;
            public WTS_CONNECTSTATE_CLASS State;
        }

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSEnumerateSessions(
            IntPtr hServer,
            int Reserved,
            int Version,
            out IntPtr ppSessionInfo,
            out int pCount);

        [DllImport("wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr pointer);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQuerySessionInformation(
            IntPtr hServer,
            int sessionId,
            WTS_INFO_CLASS wtsInfoClass,
            out IntPtr ppBuffer,
            out uint pBytesReturned);

        public static string GetActiveUser()
        {
            IntPtr pSessions = IntPtr.Zero;
            int sessionCount = 0;

            try
            {
                if (!WTSEnumerateSessions(IntPtr.Zero, 0, 1, out pSessions, out sessionCount))
                    return null;

                int dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));
                IntPtr current = pSessions;

                for (int i = 0; i < sessionCount; i++)
                {
                    WTS_SESSION_INFO session = (WTS_SESSION_INFO)Marshal.PtrToStructure(current, typeof(WTS_SESSION_INFO));
                    current = (IntPtr)((long)current + dataSize);

                    if (session.State == WTS_CONNECTSTATE_CLASS.Active)
                    {
                        string user = QuerySessionString(session.SessionId, WTS_INFO_CLASS.WTSUserName);
                        string domain = QuerySessionString(session.SessionId, WTS_INFO_CLASS.WTSDomainName);

                        if (!string.IsNullOrWhiteSpace(user))
                            return string.IsNullOrWhiteSpace(domain) ? user : $"{domain}\\{user}";
                    }
                }
            }
            finally
            {
                if (pSessions != IntPtr.Zero)
                    WTSFreeMemory(pSessions);
            }

            return null;
        }

        private static string QuerySessionString(int sessionId, WTS_INFO_CLASS infoClass)
        {
            IntPtr buffer = IntPtr.Zero;
            uint bytesReturned = 0;

            try
            {
                if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, infoClass, out buffer, out bytesReturned) && bytesReturned > 1)
                {
                    string result = Marshal.PtrToStringAnsi(buffer);
                    return result;
                }
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    WTSFreeMemory(buffer);
            }

            return null;
        }
    }
}
