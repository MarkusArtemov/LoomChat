using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class CreateChannelRequest
    {
        public int UserId { get; set; }
        public string ChannelName { get; set; }

        public CreateChannelRequest(int userId, string channelName) 
        {
            UserId = userId;
            ChannelName = channelName;
        }
    }
}
