namespace De.Hsfl.LoomChat.Chat.Dtos.Requests
{
    /// <summary>
    /// Used when creating a new channel
    /// </summary>
    public record CreateChannelRequest
    {
        public string Name { get; init; } = null!;
    }
}
