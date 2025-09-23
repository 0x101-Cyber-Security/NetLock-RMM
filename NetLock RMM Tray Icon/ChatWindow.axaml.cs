using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using NetLock_RMM_Tray_Icon.Config;

namespace NetLock_RMM_Tray_Icon
{
    public partial class ChatWindow : Window
    {
        private bool _isTyping;
        private bool _isFirstTimeOpen = true;
        private double _expandedWidth = 500;
        private double _collapsedWidth = 60;

        public ChatWindow()
        {
            InitializeComponent();

            // Window options for normal window functionality
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.WindowState = WindowState.Normal;
            this.CanResize = true;
            this.ShowInTaskbar = true;
            this.SystemDecorations = SystemDecorations.Full;
            this.Topmost = false;

            MessageTextBox.KeyDown += MessageTextBox_KeyDown;
            MessageTextBox.TextChanged += MessageTextBox_TextChanged;
            SendButton.Click += SendChatMessageButton_Click;
            RegisterSettingsMenuHandlers();
            this.Opacity = 0;
            this.Loaded += MainWindow_Loaded;
            this.Closing += ChatWindow_Closing;
        }

        public class ConfigRoot
        {
            public Handler.ChatInterfaceConfig? ChatInterface { get; set; }
        }

        public static class AppConfig
        {
            public static Handler.ChatInterfaceConfig? ChatConfig { get; set; }
        }

        private void LoadConfig()
        {
            string configPath = System.IO.Path.Combine(AppContext.BaseDirectory, "config.json");

            if (File.Exists(configPath))
            {
                string jsonString = File.ReadAllText(configPath);
                var configRoot = JsonSerializer.Deserialize<ConfigRoot>(jsonString);
                AppConfig.ChatConfig = configRoot?.ChatInterface;

                var config = AppConfig.ChatConfig;
                if (config == null) return;

                if (!string.IsNullOrEmpty(config.WindowTitle))
                    this.Title = config.WindowTitle;

                if (!string.IsNullOrEmpty(config.InputFieldText))
                    MessageTextBox.Watermark = config.InputFieldText;

                if (config.SettingsEnabled.HasValue)
                    SettingsButton.IsVisible = config.SettingsEnabled.Value;

                if (config.SettingsAboutEnabled.HasValue)
                    AboutMenuItem.IsVisible = config.SettingsAboutEnabled.Value;
            }
        }

        private void RegisterSettingsMenuHandlers()
        {
            var aboutMenuItem = this.FindControl<MenuItem>("AboutMenuItem");
            if (aboutMenuItem != null)
                aboutMenuItem.Click += AboutMenuItem_Click;
        }

        #region Settings Menu Handlers

        private void AboutMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            App.ShowAboutDialog();
        }

        #endregion

        #region Chat Logic

        private async void MainWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            LoadConfig();
            //SetWindowTopRight();

