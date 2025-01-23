using De.Hsfl.LoomChat.Common.Enums;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Represents channel membership info for the client
    /// </summary>
    public class ChannelMemberResponse
    {
        public int ChannelId { get; }
        public int UserId { get; }
        public bool IsArchived { get; }
        public ChannelRole Role { get; }

        public ChannelMemberResponse(int channelId, int userId, bool isArchived, ChannelRole role)
        {
            ChannelId = channelId;
            UserId = userId;
            IsArchived = isArchived;
            Role = role;
        }
    }
}
