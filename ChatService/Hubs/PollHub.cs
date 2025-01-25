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
        /// Erstellt eine neue Umfrage in einem bestimmten Channel 
        /// und broadcastet anschließend das Event 'PollCreated'.
        /// </summary>
        public async Task CreatePoll(int channelId, string title, List<string> options)
        {
            _logger.LogInformation("CreatePoll: ChannelId={ChannelId}, Title={Title}", channelId, title);

            // 1) Den aufrufenden User bestimmen
            int userId = GetUserId();
            _logger.LogDebug("CreatePoll: called by user {UserId}", userId);

            // 2) Poll anlegen (z. B. PollService kann CreatedByUserId speichern)
            var poll = await _pollService.CreatePollAsync(channelId, userId, title, options);

            // 3) Nachricht im Chat erzeugen, damit alle sehen, wer die Umfrage gestartet hat
            var content = $"User {userId} hat eine neue Umfrage gestartet: {title}";
            await _chatService.SendMessageAsync(channelId, userId, content);

            // 4) SignalR-Broadcast an die Gruppe (alle in Channel)
            await Clients.Group(channelId.ToString())
                         .SendAsync("PollCreated", poll.Title, options);

            _logger.LogInformation("PollCreated broadcast for ChannelId={ChannelId}, created by UserId={UserId}",
                                   channelId, userId);
        }

        /// <summary>
        /// Stimmt für eine bestimmte Option ab und broadcastet anschließend 'PollUpdated'.
        /// </summary>
        public async Task Vote(string title, string option)
        {
            int userId = GetUserId();
            _logger.LogInformation("Vote: Title={Title}, Option={Option}, User={UserId}", title, option, userId);

            // Poll anhand des Titels finden
            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null)
            {
                _logger.LogWarning("Vote: No poll found with title='{Title}'", title);
                return;
            }

            // Abstimmung im PollService
            await _pollService.VoteAsync(pollId.Value, userId, option);

            // Ergebnisse abfragen
            var results = await _pollService.GetResultsAsync(pollId.Value);

            // Broadcast an alle (oder nur an Clients.Group, wenn passender):
            await Clients.All.SendAsync("PollUpdated", title, results);

            _logger.LogInformation("PollUpdated broadcast for PollTitle={Title}, triggered by User={UserId}", title, userId);
        }

        /// <summary>
        /// Schließt eine Umfrage und broadcastet 'PollClosed'.
        /// Evtl. solltest du prüfen, ob nur der Ersteller oder ein Admin das darf.
        /// </summary>
        public async Task ClosePoll(string title)
        {
            int userId = GetUserId();
            _logger.LogInformation("ClosePoll: Title={Title}, triggered by User={UserId}", title, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null)
            {
                _logger.LogWarning("ClosePoll: No poll found with title='{Title}'", title);
                return;
            }

            // ggf. prüfen, ob userId == Ersteller oder Channel-Owner
            await _pollService.ClosePollAsync(pollId.Value);

            await Clients.All.SendAsync("PollClosed", title);

            _logger.LogInformation("PollClosed broadcast for PollTitle={Title}", title);
        }

        /// <summary>
        /// Löscht eine Umfrage und broadcastet 'PollDeleted'.
        /// Auch hier ggf. Permission checken.
        /// </summary>
        public async Task DeletePoll(string title)
        {
            int userId = GetUserId();
            _logger.LogInformation("DeletePoll: Title={Title}, triggered by User={UserId}", title, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null)
            {
                _logger.LogWarning("DeletePoll: No poll found with title='{Title}'", title);
                return;
            }

            // ggf. prüfen, ob userId == Ersteller oder Channel-Owner
            await _pollService.DeletePollAsync(pollId.Value);

            await Clients.All.SendAsync("PollDeleted", title);

            _logger.LogInformation("PollDeleted broadcast for PollTitle={Title}", title);
        }

        /// <summary>
        /// Automatisch Gruppen beitreten (optional),
        /// wenn du willst, dass alle Poll-Events nur an Channel-Mitglieder gesendet werden.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            // Falls du die Channel-Gruppenlogik anwenden willst, müsstest du hier:
            // 1) userId aus Token lesen
            // 2) channels = ...
            // 3) foreach => Groups.AddToGroupAsync(Context.ConnectionId, channelId.ToString());

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Bei Disconnect ggf. aus Gruppen entfernen o.Ä.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Liest den UserId aus dem JWT (ähnlich wie in ChatHub).
        /// </summary>
        private int GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return claim == null ? 0 : int.Parse(claim.Value);
        }
    }
}
