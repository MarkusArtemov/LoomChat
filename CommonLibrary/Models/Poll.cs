using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Models
{
    public class Poll
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsClosed { get; set; }

        public int ChannelId { get; set; }
        public Channel Channel { get; set; } = null!;

        public int CreatedByUserId { get; set; }

        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    }
}
