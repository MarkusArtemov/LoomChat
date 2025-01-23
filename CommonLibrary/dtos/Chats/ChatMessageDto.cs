using De.Hsfl.LoomChat.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public int SenderUserId { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public Channel Channel { get; set; }
    }
}
