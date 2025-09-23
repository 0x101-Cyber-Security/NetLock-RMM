using System;

namespace NetLock_RMM_Tray_Icon
{
    public class ChatMessage
    {
        public string Text { get; set; } = string.Empty;
        public bool IsOwnMessage { get; set; }
        public DateTime Timestamp { get; set; }

        public ChatMessage(string text, bool isOwnMessage)
        {
            Text = text;
            IsOwnMessage = isOwnMessage;
            Timestamp = DateTime.Now;
        }
    }
}
