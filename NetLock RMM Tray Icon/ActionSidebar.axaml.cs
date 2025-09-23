using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace NetLock_RMM_Tray_Icon
{
    public partial class ActionSidebar : Window
    {
        private bool _isMinimized;
        private double _originalHeight;

        public ActionSidebar()
        {
            InitializeComponent();
            _originalHeight = Height;
            
            // Position the sidebar at the right edge of the screen
            PositionSidebar();
            
            // Subscribe to events
            MinimizeButton.Click += OnMinimizeClick;
            ToggleVisibilityButton.Click += OnToggleVisibilityClick;
            
            // Subscribe to action button events
            SystemInfoButton.Click += OnSystemInfoClick;
            ServiceStatusButton.Click += OnServiceStatusClick;
            UpdatesButton.Click += OnUpdatesClick;
            NetworkStatusButton.Click += OnNetworkStatusClick;
            DiskSpaceButton.Click += OnDiskSpaceClick;
            PerformanceButton.Click += OnPerformanceClick;
            SecurityScanButton.Click += OnSecurityScanClick;
            FirewallStatusButton.Click += OnFirewallStatusClick;
            CleanupButton.Click += OnCleanupClick;
            BackupButton.Click += OnBackupClick;
            RestartButton.Click += OnRestartClick;
            RemoteDesktopButton.Click += OnRemoteDesktopClick;
            FileTransferButton.Click += OnFileTransferClick;
        }

        private void PositionSidebar()
        {
            // Get screen dimensions
            var screen = Screens.Primary;
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                
                // Position at the right edge of the screen
                Position = new PixelPoint(
                    (int)(workingArea.X + workingArea.Width - Width),
                    (int)(workingArea.Y + (workingArea.Height - Height) / 2)
                );
            }
        }

        private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        {
            _isMinimized = !_isMinimized;
            
            if (_isMinimized)
            {
                // Find and hide the content areas
                var contentScrollViewer = this.FindControl<ScrollViewer>("ContentScrollViewer");
                var footerBorder = this.FindControl<Border>("FooterBorder");
                
                if (contentScrollViewer != null)
                    contentScrollViewer.IsVisible = false;
                if (footerBorder != null)
                    footerBorder.IsVisible = false;
                    
                Height = 50; // Just show the header
                MinimizeButton.Content = "+";
                ToolTip.SetTip(MinimizeButton, "Maximieren");
            }
            else
            {
                // Find and show the content areas
                var contentScrollViewer = this.FindControl<ScrollViewer>("ContentScrollViewer");
                var footerBorder = this.FindControl<Border>("FooterBorder");
                
                if (contentScrollViewer != null)
                    contentScrollViewer.IsVisible = true;
                if (footerBorder != null)
                    footerBorder.IsVisible = true;
                    
                Height = _originalHeight;
                MinimizeButton.Content = "−";
                ToolTip.SetTip(MinimizeButton, "Minimieren");
            }
            
            // Ensure correct positioning after resize
            PositionSidebar();
        }

        private void OnToggleVisibilityClick(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState.Minimized;
        }

        // Action button event handlers
        private void OnSystemInfoClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("System Info", "Systeminformationen werden abgerufen...");
        }

        private void OnServiceStatusClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("Service Status", "Service-Status wird geprüft...");
        }

        private void OnUpdatesClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("Updates", "Nach Updates wird gesucht...");
        }

        private void OnNetworkStatusClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("Netzwerk Status", "Netzwerkverbindung wird geprüft...");
        }

        private void OnDiskSpaceClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("Festplattenspeicher", "Speicherplatz wird überprüft...");
        }

        private void OnPerformanceClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("Performance", "System-Performance wird analysiert...");
        }

        private void OnSecurityScanClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("Security Scan", "Sicherheitsscan wird gestartet...");
        }

        private void OnFirewallStatusClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("Firewall Status", "Firewall-Status wird geprüft...");
        }

        private void OnCleanupClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("System Cleanup", "System-Bereinigung wird gestartet...");
        }

        private void OnBackupClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("Backup", "Backup wird erstellt...");
        }

        private void OnRestartClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("System Neustart", "System wird neu gestartet...");
        }

        private void OnRemoteDesktopClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("Remote Desktop", "Remote Desktop wird gestartet...");
        }

        private void OnFileTransferClick(object? sender, RoutedEventArgs e)
        {
            ShowNotification("Datei Transfer", "Datei-Transfer wird vorbereitet...");
        }

        private void ShowNotification(string title, string message)
        {
            Dispatcher.UIThread.Post(() =>
            {
                var originalTitle = Title;
                Title = $"{title}: {message}";
                
                DispatcherTimer.Run(() =>
                {
                    Title = originalTitle;
                    return false;
                }, TimeSpan.FromSeconds(3));
            });
        }

        public void ShowSidebar()
        {
            Show();
            Activate();
            PositionSidebar(); // Ensure correct positioning when showing
        }

        public void HideSidebar()
        {
            Hide();
        }
    }
}
