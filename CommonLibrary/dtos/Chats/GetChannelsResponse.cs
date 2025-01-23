using De.Hsfl.LoomChat.Common.Models;
using System.Collections.Generic;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Basic channel info for the client
    /// </summary>
    public class GetChannelsResponse
    {
        public GetChannelsResponse(List<ChannelDto> channels) {
            this.Channels = channels;
        }
        public List<ChannelDto> Channels { get; set; }
    }
}