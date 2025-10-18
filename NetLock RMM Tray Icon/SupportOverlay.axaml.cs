using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using NetLock_RMM_Tray_Icon.Config;
using System.Text.Json;
using System.IO;
using Global.Encryption;
using Global.Helper;
using NetLock_RMM_Agent_Comm;

namespace NetLock_RMM_Tray_Icon
{
    public partial class SupportOverlay : Window
    {
        private static SupportOverlay? _instance;

        public SupportOverlay()
        {
            InitializeComponent();

            // No close button anymore, so no event handler

            // Set window properties
            this.ShowActivated = false;
            this.CanResize = false;
            this.ShowInTaskbar = false;
            this.Topmost = true;

            // Set support name and title
            SetSupportNameFromConfig();
            SetSupportTitleFromConfig();

            // Position at the bottom right of the screen
            this.Loaded += SupportOverlay_Loaded;
        }

        private void SetSupportNameFromConfig()
        {
            try
            {
                var supportNameTextBlock = this.FindControl<TextBlock>("SupportNameTextBlock");
                string configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
                if (File.Exists(configPath))
                {
                    string jsonString = File.ReadAllText(configPath);
                    jsonString = String_Encryption.Decrypt(jsonString, Application_Settings.NetLock_Local_Encryption_Key);
                    
                    using var doc = JsonDocument.Parse(jsonString);
                    var chatInterface = doc.RootElement.GetProperty("ChatInterface");
                    string firstName = chatInterface.GetProperty("OperatorFirstName").GetString() ?? "Support";
                    string lastName = chatInterface.TryGetProperty("OperatorLastName", out var ln) ? ln.GetString() ?? "" : "";
                    string fullName = string.IsNullOrWhiteSpace(lastName) ? firstName : $"{firstName} {lastName}";
                    if (supportNameTextBlock != null)
                        supportNameTextBlock.Text = fullName;
                }
            }
            catch (Exception ex)
            {
                // Fallback: default name
                var supportNameTextBlock = this.FindControl<TextBlock>("SupportNameTextBlock");
                if (supportNameTextBlock != null)
                    supportNameTextBlock.Text = "Support staff";
                
                Console.WriteLine($"Error loading support name: {ex.Message}");
                Logging.Error("SupportOverlay", "SetSupportNameFromConfig", ex.ToString());
            }
        }

        private void SetSupportTitleFromConfig()
        {
            try
            {
                var supportTitleTextBlock = this.FindControl<TextBlock>("SupportTitleTextBlock");
                string configPath = Application_Paths.tray_icon_settings_json_path;
                
                if (File.Exists(configPath))
                {
                    string jsonString = File.ReadAllText(configPath);
                    jsonString = String_Encryption.Decrypt(jsonString, Application_Settings.NetLock_Local_Encryption_Key);

                    using var doc = JsonDocument.Parse(jsonString);
                    var chatInterface = doc.RootElement.GetProperty("ChatInterface");
                    string windowTitle = chatInterface.TryGetProperty("WindowTitle", out var wt) ? wt.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(windowTitle))
                        windowTitle = "Support admin";
                    if (supportTitleTextBlock != null)
                        supportTitleTextBlock.Text = windowTitle;
                }
            }
            catch (Exception ex)
            {
                var supportTitleTextBlock = this.FindControl<TextBlock>("SupportTitleTextBlock");
                if (supportTitleTextBlock != null)
                    supportTitleTextBlock.Text = "Support admin";

                Console.WriteLine($"Error loading support title: {ex.Message}");
                Logging.Error("SupportOverlay", "SetSupportTitleFromConfig", ex.ToString());
            }
        }

        private void SupportOverlay_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            PositionBottomRight();
        }

        private void PositionBottomRight()
        {
            try
            {
                // Ensure the window is fully loaded
                this.UpdateLayout();
                
                var screen = Screens.Primary;
                if (screen != null)
                {
                    var workingArea = screen.WorkingArea;
                    
                    // Get current window size
                    var windowWidth = this.Bounds.Width > 0 ? this.Bounds.Width : this.Width;
                    var windowHeight = this.Bounds.Height > 0 ? this.Bounds.Height : this.Height;
                    
                    // Position at the very bottom right with minimal margin (5px)
                    this.Position = new PixelPoint(
                        (int)(workingArea.Right - windowWidth - 5),
                        (int)(workingArea.Bottom - windowHeight - 5)
                    );
                }
            }
            catch (Exception ex)
            {
                // Fallback: show centered
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Console.WriteLine($"Error positioning overlay: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the support overlay (singleton)
        /// </summary>
        public static void ShowSupportOverlay()
        {
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (_instance == null || !_instance.IsVisible)
                    {
                        _instance?.Close();
                        _instance = new SupportOverlay();
                        _instance.Show();
                    }
                    else
                    {
                        _instance.Activate();
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing support overlay: {ex.Message}");
            }
        }

        /// <summary>
        /// Hides the support overlay
        /// </summary>
        public static void HideSupportOverlay()
        {
            try
            {
                Dispatcher.UIThread.Invoke(() => { _instance?.Hide(); });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hiding support overlay: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes the support overlay completely
        /// </summary>
        public static void CloseSupportOverlay()
        {
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    _instance?.Close();
                    _instance = null;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing support overlay: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the support overlay is currently visible
        /// </summary>
        public static bool IsOverlayVisible => _instance?.IsVisible ?? false;

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // Prevent closing by user
            e.Cancel = true;
            // _instance = null; // Instance remains
        }
    }
}
