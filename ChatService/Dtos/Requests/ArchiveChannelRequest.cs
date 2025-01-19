namespace De.Hsfl.LoomChat.Chat.Dtos.Requests
{
    /// <summary>
    /// Used when archiving a channel membership for the current user
    /// </summary>
    public record ArchiveChannelRequest
    {
        public int ChannelId { get; init; }
    }
}
