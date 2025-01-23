namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Used when archiving a channel membership for the current user
    /// </summary>
    public class ArchiveChannelRequest
    {
        public int ChannelId { get; }

        public ArchiveChannelRequest(int channelId)
        {
            ChannelId = channelId;
        }
    }
}
