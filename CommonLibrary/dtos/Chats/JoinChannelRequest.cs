namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Used when joining a channel
    /// </summary>
    public class JoinChannelRequest
    {
        public int ChannelId { get; }

        public JoinChannelRequest(int channelId)
        {
            ChannelId = channelId;
        }
    }
}
