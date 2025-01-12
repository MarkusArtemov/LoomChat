using De.Hsfl.LoomChat.Chat.Enums;

namespace De.Hsfl.LoomChat.Chat.Models
{
    public class ChannelMember
    {
        public int ChannelId { get; set; }
        public int UserId { get; set; }
        public bool IsArchived { get; set; }
        public ChannelRole Role { get; set; } = ChannelRole.Member;

        public Channel? Channel { get; set; }
    }
}
