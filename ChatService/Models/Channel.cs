namespace De.Hsfl.LoomChat.Chat.Models
{
    public class Channel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChannelMember> ChannelMembers { get; set; } = new List<ChannelMember>();

        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
}
