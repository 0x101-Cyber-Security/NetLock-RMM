using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace NetLock_RMM_Tray_Icon
{
    public partial class App : Application
    {
        private TrayIcon _trayIcon;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();

                // TrayIcon anlegen
                _trayIcon = new TrayIcon
                {
                    Icon = new WindowIcon("C:\\Users\\NicoMak\\NetLock\\NetLock-RMM\\NetLock RMM Tray Icon\\Assets\\trayicon.ico"),
                    ToolTipText = "NetLock RMM",
                    IsVisible = true
                };

                // NativeMenu (Windows/Linux) für Kontextmenü
                var menu = new NativeMenu();

                var openItem = new NativeMenuItem("Öffnen");
                openItem.Click += (_, __) =>
                {
                    desktop.MainWindow.Show();
                    desktop.MainWindow.Activate();
                    desktop.MainWindow.WindowState = WindowState.Normal;
                    desktop.MainWindow.Focus();
                };

                var exitItem = new NativeMenuItem("Beenden");
                exitItem.Click += (_, __) =>
                {
                    desktop.Shutdown();
                };

                var openWebsiteItem = new NativeMenuItem("Website öffnen");
                openWebsiteItem.Click += (_, __) =>
                {
                    // Hier den Code zum Öffnen der Website einfügen
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://netlockrmm.com") { UseShellExecute = true });
                };

                menu.Items.Add(openItem);
                menu.Items.Add(exitItem);
                menu.Items.Add(openWebsiteItem);

                _trayIcon.Menu = menu;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
