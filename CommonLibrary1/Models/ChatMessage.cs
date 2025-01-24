using System;

namespace De.Hsfl.LoomChat.Common.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public Channel Channel { get; set; }
    }
}
