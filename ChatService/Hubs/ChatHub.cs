using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using De.Hsfl.LoomChat.Chat.Services;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models; // evtl. für Poll
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

        // ===========================
        // ==========  Chat  =========
        // ===========================
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

                // Broadcast an alle im Channel => "ReceiveChannelMessage"
                // Falls du mehr Parameter (z. B. Type) brauchst, häng sie hier an
                await Clients.Group(request.ChannelId.ToString()).SendAsync(
                    "ReceiveChannelMessage",
                    msgResponse.ChannelId,     // channelId
                    msgResponse.SenderUserId,  // senderUserId
                    userName,                  // senderName
                    msgResponse.Content,       // text content
                    msgResponse.SentAt         // timestamp
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

            // Gruppe beitreten
            await Groups.AddToGroupAsync(Context.ConnectionId, request.ChannelId.ToString());

            int userId = GetUserId();
            var messages = await _chatService.GetMessagesForChannelAsync(request.ChannelId, userId);

            _logger.LogDebug(
                "User joined channel {ChannelId}. Found {Count} messages. (UserId={UserId})",
                request.ChannelId, messages.Count, userId
            );

            // Schicke dem Caller alle Nachrichten (Text + Poll)
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

        // ===========================
        // ==========  Poll  =========
        // ===========================
        public async Task CreatePoll(int channelId, string title, List<string> options)
        {
            int userId = GetUserId();
            string userName = GetUserName();
            _logger.LogInformation("CreatePoll: ChannelId={ChannelId}, Title={Title}, UserId={UserId}",
                                   channelId, title, userId);

            // 1) Poll in DB anlegen
            var poll = await _pollService.CreatePollAsync(channelId, userId, title, options);

            // 2) PollMessage => ChatMessages (History)
            await _chatService.CreatePollMessageAsync(channelId, userId, poll.Id);

            // 3) Broadcast an alle => "ReceiveChannelMessage"
            // Hier haben wir erweiterte Parameter: 
            //   type="Poll", pollTitle, pollOptions, isClosed, hasUserVoted
            DateTime now = DateTime.UtcNow;
            await Clients.Group(channelId.ToString()).SendAsync(
                "ReceiveChannelMessage",
                channelId,
                userId,
                userName,
                "",       // content
                now,      // sentAt
                "Poll",   // type
                title,    // pollTitle
                options,
                poll.IsClosed,
                false     // hasUserVoted
            );

            _logger.LogInformation("CreatePoll -> broadcast done. ChannelId={ChannelId}, Title='{Title}'", channelId, title);
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
                // DB: Abstimmen
                await _pollService.VoteAsync(pollId.Value, userId, option);

                // Neue Ergebnisse
                var results = await _pollService.GetResultsAsync(pollId.Value);

                // Finde poll => channelId
                var pollEntity = await _chatService.GetPollByIdAsync(pollId.Value);
                if (pollEntity != null)
                {
                    // => "PollUpdated"
                    await Clients.Group(pollEntity.ChannelId.ToString())
                                 .SendAsync("PollUpdated", title, results);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vote error. Title='{Title}' User={UserId}", title, userId);
                await Clients.Caller.SendAsync("PollError", ex.Message);
            }
        }

        public async Task ClosePoll(string title)
        {
            int userId = GetUserId();
            _logger.LogInformation("ClosePoll: Title={Title}, triggered by User={UserId}", title, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null) return;

            await _pollService.ClosePollAsync(pollId.Value);

            var pollEntity = await _chatService.GetPollByIdAsync(pollId.Value);
            if (pollEntity != null)
            {
                // => "PollClosed"
                await Clients.Group(pollEntity.ChannelId.ToString())
                             .SendAsync("PollClosed", title);
            }
        }

        public async Task DeletePoll(string title)
        {
            int userId = GetUserId();
            _logger.LogInformation("DeletePoll: Title={Title}, triggered by User={UserId}", title, userId);

            var pollId = await _pollService.FindPollIdByTitleAsync(title);
            if (pollId == null) return;

            var pollEntity = await _chatService.GetPollByIdAsync(pollId.Value);
            if (pollEntity == null) return;

            // DB-löschen
            await _pollService.DeletePollAsync(pollId.Value);

            // => "PollDeleted"
            await Clients.Group(pollEntity.ChannelId.ToString())
                         .SendAsync("PollDeleted", title);
        }

        // =========================
        // ========== UTILS ========
        // =========================
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
            return (claim == null) ? 0 : int.Parse(claim.Value);
        }

        private string GetUserName()
        {
            var claim = Context.User?.FindFirst(JwtRegisteredClaimNames.UniqueName);
            return claim?.Value ?? "Unknown";
        }
    }
}
