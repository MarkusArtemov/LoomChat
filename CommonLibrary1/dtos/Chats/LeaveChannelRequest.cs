namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Used when leaving a channel
    /// </summary>
    public class LeaveChannelRequest
    {
        public int ChannelId { get; }
        public bool RemoveMembership { get; }

        public LeaveChannelRequest(int channelId, bool removeMembership)
        {
            ChannelId = channelId;
            RemoveMembership = removeMembership;
        }
    }
}
