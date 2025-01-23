using De.Hsfl.LoomChat.Common.Enums;

namespace De.Hsfl.LoomChat.Common.Models
{
    public class ChannelMember
    {
        public int ChannelId { get; set; }
        public int UserId { get; set; }
        public bool IsArchived { get; set; }
        public ChannelRole Role { get; set; } = ChannelRole.Member;

        public Channel Channel { get; set; }
    }
}
