using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Models
{
      public class PollOption
    {
        public int Id { get; set; }
        public string OptionText { get; set; } = null!;
        public int Votes { get; set; }

        public int PollId { get; set; }
        public Poll Poll { get; set; } = null!;
    }
}
