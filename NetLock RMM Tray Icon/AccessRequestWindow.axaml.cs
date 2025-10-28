using System;
using System.IO;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Global.Encryption;
using Global.Helper;
using NetLock_RMM_Agent_Comm;
using NetLock_RMM_Tray_Icon.Config;

namespace NetLock_RMM_Tray_Icon
{
    public partial class AccessRequestWindow : Window
    {
        private readonly string _requestId;
        private readonly string _firstName;
        private readonly string _lastName;
        public bool IsAccepted { get; private set; } = false;

        public AccessRequestWindow(string requestId, string firstName = "", string lastName = "")
        {
            _requestId = requestId;
            _firstName = firstName;
            _lastName = lastName;
            InitializeComponent();
            LoadConfiguration();
            LoadAndDisplayIcon();
        }

        private void LoadConfiguration()
        {
            try
            {
                var config = Handler.AppConfig.RemoteScreenControlConfig;

                if (config != null)
                {
                    // Set title
                    if (!string.IsNullOrEmpty(config.RequestTitle))
                    {
                        this.Title = config.RequestTitle;
                        TitleTextBlock.Text = config.RequestTitle;
                    }

                    // Set message
                    if (!string.IsNullOrEmpty(config.RequestMessage))
                    {
                        MessageTextBlock.Text = config.RequestMessage;
                    }

                    // Set button texts
                    if (!string.IsNullOrEmpty(config.AcceptButtonTitle))
                    {
                        AcceptButton.Content = config.AcceptButtonTitle;
                    }

                    if (!string.IsNullOrEmpty(config.DeclineButtonTitle))
                    {
                        DeclineButton.Content = config.DeclineButtonTitle;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load configuration for AccessRequestWindow: {ex.Message}");
            }
        }

        private void LoadAndDisplayIcon()
        {
            try
            {
                string configPath = Application_Paths.tray_icon_settings_json_path;
                
                if (!File.Exists(configPath))
                    return;

                string jsonString = File.ReadAllText(configPath);
                jsonString = String_Encryption.Decrypt(jsonString, Application_Settings.NetLock_Local_Encryption_Key);

                var configRoot = JsonSerializer.Deserialize<Handler.ConfigRoot>(jsonString);
                var trayConfig = configRoot?.TrayIcon;

                if (trayConfig?.IconBase64 != null && !string.IsNullOrEmpty(trayConfig.IconBase64))
                {
                    // Remove "data:image/png;base64," or similar prefix if present
                    string base64Data = trayConfig.IconBase64;
                    if (base64Data.Contains(","))
                    {
                        base64Data = base64Data.Substring(base64Data.IndexOf(",", StringComparison.Ordinal) + 1);
                    }

                    byte[] imageBytes = Convert.FromBase64String(base64Data);
                    using (var stream = new MemoryStream(imageBytes))
                    {
                        var bitmap = new Bitmap(stream);
                        
                        // Set icon in the dialog
                        var iconImage = this.FindControl<Image>("IconImage");
                        if (iconImage != null)
                        {
                            iconImage.Source = bitmap;
                        }
                        
                        // Set window icon (also used in taskbar)
                        this.Icon = new WindowIcon(bitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load icon for AccessRequestWindow: {ex.Message}");
                Logging.Error("AccessRequestWindow", "LoadAndDisplayIcon", ex.ToString());
            }
        }

        private async void AcceptButton_Click(object? sender, RoutedEventArgs e)
        {
            IsAccepted = true;
            
            // Send acceptance to server
            await UserClient.Local_Server_Send_Message($"remote_access_response${_requestId}$accepted${Environment.UserName}");
            
            Console.WriteLine($"Remote access request {_requestId} accepted");
            
            // Show SupportOverlay with operator name
            SupportOverlay.ShowSupportOverlay(_firstName, _lastName);
            
            Close();
        }

        private async void DeclineButton_Click(object? sender, RoutedEventArgs e)
        {
            IsAccepted = false;
            
            // Send decline to server
            await UserClient.Local_Server_Send_Message($"remote_access_response${_requestId}$declined${Environment.UserName}");
            
            Console.WriteLine($"Remote access request {_requestId} declined");
            Close();
        }
    }
}
