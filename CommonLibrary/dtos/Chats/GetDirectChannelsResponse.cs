using De.Hsfl.LoomChat.Common.Models;
using System.Collections.Generic;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Used when creating a new channel
    /// </summary>
    public class GetDirectChannelsResponse
    {
        public GetDirectChannelsResponse(List<ChannelDto> channels)
        {
            Channels = channels;
        }

        public List<ChannelDto> Channels { get; set; }
    }
}