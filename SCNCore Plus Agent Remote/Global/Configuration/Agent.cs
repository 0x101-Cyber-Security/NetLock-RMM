namespace Global.Configuration
{
    public class Agent
    {
        public static bool debug_mode { get; set; } = false; //enables/disables logging
        public static bool ssl { get; set; } = false;
        public static string package_guid { get; set; } = String.Empty;
        public static string communication_servers { get; set; } = String.Empty;
        public static string remote_servers { get; set; } = String.Empty;
        public static string update_servers { get; set; } = String.Empty;
        public static string trust_servers { get; set; } = String.Empty;
        public static string file_servers { get; set; } = String.Empty;
        public static string tenant_guid { get; set; } = String.Empty;
        public static string location_guid { get; set; } = String.Empty;
        public static string language { get; set; } = String.Empty;
        public static string http_https { get; set; } = String.Empty;
        public static string device_name { get; set; } = String.Empty;
        public static string hwid { get; set; } = String.Empty;
        public static string platform { get; set; } = String.Empty;
    }
}
