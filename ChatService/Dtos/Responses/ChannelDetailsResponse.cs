using System;
using System.Collections.Generic;

namespace De.Hsfl.LoomChat.Chat.Dtos.Responses
{
    /// <summary>
    /// Full channel info, including members and messages
    /// </summary>
    public record ChannelDetailsResponse
    {
        public int Id { get; init; }
        public string Name { get; init; } = null!;
        public DateTime CreatedAt { get; init; }

        public List<ChannelMemberResponse> Members { get; init; } = new();
        public List<ChatMessageResponse> Messages { get; init; } = new();
    }
}
