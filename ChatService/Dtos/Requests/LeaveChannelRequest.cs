namespace De.Hsfl.LoomChat.Chat.Dtos.Requests
{
    /// <summary>
    /// Used when leaving a channel
    /// </summary>
    public record LeaveChannelRequest
    {
        public int ChannelId { get; init; }
        public bool RemoveMembership { get; init; }
    }
}
