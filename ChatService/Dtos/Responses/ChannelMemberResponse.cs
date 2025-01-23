using De.Hsfl.LoomChat.Common.Enums;

namespace De.Hsfl.LoomChat.Chat.Dtos.Responses
{
    /// <summary>
    /// Represents channel membership info for the client
    /// </summary>
    public record ChannelMemberResponse
    {
        public int ChannelId { get; init; }
        public int UserId { get; init; }
        public bool IsArchived { get; init; }
        public ChannelRole Role { get; init; }
    }
}
