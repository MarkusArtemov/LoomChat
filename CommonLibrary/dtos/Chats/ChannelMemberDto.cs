using De.Hsfl.LoomChat.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class ChannelMemberDto
    {
        public int ChannelId { get; set; }
        public int UserId { get; set; }
        public bool IsArchived { get; set; }
        public ChannelRole Role { get; set; } = ChannelRole.Member;

    }
}
