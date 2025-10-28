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
        private string? _pendingFirstName;
        private string? _pendingLastName;
        private bool _allowClose = false;

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
                    
                    // Set initials from config
                    SetInitials(firstName, lastName);
                }
            }
            catch (Exception ex)
            {
                // Fallback: default name
                var supportNameTextBlock = this.FindControl<TextBlock>("SupportNameTextBlock");
                if (supportNameTextBlock != null)
                    supportNameTextBlock.Text = "Support staff";
                
                // Set fallback initials
                var initialsTextBlock = this.FindControl<TextBlock>("InitialsTextBlock");
                if (initialsTextBlock != null)
                    initialsTextBlock.Text = "S";
                
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
            
            // Apply pending operator name if set
            if (!string.IsNullOrEmpty(_pendingFirstName))
            {
                SetOperatorName(_pendingFirstName, _pendingLastName);
                _pendingFirstName = null;
                _pendingLastName = null;
            }
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
        /// Shows the support overlay (singleton) with optional operator name
        /// </summary>
        public static void ShowSupportOverlay(string? firstName = null, string? lastName = null)
        {
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (_instance == null || !_instance.IsVisible)
                    {
                        _instance?.Close();
                        _instance = new SupportOverlay();
                        
                        // Store pending names to be applied after window loads
                        if (!string.IsNullOrEmpty(firstName))
                        {
                            _instance._pendingFirstName = firstName;
                            _instance._pendingLastName = lastName;
                        }
                        
                        _instance.Show();
                    }
                    else
                    {
                        // Update operator name if provided (window already loaded)
                        if (!string.IsNullOrEmpty(firstName))
                        {
                            _instance.SetOperatorName(firstName, lastName);
                        }
                        
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
        /// Closes the support overlay completely
        /// </summary>
        public static void CloseSupportOverlay()
        {
            try
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    if (_instance != null)
                    {
                        _instance._allowClose = true;
                        _instance.Close();
                        _instance = null;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing support overlay: {ex.Message}");
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            // Only allow closing if explicitly requested via CloseSupportOverlay()
            if (!_allowClose)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Sets the operator name dynamically
        /// </summary>
        public void SetOperatorName(string firstName, string? lastName = null)
        {
            try
            {
                var supportNameTextBlock = this.FindControl<TextBlock>("SupportNameTextBlock");
                if (supportNameTextBlock != null)
                {
                    string fullName = string.IsNullOrWhiteSpace(lastName) 
                        ? firstName 
                        : $"{firstName} {lastName}";
                    supportNameTextBlock.Text = fullName;
                }
                
                // Set initials in avatar circle
                SetInitials(firstName, lastName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting operator name: {ex.Message}");
                Logging.Error("SupportOverlay", "SetOperatorName", ex.ToString());
            }
        }

        /// <summary>
        /// Sets the initials in the avatar circle
        /// </summary>
        private void SetInitials(string firstName, string? lastName = null)
        {
            try
            {
                var initialsTextBlock = this.FindControl<TextBlock>("InitialsTextBlock");
                if (initialsTextBlock != null)
                {
                    string initials = "";
                    
                    if (!string.IsNullOrWhiteSpace(firstName))
                    {
                        initials += firstName[0].ToString().ToUpper();
                    }
                    
                    if (!string.IsNullOrWhiteSpace(lastName))
                    {
                        initials += lastName[0].ToString().ToUpper();
                    }
                    
                    // Fallback if no names provided
                    if (string.IsNullOrWhiteSpace(initials))
                    {
                        initials = "?";
                    }
                    
                    initialsTextBlock.Text = initials;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting initials: {ex.Message}");
                Logging.Error("SupportOverlay", "SetInitials", ex.ToString());
            }
        }
    }
}
