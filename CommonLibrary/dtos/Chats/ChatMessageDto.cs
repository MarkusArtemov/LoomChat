using System;
using System.Collections.Generic;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public int SenderUserId { get; set; }
        public DateTime SentAt { get; set; }
        public MessageType Type { get; set; }  // => "Text", "Poll"

        // Nur bei Text:
        public string Content { get; set; } = string.Empty;

        // Nur bei Poll:
        public int? PollId { get; set; }
        public bool IsClosed { get; set; }
        public string PollTitle { get; set; } = string.Empty;
        public List<string> PollOptions { get; set; } = new List<string>();

        // NEU: Damit das UI zwischen Vote/Ergebnis umschalten kann:
        public bool HasUserVoted { get; set; } = false;
    }
}
