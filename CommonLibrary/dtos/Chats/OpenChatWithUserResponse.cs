using De.Hsfl.LoomChat.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class OpenChatWithUserResponse
    {
        public ChannelDto Channel { get; set; }

        public OpenChatWithUserResponse(ChannelDto channel)
        {
            Channel = channel;
        }
    }
}
