using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using De.Hsfl.LoomChat.Chat.Services;
using System.Security.Claims;

namespace De.Hsfl.LoomChat.Chat.Hubs
{
    [Authorize]
    public class PollHub : Hub
    {
        private readonly PollService _pollService;
        private readonly ChatService _chatService;
        private readonly ILogger<PollHub> _logger;

        public PollHub(PollService pollService,
                       ChatService chatService,
                       ILogger<PollHub> logger)
        {
            _pollService = pollService;
            _chatService = chatService;
            _logger = logger;
        }

        public async Task CreatePoll(int channelId, string title, List<string> options)
        {
            int userId = GetUserId();
            _logger.LogInformation("CreatePoll: ChannelId={ChannelId}, Title={Title}, UserId={UserId}",
                                   channelId, title, userId);

            var poll = await _pollService.CreatePollAsync(channelId, userId, title, options);

            // ChatMessage vom Typ "Poll" anlegen
            await _chatService.CreatePollMessageAsync(channelId, userId, poll.Id);

            // Broadcast an alle im Channel
            await Clients.Group(channelId.ToString())
                         .SendAsync("PollCreated", poll.Title, options);

            _logger.LogInformation("PollCreated broadcast -> Channel={ChannelId}, PollTitle='{Title}'",
                                   channelId, title);
        }

        public async Task Vote(string title, string option)
        {
            int userId = GetUserId();
            _logger.LogInformation("Vote: Title={Title}, Option={Option}, User={UserId}",
                                   title, option, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null)
            {
                _logger.LogWarning("Vote: Poll '{Title}' not found!", title);
                return;
            }

            try
            {
                await _pollService.VoteAsync(pollId.Value, userId, option);

                // Neue Ergebnisse abrufen
                var results = await _pollService.GetResultsAsync(pollId.Value);

                // Broadcast an alle
                await Clients.All.SendAsync("PollUpdated", title, results);
            }
            catch (Exception ex)
            {
                // => Hier könntest du dem Aufrufer eine Fehlermeldung schicken,
                //    wenn z.B. "User already voted." oder "Poll is closed".
                _logger.LogError(ex, "Vote error. Title='{Title}' User={UserId}", title, userId);

                // Optional: Sende an Caller
                await Clients.Caller.SendAsync("PollError", ex.Message);
            }
        }

        public async Task ClosePoll(string title)
        {
            int userId = GetUserId();
            _logger.LogInformation("ClosePoll: Title={Title}, triggered by User={UserId}",
                                   title, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null) return;

            await _pollService.ClosePollAsync(pollId.Value);

            await Clients.All.SendAsync("PollClosed", title);
        }

        public async Task DeletePoll(string title)
        {
            int userId = GetUserId();
            _logger.LogInformation("DeletePoll: Title={Title}, triggered by User={UserId}",
                                   title, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null) return;

            await _pollService.DeletePollAsync(pollId.Value);

            await Clients.All.SendAsync("PollDeleted", title);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        private int GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return claim == null ? 0 : int.Parse(claim.Value);
        }
    }
}
