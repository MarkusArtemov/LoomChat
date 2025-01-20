namespace De.Hsfl.LoomChat.Chat.Dtos.Requests
{
    /// <summary>
    /// Used when joining a channel
    /// </summary>
    public record JoinChannelRequest
    {
        public int ChannelId { get; init; }
    }
}
