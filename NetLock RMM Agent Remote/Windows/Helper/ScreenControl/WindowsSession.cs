using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using Global.Helper;
using Global.Configuration;

namespace Windows.Helper.ScreenControl;

[DataContract]
public enum WindowsSessionType
{
    Console = 1,
    RDP = 2
}

[DataContract]
public class WindowsSession
{
    [DataMember(Name = "ID")]
    public uint Id { get; set; }
    [DataMember(Name = "Name")]
    public string Name { get; set; } = string.Empty;
    [DataMember(Name = "Type")]
    public WindowsSessionType Type { get; set; }
    [DataMember(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets all active Windows sessions (including login screen)
    /// </summary>
    public static List<uint> GetActiveSessions()
    {
        try
        {
            var sessions = new List<uint>();
        
            nint pSessionInfo = nint.Zero;
            int sessionCount = 0;

            if (WTSAPI32.WTSEnumerateSessions(nint.Zero, 0, 1, ref pSessionInfo, ref sessionCount) != 0)
            {
                nint current = pSessionInfo;
            
                for (int i = 0; i < sessionCount; i++)
                {
                    var sessionInfo = Marshal.PtrToStructure<WTSAPI32.WTS_SESSION_INFO>(current);
                
                    // Only active sessions and console (including login screen)
                    if (sessionInfo.State == WTSAPI32.WTS_CONNECTSTATE_CLASS.WTSActive || 
                        sessionInfo.State == WTSAPI32.WTS_CONNECTSTATE_CLASS.WTSConnected)
                    {
                        sessions.Add(sessionInfo.SessionID);
                    
                        if (Agent.debug_mode)
                            Logging.Debug("WindowsSession.GetActiveSessions", 
                                "Found session", 
                                $"SessionId: {sessionInfo.SessionID}, State: {sessionInfo.State}, Station: {sessionInfo.pWinStationName}");
                    }
                
                    current = nint.Add(current, Marshal.SizeOf<WTSAPI32.WTS_SESSION_INFO>());
                }
            
                WTSAPI32.WTSFreeMemory(pSessionInfo);
            }
        
            return sessions;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Logging.Error("WindowsSession.GetActiveSessions", "Error enumerating sessions", e.ToString());
            return new List<uint>();
        }
    }

    /// <summary>
    /// Gets the session ID for a specific process
    /// </summary>
    public static uint GetProcessSessionId(int processId)
    {
        uint sessionId = 0;
        Kernel32.ProcessIdToSessionId((uint)processId, ref sessionId);
        return sessionId;
    }

    /// <summary>
    /// Checks if a user is logged into a specific session (not just login screen)
    /// Session 0 = Services/System, Session 1+ with WTSActive = Logged in user
    /// </summary>
    public static bool IsUserLoggedIntoSession(uint sessionId)
    {
        nint buffer = nint.Zero;
        uint bytesReturned = 0;

        try
        {
            // Session 0 is always the system/services session (login screen)
            if (sessionId == 0)
            {
                if (Agent.debug_mode)
                    Logging.Debug("WindowsSession.IsUserLoggedIntoSession", 
                        "Session check", 
                        $"SessionId: {sessionId}, IsLoggedIn: False (System session)");
                return false;
            }

            // Check the connect state for this session
            if (WTSAPI32.WTSQuerySessionInformation(nint.Zero, sessionId, 
                WTSAPI32.WTS_INFO_CLASS.WTSConnectState, out buffer, out bytesReturned))
            {
                if (buffer != nint.Zero)
                {
                    int connectState = Marshal.ReadInt32(buffer);
                    WTSAPI32.WTS_CONNECTSTATE_CLASS state = (WTSAPI32.WTS_CONNECTSTATE_CLASS)connectState;

                    // WTSActive means a user is actively logged in and working
                    // WTSDisconnected means a user was logged in but is now disconnected (still logged in)
                    bool isLoggedIn = (state == WTSAPI32.WTS_CONNECTSTATE_CLASS.WTSActive ||
                                      state == WTSAPI32.WTS_CONNECTSTATE_CLASS.WTSDisconnected);

                    if (Agent.debug_mode)
                        Logging.Debug("WindowsSession.IsUserLoggedIntoSession",
                            "Session check",
                            $"SessionId: {sessionId}, State: {state}, IsLoggedIn: {isLoggedIn}");

                    return isLoggedIn;
                }
            }
        }
        catch (Exception ex)
        {
            if (Agent.debug_mode)
                Logging.Error("WindowsSession.IsUserLoggedIntoSession", 
                    "Error checking session login status", 
                    ex.ToString());
        }
        finally
        {
            if (buffer != nint.Zero)
                WTSAPI32.WTSFreeMemory(buffer);
        }
        
        // Default: Sessions > 0 are usually user sessions
        bool defaultIsLoggedIn = sessionId > 0;

        if (Agent.debug_mode)
            Logging.Debug("WindowsSession.IsUserLoggedIntoSession", 
                "Session check (fallback)", 
                $"SessionId: {sessionId}, IsLoggedIn: {defaultIsLoggedIn} (default based on session ID)");
        
        return defaultIsLoggedIn;
    }
}
