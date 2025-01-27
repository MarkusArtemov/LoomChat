using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using De.Hsfl.LoomChat.Chat.Services;
using De.Hsfl.LoomChat.Common.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace De.Hsfl.LoomChat.Chat.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;
        private readonly PollService _pollService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ChatService chatService, PollService pollService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _pollService = pollService;
            _logger = logger;
        }

        // ===============================
        // ==========   Chat   ===========
        // ===============================
        public async Task SendMessageToChannel(SendMessageRequest request)
        {
            try
            {
                int userId = GetUserId();
                string userName = GetUserName();

                _logger.LogInformation(
                    "SendMessageToChannel. Channel={ChannelId}, RequestUserId={RequestUserId}, RealUserId={RealUserId}, Msg={Message}",
                    request.ChannelId, request.UserId, userId, request.Message
                );

                var msgResponse = await _chatService.SendMessageAsync(
                    request.ChannelId,
                    userId,
                    request.Message
                );

                // Broadcast an alle im Channel
                await Clients.Group(request.ChannelId.ToString()).SendAsync(
                    "ReceiveChannelMessage",
                    msgResponse.ChannelId,
                    msgResponse.SenderUserId,
                    userName,
                    msgResponse.Content,
                    msgResponse.SentAt
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessageToChannel. ChannelId={ChannelId}", request.ChannelId);
                throw;
            }
        }

        public async Task JoinChannel(JoinChannelRequest request)
        {
            _logger.LogInformation(
                "JoinChannel. ConnId={ConnId}, ChannelId={ChannelId}",
                Context.ConnectionId, request.ChannelId
            );

            // Tritt der SignalR-Gruppe bei
            await Groups.AddToGroupAsync(Context.ConnectionId, request.ChannelId.ToString());

            int userId = GetUserId();
            var messages = await _chatService.GetMessagesForChannelAsync(request.ChannelId, userId);

            _logger.LogDebug(
                "User joined channel {ChannelId}. Found {Count} messages. (UserId={UserId})",
                request.ChannelId, messages.Count, userId
            );

            // Schicke dem Caller alle Nachrichten (inkl. HasUserVoted bei Polls)
            await Clients.Caller.SendAsync("ChannelHistory", request.ChannelId, messages);
        }

        public async Task LeaveChannel(LeaveChannelRequest request)
        {
            _logger.LogInformation(
                "LeaveChannel. ConnId={ConnId}, ChannelId={ChannelId}, RemoveMembership={RemoveMembership}",
                Context.ConnectionId, request.ChannelId, request.RemoveMembership
            );

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, request.ChannelId.ToString());

            if (request.RemoveMembership)
            {
                int userId = GetUserId();
                await _chatService.RemoveUserFromChannelAsync(request.ChannelId, userId);
            }

            await Clients.Caller.SendAsync("LeftChannel", request.ChannelId, request.RemoveMembership);
        }

        public async Task ArchiveChannel(ArchiveChannelRequest request)
        {
            _logger.LogInformation("ArchiveChannel. ChannelId={ChannelId}", request.ChannelId);

            int userId = GetUserId();
            var success = await _chatService.ArchiveChannelForUserAsync(request.ChannelId, userId);

            await Clients.Caller.SendAsync("ChannelArchived", request.ChannelId, success);
        }

        // ===============================
        // ==========   Poll   ===========
        // ===============================
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

                // Broadcast an alle in dem Channel
                // (Alternativ: await Clients.All.SendAsync("PollUpdated", ...) falls alle?)
                // Da wir Polls an die Channel-Gruppe senden wollen:
                var poll = await _chatService.GetPollByIdAsync(pollId.Value);
                if (poll != null)
                {
                    await Clients.Group(poll.ChannelId.ToString())
                                 .SendAsync("PollUpdated", title, results);
                }
            }
            catch (Exception ex)
            {
                // => "User already voted." oder "Poll is closed", etc.
                _logger.LogError(ex, "Vote error. Title='{Title}' User={UserId}", title, userId);

                // Optional: Meldung an Caller
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

            // Broadcast an die jeweilige Channel-Gruppe
            var poll = await _chatService.GetPollByIdAsync(pollId.Value);
            if (poll != null)
            {
                await Clients.Group(poll.ChannelId.ToString())
                             .SendAsync("PollClosed", title);
            }
        }

        public async Task DeletePoll(string title)
        {
            int userId = GetUserId();
            _logger.LogInformation("DeletePoll: Title={Title}, triggered by User={UserId}",
                                   title, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null) return;

            // ChannelId merken
            var poll = await _chatService.GetPollByIdAsync(pollId.Value);
            if (poll == null) return;

            // Poll löschen
            await _pollService.DeletePollAsync(pollId.Value);

            // Broadcast
            await Clients.Group(poll.ChannelId.ToString())
                         .SendAsync("PollDeleted", title);
        }

        // ===============================
        // ==========  Utility  ==========
        // ===============================
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

        private string GetUserName()
        {
            var claim = Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName);
            return claim?.Value ?? "Unknown";
        }
    }
}
