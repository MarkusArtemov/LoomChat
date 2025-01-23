using De.Hsfl.LoomChat.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class ChannelDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDmChannel { get; set; } = false;

        public string DisplayName { get; set; } = null;
        public ICollection<ChannelMemberDto> ChannelMembers { get; set; } = new List<ChannelMemberDto>();

        public ICollection<ChatMessageDto> ChatMessages { get; set; } = new List<ChatMessageDto>();
    }
}
