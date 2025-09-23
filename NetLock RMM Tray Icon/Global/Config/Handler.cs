using System.Text.Json;
using System.IO;
using System;

namespace NetLock_RMM_Tray_Icon.Config;

public class Handler
{
    public class TrayButton
    {
        public string? Name { get; set; }
        public string? Text { get; set; }
        public string? Action  { get; set; }
    }
    
    public static TrayButton[] TrayButtons { get; set; } = new TrayButton[]
    {
        new TrayButton { Name = "open_app", Text = "Open Application", Action = "open_app" },
        new TrayButton { Name = "open_website", Text = "Open Website", Action = "open_website" },
        new TrayButton { Name = "exit", Text = "Exit", Action = "exit" }
    };
    
    public class ChatInterfaceConfig
    {
        public string? LoadingMessage { get; set; } = "NetLock RMM wird geladen...";

        public string? WindowTitle { get; set; } = "NetLock RMM Chat Support";
        public string? Subtitle { get; set; } = "NetLock RMM Support Team";
        public string? OperatorFirstName { get; set; } = "Admin";
        public string? OperatorLastName { get; set; } 
        public string? WelcomeMessage { get; set; } = "Welcome to NetLock RMM Support. How can we assist you today?";
        public string? InputFieldText { get; set; } = "Type your message here...";
        public bool? SettingsEnabled { get; set; } = false;
        public bool? SettingsExportChatHistoryEnabled { get; set; } = false;
        public bool? SettingsCopyChatHistoryEnabled { get; set; } = false;
        public bool? SettingsAboutEnabled { get; set; } = false;
    }
}