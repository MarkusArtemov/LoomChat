using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using De.Hsfl.LoomChat.Chat.Services;

namespace De.Hsfl.LoomChat.Chat.Hubs
{
    [Authorize]
    public class PollHub : Hub
    {
        private readonly PollService _pollService;
        private readonly ChatService _chatService;
        private readonly ILogger<PollHub> _logger;

        public PollHub(PollService pollService, ChatService chatService, ILogger<PollHub> logger)
        {
            _pollService = pollService;
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Erstellt eine neue Umfrage (Poll) in einem Channel, 
        /// legt anschließend eine PollMessage an (Chatverlauf), 
        /// und broadcastet "PollCreated" an alle.
        /// </summary>
        public async Task CreatePoll(int channelId, string title, List<string> options)
        {
            _logger.LogInformation("CreatePoll: ChannelId={ChannelId}, Title={Title}", channelId, title);

            // 1) User aus JWT ermitteln
            int userId = GetUserId();
            _logger.LogDebug("CreatePoll aufgerufen von UserId={UserId}", userId);

            // 2) Den eigentlichen Poll in der DB anlegen (Tabelle Poll + PollOptions)
            var poll = await _pollService.CreatePollAsync(channelId, userId, title, options);

            // 3) Zusätzlich eine PollMessage im Chat anlegen, damit der Client 
            //    ein Element vom Type=Poll (ChatMessageDto) sehen kann.
            await _chatService.CreatePollMessageAsync(channelId, userId, poll.Id);

            // 4) Broadcast an alle Clients in der Channel-Gruppe
            await Clients.Group(channelId.ToString())
                         .SendAsync("PollCreated", poll.Title, options);

            _logger.LogInformation(
                "PollCreated broadcast -> Channel={ChannelId}, PollTitle='{Title}'",
                channelId, title
            );
        }

        /// <summary>
        /// User votet für eine bestimmte Option -> Zähler hoch + "PollUpdated" Broadcast.
        /// </summary>
        public async Task Vote(string title, string option)
        {
            int userId = GetUserId();
            _logger.LogInformation("Vote: Title={Title}, Option={Option}, User={UserId}", title, option, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null)
            {
                _logger.LogWarning("Vote fehlgeschlagen: Keine Poll mit Title='{Title}' gefunden.", title);
                return;
            }

            // Abstimmung -> Votes++
            await _pollService.VoteAsync(pollId.Value, userId, option);

            // Neue Ergebnisse abrufen
            var results = await _pollService.GetResultsAsync(pollId.Value);

            // Broadcast an alle (oder an Clients.Group, wenn gewünscht)
            await Clients.All.SendAsync("PollUpdated", title, results);

            _logger.LogInformation("PollUpdated broadcast -> Poll='{Title}', Voter={UserId}", title, userId);
        }

        /// <summary>
        /// Schließt eine Umfrage, keine weiteren Votes möglich -> "PollClosed".
        /// </summary>
        public async Task ClosePoll(string title)
        {
            int userId = GetUserId();
            _logger.LogInformation("ClosePoll: Title={Title}, triggered by User={UserId}", title, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null) return;

            await _pollService.ClosePollAsync(pollId.Value);

            await Clients.All.SendAsync("PollClosed", title);
            _logger.LogInformation("PollClosed -> Poll='{Title}'", title);
        }

        /// <summary>
        /// Löscht eine Umfrage komplett -> "PollDeleted".
        /// </summary>
        public async Task DeletePoll(string title)
        {
            int userId = GetUserId();
            _logger.LogInformation("DeletePoll: Title={Title}, triggered by User={UserId}", title, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null) return;

            await _pollService.DeletePollAsync(pollId.Value);

            await Clients.All.SendAsync("PollDeleted", title);
            _logger.LogInformation("PollDeleted -> Poll='{Title}'", title);
        }

        // Optional: Gruppenbeitritt, falls Sie wollen
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Extrahiert die UserId aus dem JWT.
        /// </summary>
        private int GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return claim == null ? 0 : int.Parse(claim.Value);
        }
    }
}
