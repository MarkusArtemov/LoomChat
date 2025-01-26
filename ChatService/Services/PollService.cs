using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using De.Hsfl.LoomChat.Chat.Persistence;
using De.Hsfl.LoomChat.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace De.Hsfl.LoomChat.Chat.Services
{
    public class PollService
    {
        private readonly ChatDbContext _dbContext;
        private readonly ILogger<PollService> _logger;

        public PollService(ChatDbContext dbContext, ILogger<PollService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Poll> CreatePollAsync(int channelId, int userId, string title, List<string> options)
        {
            _logger.LogInformation("CreatePollAsync: ChannelId={ChannelId}, UserId={UserId}, Title={Title}",
                                   channelId, userId, title);

            var poll = new Poll
            {
                ChannelId = channelId,
                CreatedByUserId = userId,
                Title = title,
                CreatedAt = DateTime.UtcNow
            };

            // Optionen hinzufügen
            foreach (var opt in options)
            {
                poll.Options.Add(new PollOption { OptionText = opt });
            }

            _dbContext.Polls.Add(poll);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Poll created (Id={PollId}) for ChannelId={ChannelId} by UserId={UserId}",
                                   poll.Id, channelId, userId);
            return poll;
        }

        /// <summary>
        /// Liefert den PollId zur gegebenen Poll-Title (falls 1:1).
        /// </summary>
        public async Task<int?> FindPollIdByTitleAsync(string title)
        {
            var poll = await _dbContext.Polls.FirstOrDefaultAsync(p => p.Title == title);
            return poll?.Id;
        }

        /// <summary>
        /// User 'userId' stimmt für die Option 'optionText' in der Poll 'pollId' ab.
        /// Jetzt mit Prüfung, ob er schon abgestimmt hat (1 Vote/Benutzer).
        /// </summary>
        public async Task VoteAsync(int pollId, int userId, string optionText)
        {
            _logger.LogInformation("VoteAsync: PollId={PollId}, UserId={UserId}, Option={OptionText}",
                                   pollId, userId, optionText);

            var poll = await _dbContext.Polls
                .Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Id == pollId);

            if (poll == null)
                throw new Exception("Poll not found");
            if (poll.IsClosed)
                throw new Exception("Poll is closed");

            // Hat der User bereits abgestimmt?
            bool alreadyVoted = await _dbContext.PollVotes
                .AnyAsync(v => v.PollId == pollId && v.UserId == userId);
            if (alreadyVoted)
                throw new Exception("User already voted.");

            // Option suchen
            var opt = poll.Options.FirstOrDefault(o => o.OptionText == optionText);
            if (opt == null)
                throw new Exception("Option not found");

            // Votes hochzählen
            opt.Votes++;

            // PollVote-Eintrag anlegen
            var vote = new PollVote
            {
                PollId = poll.Id,
                PollOptionId = opt.Id,
                UserId = userId,
                VotedAt = DateTime.UtcNow
            };
            _dbContext.PollVotes.Add(vote);

            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("User {UserId} voted for '{OptionText}' in poll {PollId}. Votes now={Votes}",
                             userId, optionText, pollId, opt.Votes);
        }

        public async Task ClosePollAsync(int pollId)
        {
            var poll = await _dbContext.Polls.FindAsync(pollId);
            if (poll == null) return;

            poll.IsClosed = true;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("ClosePollAsync: PollId={PollId} was closed.", pollId);
        }

        public async Task DeletePollAsync(int pollId)
        {
            var poll = await _dbContext.Polls
                .Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Id == pollId);
            if (poll == null) return;

            // Optional: zugehörige PollVotes löschen
            var votes = await _dbContext.PollVotes
                .Where(v => v.PollId == pollId)
                .ToListAsync();
            _dbContext.PollVotes.RemoveRange(votes);

            // Optionen löschen
            _dbContext.PollOptions.RemoveRange(poll.Options);
            _dbContext.Polls.Remove(poll);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("DeletePollAsync: PollId={PollId} deleted.", pollId);
        }

        /// <summary>
        /// Gibt ein Dictionary: OptionText => Votes
        /// </summary>
        public async Task<Dictionary<string, int>> GetResultsAsync(int pollId)
        {
            var poll = await _dbContext.Polls
                .Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Id == pollId);

            if (poll == null)
                throw new Exception("Poll not found");

            _logger.LogDebug("GetResultsAsync: PollId={PollId} => {Count} options",
                             pollId, poll.Options.Count);

            var dict = new Dictionary<string, int>();
            foreach (var opt in poll.Options)
            {
                dict[opt.OptionText] = opt.Votes;
            }
            return dict;
        }

        /// <summary>
        /// Prüft, ob der gegebene User in der Poll bereits abgestimmt hat.
        /// </summary>
        public async Task<bool> HasUserVotedAsync(int pollId, int userId)
        {
            return await _dbContext.PollVotes
                .AnyAsync(v => v.PollId == pollId && v.UserId == userId);
        }
    }
}
