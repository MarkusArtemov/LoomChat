using System;

namespace De.Hsfl.LoomChat.Chat.Dtos.Responses
{
    /// <summary>
    /// Represents chat message data for the client
    /// </summary>
    public record ChatMessageResponse
    {
        public int Id { get; init; }
        public int ChannelId { get; init; }
        public int SenderUserId { get; init; }
        public string Content { get; init; } = null!;
        public DateTime SentAt { get; init; }
    }
}
