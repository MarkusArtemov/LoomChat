using System.Collections.Generic;
using System;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public int SenderUserId { get; set; }
        public DateTime SentAt { get; set; }

        public MessageType Type { get; set; }  // => "Text", "Poll"

        // Nur genutzt bei Type=Text
        public string Content { get; set; }

        // Nur genutzt bei Type=Poll
        public int PollId { get; set; }
        public bool IsClosed { get; set; }
        public string PollTitle { get; set; }
        public List<string> PollOptions { get; set; }
    }
}
