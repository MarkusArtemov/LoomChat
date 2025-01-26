using System;

namespace De.Hsfl.LoomChat.Common.Models
{
    public abstract class ChatMessage
    {
        public int Id { get; set; }

        public int ChannelId { get; set; }
        public Channel Channel { get; set; }

        public int SenderUserId { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
