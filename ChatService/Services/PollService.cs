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

        /// <summary>
        /// Erstellt eine neue Umfrage für einen Channel, angelegt von userId.
        /// Speichert z.B. CreatedByUserId in der Poll-Entität.
        /// </summary>
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
        /// Findet die Poll-Id anhand des Titels (Beispiel).
        /// In der Praxis lieber PollId direkt vom Client übergeben.
        /// </summary>
        public async Task<int?> FindPollIdByTitleAsync(string title)
        {
            var poll = await _dbContext.Polls.FirstOrDefaultAsync(p => p.Title == title);
            return poll?.Id;
        }

        /// <summary>
        /// User 'userId' stimmt für die Option 'optionText' in der Poll 'pollId' ab.
        /// Aktuell werden nur die Votes hochgezählt.
        /// Wenn du wirklich pro User erfassen willst, wer abgestimmt hat,
        /// brauchst du eine separate Tabelle (z.B. PollVotes).
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

            var opt = poll.Options.FirstOrDefault(o => o.OptionText == optionText);
            if (opt == null)
                throw new Exception("Option not found");

            opt.Votes++;
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("User {UserId} voted for option '{OptionText}' in poll {PollId}. Votes now={Votes}",
                             userId, optionText, pollId, opt.Votes);
        }

        /// <summary>
        /// Schließt die Poll (IsClosed = true).
        /// Evtl. kannst du checken, ob userId == poll.CreatedByUserId oder ChannelOwner.
        /// </summary>
        public async Task ClosePollAsync(int pollId)
        {
            var poll = await _dbContext.Polls.FindAsync(pollId);
            if (poll == null) return;

            poll.IsClosed = true;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("ClosePollAsync: PollId={PollId} was closed.", pollId);
        }

        /// <summary>
        /// Löscht eine Poll + zugehörige Optionen.
        /// Evtl. Permission-Check, ob userId == Ersteller.
        /// </summary>
        public async Task DeletePollAsync(int pollId)
        {
            var poll = await _dbContext.Polls
                .Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Id == pollId);

            if (poll == null) return;

            _dbContext.PollOptions.RemoveRange(poll.Options);
            _dbContext.Polls.Remove(poll);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("DeletePollAsync: PollId={PollId} deleted.", pollId);
        }

        /// <summary>
        /// Liefert ein Dictionary (OptionText => VoteCount) für die Poll.
        /// </summary>
        public async Task<Dictionary<string, int>> GetResultsAsync(int pollId)
        {
            var poll = await _dbContext.Polls
                .Include(p => p.Options)
                .FirstOrDefaultAsync(p => p.Id == pollId);

            if (poll == null)
                throw new Exception("Poll not found");

            _logger.LogDebug("GetResultsAsync: PollId={PollId} => {Count} options found",
                             pollId, poll.Options.Count);

            return poll.Options.ToDictionary(o => o.OptionText, o => o.Votes);
        }
    }
}
