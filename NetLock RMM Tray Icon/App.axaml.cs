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
using System.Net.Security;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Media;
using Global.Encryption;
using Global.Helper;
using NetLock_RMM_Agent_Comm;


namespace NetLock_RMM_Tray_Icon
{
    public partial class App : Application
    {
        private TrayIcon _trayIcon;
        private ActionSidebar? _actionSidebar;
        
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
                    // Set shutdown mode to prevent automatic shutdown when no window is visible
                    desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    
                    LoadConfig();
                    
                    // Load tray icon from config or use default
                    WindowIcon? trayIconImage = null;
                    try
                    {
                        // Try to load icon from Base64 in config
                        if (!string.IsNullOrEmpty(Handler.AppConfig.TrayConfig?.IconBase64))
                        {
                            string base64Data = Handler.AppConfig.TrayConfig.IconBase64;
                            
                            // Remove data URI prefix if present (e.g., "data:image/png;base64,")
                            if (base64Data.Contains(","))
                            {
                                int commaIndex = base64Data.IndexOf(',');
                                base64Data = base64Data.Substring(commaIndex + 1);
                            }
                            
                            byte[] iconBytes = Convert.FromBase64String(base64Data);
                            using (var ms = new MemoryStream(iconBytes))
                            {
                                var bitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                                trayIconImage = new WindowIcon(bitmap);
                            }
                            Console.WriteLine("Loaded tray icon from Base64 config");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load icon from Base64: {ex.Message}");
                        Logging.Error("OnFrameworkInitializationCompleted", "Failed to load Base64 icon", ex.ToString());
                    }
                    
                    // Fallback to default icon if Base64 loading failed
                    if (trayIconImage == null)
                    {
                        try
                        {
                            trayIconImage = new WindowIcon(System.IO.Path.Combine("Assets", "trayicon.ico"));
                            Console.WriteLine("Using default tray icon from Assets");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to load default icon: {ex.Message}");
                        }
                    }
                    
                    _trayIcon = new TrayIcon
                    {
                        Icon = trayIconImage,
                        ToolTipText = Handler.AppConfig.TrayConfig?.Title ?? "NetLock RMM",
                        IsVisible = true
                    };
                    
                    var menu = new NativeMenu();
                    
                    // Additional buttons from config
                    string configPath = System.IO.Path.Combine(Application_Paths.tray_icon_settings_json_path);
                    
                    if (File.Exists(configPath))
                    {
                        try
                        {
                            string jsonString = File.ReadAllText(configPath);
                            
                            jsonString = String_Encryption.Decrypt(jsonString, Application_Settings.NetLock_Local_Encryption_Key);
                            
                            var configRoot = JsonSerializer.Deserialize<Handler.ConfigRoot>(jsonString);
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
                    if (Handler.AppConfig.TrayConfig != null && Handler.AppConfig.TrayConfig.AboutButtonEnabled == true && Handler.AppConfig.AboutConfig.Enabled == true)
                    {
                        var aboutItem = new NativeMenuItem(Handler.AppConfig.TrayConfig.AboutButtonTitle ?? "About");
                        aboutItem.Click += (_, __) => ShowAboutDialog();
                        menu.Items.Add(aboutItem);
                    }

                    // Add exit button from config
                    if (Handler.AppConfig.TrayConfig != null && Handler.AppConfig.TrayConfig.ExitButtonEnabled == true)
                    {
                        var exitItem = new NativeMenuItem(Handler.AppConfig.TrayConfig.ExitButtonTitle ?? "Exit");
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
                    case "open url":
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
                    case "start process":
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
            string configPath = Application_Paths.tray_icon_settings_json_path;

            try
            {
                if (File.Exists(configPath))
                {
                    string jsonString = File.ReadAllText(configPath);
                
                    jsonString = String_Encryption.Decrypt(jsonString, Application_Settings.NetLock_Local_Encryption_Key);
                
                    var configRoot = JsonSerializer.Deserialize<Handler.ConfigRoot>(jsonString);
                    Handler.AppConfig.TrayConfig = configRoot?.TrayIcon;
                    Handler.AppConfig.AboutConfig = configRoot?.AboutInterface;
                    Handler.AppConfig.ChatConfig = configRoot?.ChatInterface;
                }
            }
            catch (Exception e)
            {
                Logging.Error("LoadConfig", "error", e.ToString());
            }
        }
        
        public static void ShowAboutDialog()
        {
            try
            {
                var about = Handler.AppConfig.AboutConfig ?? new Handler.AboutInterfaceConfig();
                var dialog = new Window
                {
                    Title = about.WindowTitle ?? "About NetLock RMM",
                    Width = 450,
                    MinHeight = 320,
                    MaxHeight = 700,
                    SizeToContent = SizeToContent.Height,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    CanResize = false,
                    Background = new SolidColorBrush(Color.Parse("#F0F2F5")),
                    ShowInTaskbar = true
                };

                // Set window icon from config
                try
                {
                    if (!string.IsNullOrEmpty(Handler.AppConfig.TrayConfig?.IconBase64))
                    {
                        string base64Data = Handler.AppConfig.TrayConfig.IconBase64;
                        
                        // Remove data URI prefix if present (e.g., "data:image/png;base64,")
                        if (base64Data.Contains(","))
                        {
                            int commaIndex = base64Data.IndexOf(',');
                            base64Data = base64Data.Substring(commaIndex + 1);
                        }
                        
                        byte[] iconBytes = Convert.FromBase64String(base64Data);
                        using (var ms = new MemoryStream(iconBytes))
                        {
                            var bitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                            dialog.Icon = new WindowIcon(bitmap);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set window icon for About dialog: {ex.Message}");
                    Logging.Error("ShowAboutDialog", "Failed to set window icon", ex.ToString());
                }

                var content = new StackPanel
                {
                    Margin = new Thickness(30),
                    Spacing = 15
                };

                // Try to load logo from Base64 config, fallback to emoji
                bool logoLoaded = false;
                try
                {
                    if (!string.IsNullOrEmpty(Handler.AppConfig.TrayConfig?.IconBase64))
                    {
                        string base64Data = Handler.AppConfig.TrayConfig.IconBase64;
                        
                        // Remove data URI prefix if present (e.g., "data:image/png;base64,")
                        if (base64Data.Contains(","))
                        {
                            int commaIndex = base64Data.IndexOf(',');
                            base64Data = base64Data.Substring(commaIndex + 1);
                        }
                        
                        byte[] iconBytes = Convert.FromBase64String(base64Data);
                        using (var ms = new MemoryStream(iconBytes))
                        {
                            var bitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                            var logoImage = new Avalonia.Controls.Image
                            {
                                Source = bitmap,
                                Width = 80,
                                Height = 80,
                                HorizontalAlignment = HorizontalAlignment.Center
                            };
                            content.Children.Add(logoImage);
                            logoLoaded = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load logo from Base64 for About dialog: {ex.Message}");
                    Logging.Error("ShowAboutDialog", "Failed to load Base64 logo", ex.ToString());
                }
                
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
                    string version = "Unknown.";

                    if (File.Exists(Application_Paths.netlock_comm_agent_version_txt))
                    {
                        version = File.ReadAllText(Application_Paths.netlock_comm_agent_version_txt);
                        version = String_Encryption.Decrypt(version, Application_Settings.NetLock_Local_Encryption_Key);
                    }
                        
                    content.Children.Add(new TextBlock
                    {
                        Text = $"Version: {version}",
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
                        Text = about.CopyrightText ?? $"© {DateTime.Now.Year} Copyright 0x101 GmbH. All rights reserved.",
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
                
                // Use Show() instead of ShowDialog() since there is no MainWindow (tray-only app)
                dialog.Show();
            }
            catch (Exception e)
            {
                Logging.Error("ShowAboutDialog", "error", e.ToString());
            }
        }
    }
}
