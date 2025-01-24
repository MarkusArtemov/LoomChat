using System;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }
}
