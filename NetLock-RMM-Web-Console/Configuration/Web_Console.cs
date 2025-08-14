namespace NetLock_RMM_Web_Console.Configuration
{
    public class Web_Console
    {
        public static string title = String.Empty;
        public static string language = "en-US";
        public static bool loggingEnabled = false;
        public static string token_service_secret_key = String.Empty; // generated on startup
        public static string publicOverrideUrl = String.Empty; // used to override the public URL for the web console, useful for reverse proxies or load balancers
        public static string agentConfigurationConnectionString = String.Empty; // used for cloud instances to 
    }
}
