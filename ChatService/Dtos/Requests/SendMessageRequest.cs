namespace De.Hsfl.LoomChat.Chat.Dtos.Requests
{
    /// <summary>
    /// Used when sending a message to a channel
    /// </summary>
    public record SendMessageRequest
    {
        public int ChannelId { get; init; }
        public string Content { get; init; } = null!;
    }
}
