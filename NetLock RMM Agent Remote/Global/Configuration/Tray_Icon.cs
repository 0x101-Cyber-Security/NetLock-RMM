namespace Global.Configuration;

public class Tray_Icon
{
    public class TrayIconConfigRoot
    {
        public Tray_Icon.RemoteScreenControlConfig RemoteScreenControl { get; set; }
    }
    
    static class AppConfig
    {
        public static RemoteScreenControlConfig? RemoteScreenControlConfig { get; set; }
    }
    
    public class RemoteScreenControlConfig
    {
        public string? RequestTitle { get; set; } = "A admin wants to control your screen";
        public string? RequestMessage { get; set; } = "Do you want to allow remote screen control?";
        public string? AcceptButtonTitle { get; set; } = "Accept";
        public string? DeclineButtonTitle { get; set; } = "Decline";
        public string? StopButtonTitle { get; set; } = "Stop Remote Screen Control";
        public string? SessionActiveText { get; set; } = "A admin is controlling your screen";
    }
}