namespace De.Hsfl.LoomChat.Chat.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public Channel? Channel { get; set; }
    }
}
