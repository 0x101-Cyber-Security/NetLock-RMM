namespace NetLock_RMM_Server
{ 
    public class Application_Settings
    {
        public static string server_version = "2.5.0.2";
        public static string agent_version = "2.5.0.0";
        public static string Local_Encryption_Key = "01234567890123456789012345678901";

        public static bool IsLiveEnvironment = true;
        public static string Members_Portal_Api_Url_Live = "https://api.members.netlockrmm.com";
        public static string Members_Portal_Api_Url_Test = "http://localhost:80";
    }
}
