using System;
using System.Collections.Generic;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Represents any chat message data for the client (text or poll).
    /// </summary>
    public class ChatMessageResponse
    {
        // Gemeinsame Felder
        public int Id { get; set; }
        public int ChannelId { get; set; }
        public int SenderUserId { get; set; }
        public DateTime SentAt { get; set; }

        // Typ der Nachricht
        public MessageType Type { get; set; }  // z.B. Text, Poll

        // --- Falls Type=Text ---
        public string Content { get; set; }

        // --- Falls Type=Poll ---
        public int PollId { get; set; }
        public bool IsClosed { get; set; }
        public string PollTitle { get; set; }
        public List<string> PollOptions { get; set; } = new List<string>();
    }


}
