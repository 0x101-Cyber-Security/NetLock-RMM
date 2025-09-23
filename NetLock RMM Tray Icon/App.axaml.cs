using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.IO;
using System.Text.Json;
using NetLock_RMM_Tray_Icon.Config;
using Avalonia.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Media;
using Global.Helper;


namespace NetLock_RMM_Tray_Icon
{
    public partial class App : Application
    {
        private TrayIcon _trayIcon;
        private ActionSidebar? _actionSidebar;

        public class ButtonConfig
        {
            public string? Name { get; set; }
            public string? Action { get; set; }
            public string? ActionDetails { get; set; }
        }
        
        public class TrayIconConfig
        {
            public string? Title { get; set; } = "NetLock RMM";
            public bool? AboutButtonEnabled { get; set; } = true;
            public string? AboutButtonTitle { get; set; } = "About";
            public bool? ExitButtonEnabled { get; set; } = true;
            public string? ExitButtonTitle { get; set; } = "Exit";
        }
        
        public class AboutInterfaceConfig
        {
            public string? WindowTitle { get; set; } = "About NetLock RMM";
            public string? Emoji { get; set; } = "🔒";
            public string? Title { get; set; } = "NetLock RMM";
            public string? Description { get; set; } = " The open-source RMM supporting Windows, Linux & MacOS.";
            public bool? VersionEnabled { get; set; } = false;
            public bool? PolicyEnabled { get; set; } = false;
            public bool? CopyrightTextEnabled { get; set; } = true;
            public string? CopyrightText { get; set; } = null;
            public string? CloseButtonTitle { get; set; } = "Close";
        }
        
        public class ConfigRoot
        {
            public TrayIconConfig? TrayIcon { get; set; }
            public AboutInterfaceConfig? AboutInterface { get; set; }
            public Handler.ChatInterfaceConfig? ChatInterface { get; set; }
            public List<ButtonConfig>? Buttons { get; set; }
        }
        
        public static class AppConfig
        {
            public static TrayIconConfig? TrayConfig { get; set; }
            public static AboutInterfaceConfig? AboutConfig { get; set; }
            public static Handler.ChatInterfaceConfig? ChatConfig { get; set; }
        }
        
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            try
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new ChatWindow();
                    
                    // Initialize ActionSidebar
                    //_actionSidebar = new ActionSidebar();
                    //_actionSidebar.Show();
                    
                    LoadConfig();
                    
                    _trayIcon = new TrayIcon
                    {
                        Icon = new WindowIcon(System.IO.Path.Combine("Assets", "trayicon.ico")),
                        ToolTipText = AppConfig.TrayConfig?.Title ?? "NetLock RMM",
                        IsVisible = true
                    };
                    var menu = new NativeMenu();
                    
                    // Additional buttons from config
                    string configPath = System.IO.Path.Combine(AppContext.BaseDirectory, "config.json");
                    if (File.Exists(configPath))
                    {
                        try
                        {
                            string jsonString = File.ReadAllText(configPath);
                            var configRoot = JsonSerializer.Deserialize<ConfigRoot>(jsonString);
                            if (configRoot?.Buttons != null)
                            {
                                foreach (var button in configRoot.Buttons)
                                {
                                    if (!string.IsNullOrEmpty(button.Name))
                                    {
                                        var menuItem = new NativeMenuItem(button.Name);
                                        menuItem.Click += (_, __) => HandleButtonAction(button.Action, button.ActionDetails);
                                        menu.Items.Add(menuItem);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("OnFrameworkInitializationCompleted", "Config loading failed", ex.ToString());
                        }
                    }
                
                    // Add buttons from config as menu items
                    if (AppConfig.TrayConfig != null && AppConfig.TrayConfig.AboutButtonEnabled == true)
                    {
                        var aboutItem = new NativeMenuItem(AppConfig.TrayConfig.AboutButtonTitle ?? "About");
                        aboutItem.Click += (_, __) => ShowAboutDialog();
                        menu.Items.Add(aboutItem);
                    }

                    // Add exit button from config
                    if (AppConfig.TrayConfig != null && AppConfig.TrayConfig.ExitButtonEnabled == true)
                    {
                        var exitItem = new NativeMenuItem(AppConfig.TrayConfig.ExitButtonTitle ?? "Exit");
                        exitItem.Click += (_, __) => 
                        {
                            _actionSidebar?.Close();
                            desktop.Shutdown();
                        };
                        menu.Items.Add(exitItem);
                    }
                    
                    _trayIcon.Menu = menu;
                }
                
                base.OnFrameworkInitializationCompleted();
                
                var client = new UserClient();
                await client.Local_Server_Connect();
            }
            catch (Exception ex)
            {
                Logging.Error("OnFrameworkInitializationCompleted", "error", ex.ToString());
            }
        }

