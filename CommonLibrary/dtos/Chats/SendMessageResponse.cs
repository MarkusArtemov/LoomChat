using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class SendMessageResponse
    {
        public ChannelDto Channel { get; set; }
        public SendMessageResponse(ChannelDto channel) 
        {
            Channel = channel;
        }
    }
}
