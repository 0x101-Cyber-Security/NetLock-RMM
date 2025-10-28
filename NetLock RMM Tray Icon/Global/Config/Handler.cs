using System.Text.Json;
using System.IO;
using System;
using System.Collections.Generic;

namespace NetLock_RMM_Tray_Icon.Config;

public class Handler
{
    public class ConfigRoot
    {
        public TrayIconConfig? TrayIcon { get; set; }
        public AboutInterfaceConfig? AboutInterface { get; set; }
        public ChatInterfaceConfig? ChatInterface { get; set; }
        public List<TrayButtonConfig>? Buttons { get; set; }
    }
        
    public static class AppConfig
    {
        public static TrayIconConfig? TrayConfig { get; set; }
        public static AboutInterfaceConfig? AboutConfig { get; set; }
        public static ChatInterfaceConfig? ChatConfig { get; set; }
        
        public static RemoteScreenControlConfig? RemoteScreenControlConfig { get; set; }
    }
    
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
        public string? Title { get; set; } = String.Empty;
        public bool? AboutButtonEnabled { get; set; } = false;
        public string? AboutButtonTitle { get; set; } = String.Empty;
        public bool? ExitButtonEnabled { get; set; } = false;
        public string? ExitButtonTitle { get; set; } = String.Empty;
        public string? IconBase64 { get; set; } = String.Empty;
    }
    
    public class ChatInterfaceConfig
    {
        public string? LoadingMessage { get; set; } = String.Empty;

        public string? WindowTitle { get; set; } = String.Empty;
        public string? Subtitle { get; set; } = String.Empty;
        public string? OperatorFirstName { get; set; } = String.Empty;
        public string? OperatorLastName { get; set; }
        public bool? WelcomeMessageEnabled { get; set; } = false;
        public string? WelcomeMessageText { get; set; } = String.Empty;
        public string? InputFieldText { get; set; } = String.Empty;
        public bool? SettingsEnabled { get; set; } = false;
        public bool? SettingsExportChatHistoryEnabled { get; set; } = false;
        public bool? SettingsCopyChatHistoryEnabled { get; set; } = false;
        public bool? SettingsAboutEnabled { get; set; } = false;
    }
    
    public class AboutInterfaceConfig
    {
        public bool? Enabled { get; set; } = false;
        public string? WindowTitle { get; set; } = String.Empty;
        public string? Title { get; set; } = String.Empty;
        public string? Description { get; set; } = String.Empty;
        public bool? VersionEnabled { get; set; } = false;
        public bool? PolicyEnabled { get; set; } = false;
        public bool? CopyrightTextEnabled { get; set; } = false;
        public string? CopyrightText { get; set; } = null;
        public string? CloseButtonTitle { get; set; } = String.Empty;
    }
    
    public class RemoteScreenControlConfig
    {
        public string? RequestTitle { get; set; } = String.Empty;
        public string? RequestMessage { get; set; } = String.Empty;
        public string? AcceptButtonTitle { get; set; } = String.Empty;
        public string? DeclineButtonTitle { get; set; } = String.Empty;
        public string? StopButtonTitle { get; set; } = String.Empty;
        public string? SessionActiveText { get; set; } = String.Empty;
    }
}