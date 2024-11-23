namespace NetLock_RMM_Web_Console.Classes.MySQL
{
    public class Config
    {
        public string Server { get; set; } = String.Empty;
        public int Port { get; set; } = 3306;
        public string Database { get; set; } = String.Empty;
        public string User { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;
        public string SslMode { get; set; } = String.Empty;
        public string AdditionalConnectionParameters { get; set; } = String.Empty;
    }
}
