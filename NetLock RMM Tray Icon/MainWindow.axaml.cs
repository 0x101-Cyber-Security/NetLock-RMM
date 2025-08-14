using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System;

namespace NetLock_RMM_Tray_Icon
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SendButton.Click += SendChatMessageButton_Click;

            

        }

        private void SendChatMessageButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {


            var message = MessageTextBox.Text?.Trim();
            if (!string.IsNullOrEmpty(message))
            {
                var bubble = CreateChatBubble(message, isOwnMessage: true);
                ChatMessagesPanel.Children.Add(bubble);
                MessageTextBox.Text = string.Empty;

                // Optional: simulate automatic response
                Dispatcher.UIThread.Post(async () =>
                {
                    await Task.Delay(1000);
                    var reply = CreateChatBubble("Ich bin ein Bot 👋", isOwnMessage: false);
                    ChatMessagesPanel.Children.Add(reply);
                });

                ChatScrollViewer.ScrollToEnd();
            }
        }

        private async void SimulateIncomingMessage()
        {
            await Task.Delay(1000);
            AddMessageBubble("Das ist eine simulierte Antwort.", isMine: false);
        }

        private Border CreateChatBubble(string text, bool isOwnMessage)
        {
            var timeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm"),
                Foreground = Brushes.LightGray,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var messageText = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400
            };

            // StackPanel for text and time
            var messageStack = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            messageStack.Children.Add(messageText);
            messageStack.Children.Add(timeText);

            var messageBubble = new Border
            {
                Background = isOwnMessage ? Brushes.SteelBlue : Brushes.Gray,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Child = messageStack
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = isOwnMessage ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Spacing = 8
            };

            // Without image only show bubble
            panel.Children.Add(messageBubble);

            return new Border
            {
                Child = panel,
                Margin = new Thickness(4)
            };
        }




        private void AddMessageBubble(string message, bool isMine)
        {
            var bubble = new Border
            {
                Background = isMine ? Brushes.DarkBlue : Brushes.DarkRed,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(6),
                MaxWidth = 400,
                HorizontalAlignment = isMine ? Avalonia.Layout.HorizontalAlignment.Right : Avalonia.Layout.HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.White
                }
            };

            ChatMessagesPanel.Children.Add(bubble);

            // Scroll to bottom
            ChatScrollViewer.ScrollToEnd();
        }

        // On minimize, hide the window instead of closing
        protected override void OnClosing(Avalonia.Controls.WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = true; // Prevent closing
            this.Hide(); // Hide the window instead
        }
    }
}