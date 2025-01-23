using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class OpenChatWithUserRequest
    {
        public int OwnId { get; set; }
        public int OtherId { get; set; }
        public OpenChatWithUserRequest(int ownId, int otherId) 
        {
            OwnId = ownId;
            OtherId = otherId;
        }
    }
}