            if (_isFirstTimeOpen)
            {
                await StartupAnimation();
                _isFirstTimeOpen = false;
            }
            else
            {
                this.Opacity = 1;
                AddWelcomeMessage();
            }
        }

        private void ChatWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            // Prevent closing, just hide the window
            e.Cancel = true;
            this.Hide();
        }

        private void SetWindowTopRight()
        {
            try
            {
                var screen = Screens.Primary;
                if (screen != null)
                {
                    var workingArea = screen.WorkingArea;
                    var windowWidth = this.Bounds.Width > 0 ? this.Bounds.Width : this.Width;
                    this.Position = new PixelPoint(
                        (int)(workingArea.Right - windowWidth - 5),
                        (int)(workingArea.Y + 5)
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error positioning chat window: {ex.Message}");
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void MessageTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    var textBox = sender as TextBox;
                    if (textBox != null)
                    {
                        int caretIndex = textBox.CaretIndex;
                        string currentText = textBox.Text ?? "";
                        textBox.Text = currentText.Insert(caretIndex, "\n");
                        textBox.CaretIndex = caretIndex + 1;
                    }

                    e.Handled = true;
                }
                else
                {
                    e.Handled = true;
                    SendMessage();
                }
            }
        }

        private void MessageTextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            SendButton.IsEnabled = !string.IsNullOrWhiteSpace(MessageTextBox.Text);
        }

        private void SendChatMessageButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            var message = MessageTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                AddChatMessage(message, true);
                MessageTextBox.Text = string.Empty;
                ScrollToBottom();
                SimulateTypingAndResponse(message);
            }
        }

        private void AddWelcomeMessage()
        {
            var welcomeText = AppConfig.ChatConfig?.WelcomeMessage ?? "Willkommen!";
            AddChatMessage(welcomeText, false);
        }

        private void AddChatMessage(string message, bool isOwnMessage)
        {
            var messageContainer = CreateMessageBubble(message, isOwnMessage);
            ChatMessagesPanel.Children.Add(messageContainer);
            AnimateNewMessage(messageContainer);
        }

        private Border CreateMessageBubble(string text, bool isOwnMessage)
        {
            var timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#95A5A6")),
                Margin = new Thickness(0, 2, 0, 0)
            };

            var messageText = new TextBlock
            {
                Text = text,
                FontSize = 14,
                Foreground = isOwnMessage ? Brushes.White : new SolidColorBrush(Color.Parse("#2C3E50")),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400,
                LineHeight = 20
            };

            var textContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 2
            };
            textContainer.Children.Add(messageText);
            textContainer.Children.Add(timeText);

            var bubble = new Border
            {
                Background = isOwnMessage
                    ? new SolidColorBrush(Color.Parse("#3498DB"))
                    : new SolidColorBrush(Color.Parse("#ECF0F1")),
                CornerRadius = new CornerRadius(18, 18, isOwnMessage ? 4 : 18, isOwnMessage ? 18 : 4),
                Padding = new Thickness(16, 12),
                Child = textContainer,
                Margin = new Thickness(isOwnMessage ? 60 : 0, 0, isOwnMessage ? 0 : 60, 0)
            };

            var messageContainer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = isOwnMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Spacing = 8
            };

            if (!isOwnMessage)
            {
                var avatar = new Ellipse
                {
                    Width = 32,
                    Height = 32,
                    Fill = new SolidColorBrush(Color.Parse("#3498DB")),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 2)
                };

                try
                {
                    avatar.Fill = new ImageBrush
                    {
                        Source = new Avalonia.Media.Imaging.Bitmap("Assets/avatar.png"),
                        Stretch = Stretch.UniformToFill
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler beim Laden des Avatars: {ex.Message}");
                    avatar.Fill = new SolidColorBrush(Color.Parse("#ECF0F1"));
                }

                messageContainer.Children.Add(avatar);
            }

            messageContainer.Children.Add(bubble);

            return new Border
            {
                Child = messageContainer,
                Margin = new Thickness(0, 4)
            };
        }

        private async void SimulateTypingAndResponse(string userMessage)
        {
            ShowTypingIndicator();
            await Task.Delay(1500 + new Random().Next(1500));
            HideTypingIndicator();

            var response = GenerateBotResponse(userMessage);
            AddChatMessage(response, false);
            ScrollToBottom();
        }

        private void ShowTypingIndicator()
        {
            if (_isTyping) return;

            _isTyping = true;
            var typingIndicator = CreateTypingIndicator();
            typingIndicator.Name = "TypingIndicator";
            ChatMessagesPanel.Children.Add(typingIndicator);
            ScrollToBottom();
        }

        private void HideTypingIndicator()
        {
            if (!_isTyping) return;

            _isTyping = false;
            for (int i = ChatMessagesPanel.Children.Count - 1; i >= 0; i--)
            {
                if (ChatMessagesPanel.Children[i] is Border border && border.Name == "TypingIndicator")
                {
                    ChatMessagesPanel.Children.RemoveAt(i);
                    break;
                }
            }
        }

        private Border CreateTypingIndicator()
        {
            var dots = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            for (int i = 0; i < 3; i++)
            {
                var dot = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(Color.Parse("#95A5A6"))
                };
                dots.Children.Add(dot);
            }

            var typingText = new TextBlock
            {
                Text = "Bot schreibt",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#95A5A6")),
                Margin = new Thickness(0, 0, 8, 0)
            };

            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { typingText, dots }
            };

            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#ECF0F1")),
                CornerRadius = new CornerRadius(18, 18, 18, 4),
                Padding = new Thickness(16, 8),
                Child = content,
                Margin = new Thickness(42, 4, 60, 4),
                Name = "TypingIndicator"
            };
        }

        private string GenerateBotResponse(string userMessage)
        {
            var lowerMessage = userMessage.ToLower();

            if (lowerMessage.Contains("hallo") || lowerMessage.Contains("hi"))
                return "Hallo! Schön Sie kennenzulernen. Wie kann ich Ihnen bei NetLock RMM helfen?";

            if (lowerMessage.Contains("problem") || lowerMessage.Contains("fehler"))
                return
                    "Ich verstehe, dass Sie ein Problem haben. Können Sie mir mehr Details dazu geben? Welche Fehlermeldung erhalten Sie?";

            if (lowerMessage.Contains("installation"))
                return
                    "Gerne helfe ich Ihnen bei der Installation! Welches Betriebssystem verwenden Sie und auf welchem Schritt sind Sie hängengeblieben?";

            if (lowerMessage.Contains("verbindung") || lowerMessage.Contains("connection"))
                return
                    "Verbindungsprobleme können verschiedene Ursachen haben. Prüfen Sie bitte:\n\n• Ihre Internetverbindung\n• Firewall-Einstellungen\n• Proxy-Konfiguration\n\nWelche Fehlermeldung sehen Sie genau?";

            if (lowerMessage.Contains("lizenz") || lowerMessage.Contains("license"))
                return
                    "Für Lizenzfragen wenden Sie sich bitte an unser Sales-Team:\n\n📧 sales@netlockrmm.com\n📞 +49 123 456 789\n\nIch kann Ihnen bei technischen Fragen weiterhelfen.";

            if (lowerMessage.Contains("danke") || lowerMessage.Contains("thank"))
                return "Gerne geschehen! Gibt es noch etwas anderes, womit ich Ihnen helfen kann? 😊";

            return
                "Das ist eine interessante Frage! Lassen Sie mich Ihnen dazu weiterhelfen. Können Sie mir etwas mehr Kontext geben, damit ich Ihnen gezielter helfen kann?";
        }

        private void AnimateNewMessage(Border messageContainer)
        {
            messageContainer.Opacity = 0;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                for (double opacity = 0; opacity <= 1; opacity += 0.1)
                {
                    messageContainer.Opacity = opacity;
                    await Task.Delay(20);
                }
            });
        }

        private void ScrollToBottom()
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(100);
                ChatScrollViewer.ScrollToEnd();
                await Task.Delay(50);
                ChatScrollViewer.ScrollToEnd();
            });
        }

        #endregion

        #region Startup Animation

        private async Task StartupAnimation()
        {
            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid != null)
            {
                foreach (var child in mainGrid.Children)
                {
                    if (child is Control control)
                    {
                        control.Opacity = 0;
                        if (control is Border border)
                        {
                            border.RenderTransform = new TranslateTransform(0, 30);
                        }
                    }
                }
            }

            this.Opacity = 1;
            await ShowLoadingAnimation();
            await AnimateUiElements();
            await Task.Delay(300);
            AddWelcomeMessage();
        }

        private async Task ShowLoadingAnimation()
        {
            var loadingOverlay = CreateLoadingOverlay();

            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid != null)
            {
                // Overlay for loading animation
                Grid.SetRowSpan(loadingOverlay, 3);
                Grid.SetColumnSpan(loadingOverlay, 1);
                mainGrid.Children.Add(loadingOverlay);
            }

            await Task.Delay(2000);

            if (mainGrid != null)
            {
                await AnimateElementOut(loadingOverlay);
                mainGrid.Children.Remove(loadingOverlay);
            }
        }

        private Border CreateLoadingOverlay()
        {
            var logoContainer = new Border
            {
                Width = 80,
                Height = 80,
                CornerRadius = new CornerRadius(40),
                Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(Color.Parse("#3498DB"), 0),
                        new GradientStop(Color.Parse("#2980B9"), 0.5),
                        new GradientStop(Color.Parse("#1ABC9C"), 1)
                    }
                },
                Child = new TextBlock
                {
                    Text = "💬",
                    FontSize = 32,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White
                }
            };

            var rotateTransform = new RotateTransform();
            logoContainer.RenderTransform = rotateTransform;
            logoContainer.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            StartSpinAnimation(rotateTransform);

            var loadingText = new TextBlock
            {
                Text = AppConfig.ChatConfig?.LoadingMessage ?? "Wird geladen...",
                FontSize = 18,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#2C3E50")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var dotsContainer = CreateLoadingDots();

            var content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children = { logoContainer, loadingText, dotsContainer }
            };

            return new Border
            {
                Background = new SolidColorBrush(Color.Parse("#F0F2F5")) { Opacity = 0.95 },
                Child = content,
                Name = "LoadingOverlay"
            };
        }

        private StackPanel CreateLoadingDots()
        {
            var dotsContainer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 8,
                Margin = new Thickness(0, 15, 0, 0)
            };

            for (int i = 0; i < 3; i++)
            {
                var dot = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = new SolidColorBrush(Color.Parse("#3498DB"))
                };

                dotsContainer.Children.Add(dot);
                StartDotBounceAnimation(dot, i * 200);
            }

            return dotsContainer;
        }

        private async void StartSpinAnimation(RotateTransform transform)
        {
            while (transform.Angle < 720)
            {
                transform.Angle += 4;
                await Task.Delay(16);
            }
        }

        private async void StartDotBounceAnimation(Ellipse dot, int delay)
        {
            await Task.Delay(delay);

            var transform = new ScaleTransform();
            dot.RenderTransform = transform;
            dot.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            for (int i = 0; i < 10; i++)
            {
                for (double scale = 1.0; scale <= 1.3; scale += 0.1)
                {
                    transform.ScaleX = scale;
                    transform.ScaleY = scale;
                    await Task.Delay(25);
                }

                for (double scale = 1.3; scale >= 1.0; scale -= 0.1)
                {
                    transform.ScaleX = scale;
                    transform.ScaleY = scale;
                    await Task.Delay(25);
                }

                await Task.Delay(150);
            }
        }

        private async Task AnimateUiElements()
        {
            var mainGrid = this.FindControl<Grid>("MainGrid");
            if (mainGrid == null) return;

            Control?[] elements =
            {
                mainGrid.Children.Count > 0 ? mainGrid.Children[0] as Control : null,
                mainGrid.Children.Count > 1 ? mainGrid.Children[1] as Control : null,
                mainGrid.Children.Count > 2 ? mainGrid.Children[2] as Control : null
            };

            for (int i = 0; i < elements.Length; i++)
            {
                if (elements[i] != null)
                {
                    await AnimateElementIn(elements[i]!, i * 150);
                }
            }
        }

        private async Task AnimateElementIn(Control element, int delay)
        {
            await Task.Delay(delay);

            if (element.RenderTransform is TranslateTransform transform)
            {
                for (double y = 30; y >= 0; y -= 3)
                {
                    transform.Y = y;
                    element.Opacity = Math.Min(1.0, (30 - y) / 30);
                    await Task.Delay(16);
                }
            }

            element.Opacity = 1;
            element.RenderTransform = new TranslateTransform(0, 0);
        }

        private async Task AnimateElementOut(Control element)
        {
            for (double opacity = 1.0; opacity >= 0; opacity -= 0.1)
            {
                element.Opacity = opacity;
                await Task.Delay(20);
            }
        }

        #endregion
    }
}
