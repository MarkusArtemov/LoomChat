using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class SendMessageRequest
    {
        public int UserId { get; set; }
        public string Message { get; set; }
        public int ChannelId { get; set; }

        public SendMessageRequest(int userid, string message, int channelId) 
        {
            UserId = userid;
            Message = message;
            ChannelId = channelId;
        }
    }
}
