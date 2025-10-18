using System.Text.Json;
using System.IO;
using System;

namespace NetLock_RMM_Tray_Icon.Config;

public class Handler
{
    public class TrayButtonConfig
    {
        public string? Name { get; set; }
        public string? Text { get; set; }
        public string? Action  { get; set; }
        public string? ActionDetails  { get; set; }
    }
    
    public class TrayIconConfig
    {
        public bool? Enabled { get; set; } = false;
        public string? Title { get; set; } = "NetLock RMM";
        public bool? AboutButtonEnabled { get; set; } = false;
        public string? AboutButtonTitle { get; set; } = "About";
        public bool? ExitButtonEnabled { get; set; } = false;
        public string? ExitButtonTitle { get; set; } = "Exit";
    }
    
    public class ChatInterfaceConfig
    {
        public string? LoadingMessage { get; set; } = "NetLock RMM wird geladen...";

        public string? WindowTitle { get; set; } = "NetLock RMM Chat Support";
        public string? Subtitle { get; set; } = "NetLock RMM Support Team";
        public string? OperatorFirstName { get; set; } = "Admin";
        public string? OperatorLastName { get; set; } 
        public bool? WelcomeMessageEnabled { get; set; } = false;
        public string? WelcomeMessageText { get; set; } = "Welcome to NetLock RMM Support. How can we assist you today?";
        public string? InputFieldText { get; set; } = "Type your message here...";
        public bool? SettingsEnabled { get; set; } = false;
        public bool? SettingsExportChatHistoryEnabled { get; set; } = false;
        public bool? SettingsCopyChatHistoryEnabled { get; set; } = false;
        public bool? SettingsAboutEnabled { get; set; } = false;
    }
    
    public class AboutInterfaceConfig
    {
        public bool? Enabled { get; set; } = false;
        public string? WindowTitle { get; set; } = "About NetLock RMM";
        public string? Emoji { get; set; } = "🔒";
        public string? Title { get; set; } = "NetLock RMM";
        public string? Description { get; set; } = " The open-source RMM supporting Windows, Linux & MacOS.";
        public bool? VersionEnabled { get; set; } = false;
        public bool? PolicyEnabled { get; set; } = false;
        public bool? CopyrightTextEnabled { get; set; } = false;
        public string? CopyrightText { get; set; } = null;
        public string? CloseButtonTitle { get; set; } = "Close";
    }
    
    public class RemoteScreenControlConfig
    {
        public bool? Enabled { get; set; } = false;

        public string? RequestTitle { get; set; } = "A admin wants to control your screen";
        public string? RequestMessage { get; set; } = "Do you want to allow remote screen control?";
        public string? AcceptButtonTitle { get; set; } = "Accept";
        public string? DeclineButtonTitle { get; set; } = "Decline";
        public string? StopButtonTitle { get; set; } = "Stop Remote Screen Control";
        public string? SessionActiveText { get; set; } = "A admin is controlling your screen";
    }
}