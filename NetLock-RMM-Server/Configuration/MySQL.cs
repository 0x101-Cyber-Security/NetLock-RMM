namespace NetLock_RMM_Server.Configuration
{
    public class MySQL
    {
        public static string Connection_String = String.Empty;
        public static string Server { get; set; } = String.Empty;
        public static int Port { get; set; } = 3306;
        public static string Database { get; set; } = String.Empty;
        public static string User { get; set; } = String.Empty;
        public static string Password { get; set; } = String.Empty;
        public static string SslMode { get; set; } = String.Empty;
        public static string AdditionalConnectionParameters { get; set; } = String.Empty;
    }
}
