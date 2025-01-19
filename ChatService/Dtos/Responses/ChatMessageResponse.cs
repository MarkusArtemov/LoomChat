using System;

namespace De.Hsfl.LoomChat.Chat.Dtos.Responses
{
    /// <summary>
    /// Basic channel info for the client
    /// </summary>
    public record ChannelResponse
    {
        public int Id { get; init; }
        public string Name { get; init; } = null!;
        public DateTime CreatedAt { get; init; }
    }
}