        private void HandleButtonAction(string? action, string? actionDetails)
        {
            try
            {
                if (string.IsNullOrEmpty(action) || string.IsNullOrEmpty(actionDetails))
                    return;
                
                switch (action.ToLower())
                {
                    case "url":
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(actionDetails) 
                            { 
                                UseShellExecute = true 
                            });
                        }
                        catch (Exception ex)
                        {
                            Logging.Error("HandleButtonAction", "error", ex.ToString());
                        }
                        break;
                    case "start_process":
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(actionDetails) 
                            { 
                                UseShellExecute = true 
                            });
                        }
                        catch (Exception e)
                        {
                            Logging.Error("HandleButtonAction", "error", e.ToString());
                        }
                        break;
                    default:
                        Logging.Error("HandleButtonAction", "error, unknown event", actionDetails);
                        break;
                }   
            }
            catch (Exception ex)
            {
                Logging.Error("HandleButtonAction", "error", ex.ToString());
            }
        }
        
        private void LoadConfig()
        {
            string configPath = System.IO.Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(configPath))
            {
                string jsonString = File.ReadAllText(configPath);
                var configRoot = JsonSerializer.Deserialize<ConfigRoot>(jsonString);
                AppConfig.TrayConfig = configRoot?.TrayIcon;
                AppConfig.AboutConfig = configRoot?.AboutInterface;
                AppConfig.ChatConfig = configRoot?.ChatInterface;
            }
        }
        
        public static void ShowAboutDialog()
        {
            try
            {
                var about = AppConfig.AboutConfig ?? new AboutInterfaceConfig();
                var dialog = new Window
                {
                    Title = about.WindowTitle ?? "About NetLock RMM",
                    Width = 450,
                    MinHeight = 320,
                    MaxHeight = 700,
                    SizeToContent = SizeToContent.Height,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    Background = new SolidColorBrush(Color.Parse("#F0F2F5"))
                };

                var content = new StackPanel
                {
                    Margin = new Thickness(30),
                    Spacing = 15
                };

                content.Children.Add(new TextBlock
                {
                    Text = about.Emoji ?? "🔒",
                    FontSize = 48,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                content.Children.Add(new TextBlock
                {
                    Text = about.Title ?? "NetLock RMM",
                    FontSize = 24,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.Parse("#2C3E50"))
                });

                if (!string.IsNullOrWhiteSpace(about.Description))
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = about.Description,
                        TextAlignment = TextAlignment.Center,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(Color.Parse("#34495E")),
                        Margin = new Thickness(0, 10)
                    });
                }
                // Show policy if enabled
                if (about.PolicyEnabled == true)
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = "By using this software you agree to the NetLock RMM Terms & Privacy Policy.",
                        TextAlignment = TextAlignment.Center,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.Parse("#7F8C8D")),
                        Margin = new Thickness(0, 10, 0, 0)
                    });
                }
                // Show version if enabled
                if (about.VersionEnabled == true)
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = $"Version: {Application_Settings.Version}",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = new SolidColorBrush(Color.Parse("#7F8C8D")),
                        Margin = new Thickness(0, 10, 0, 0)
                    });
                }
                if (about.CopyrightTextEnabled == true)
                {
                    content.Children.Add(new TextBlock
                    {
                        Text = about.CopyrightText ?? $"© {DateTime.Now.Year} Copyright 2025 0x101 Cyber Security - All rights reserved.",
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = new SolidColorBrush(Color.Parse("#95A5A6")),
                        Margin = new Thickness(0, 20, 0, 0)
                    });
                }

                var closeButton = new Button
                {
                    Content = about.CloseButtonTitle ?? "Close",
                    Padding = new Thickness(20, 8),
                    Background = new SolidColorBrush(Color.Parse("#3498DB")),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(4),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                closeButton.Click += (s, e) => dialog.Close();
                content.Children.Add(closeButton);

                dialog.Content = content;
                dialog.ShowDialog((Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
            }
            catch (Exception e)
            {
                Logging.Error("ShowAboutDialog", "error", e.ToString());
            }
        }
    }
}
