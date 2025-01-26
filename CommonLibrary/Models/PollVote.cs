using System;

namespace De.Hsfl.LoomChat.Common.Models
{
    /// <summary>
    /// Repräsentiert eine einzelne abgegebene Stimme (Vote) eines Users für eine bestimmte Option.
    /// </summary>
    public class PollVote
    {
        public int Id { get; set; }

        public int PollId { get; set; }
        public Poll Poll { get; set; } = null!;

        public int PollOptionId { get; set; }
        public PollOption PollOption { get; set; } = null!;

        /// <summary>
        /// Welcher User (UserId) hat hier abgestimmt?
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Wann hat der User abgestimmt?
        /// </summary>
        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    }
}
