namespace NetLock_RMM_Web_Console.Classes.Remote_Server
{
    public class Config
    {
        public string Server { get; set; } = String.Empty;
        public int Port { get; set; } = 80;
        public bool UseSSL { get; set; } = false;
    }
}
