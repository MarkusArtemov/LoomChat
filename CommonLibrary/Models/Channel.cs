using System;
using System.Collections.Generic;

namespace De.Hsfl.LoomChat.Common.Models
{
    public class Channel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDmChannel { get; set; } = false;
        public ICollection<ChannelMember> ChannelMembers { get; set; } = new List<ChannelMember>();

        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

        public ICollection<Poll> Polls { get; set; } = new List<Poll>();
    }
}
