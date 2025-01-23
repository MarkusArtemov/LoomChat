using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using De.Hsfl.LoomChat.Chat.Services;
using De.Hsfl.LoomChat.Common.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace De.Hsfl.LoomChat.Chat.Hubs
{
    /// <summary>
    /// Provides real-time chat actions via SignalR
    /// </summary>
    /// 
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Sends a message to a channel in real-time
        /// </summary>
        public async Task SendMessageToChannel(SendMessageRequest request)
        {
            try
            {
                _logger.LogInformation("SendMessageToChannel called. ChannelId={ChannelId}, RequestUserId={RequestUserId}, Message={Message}",
                                       request.ChannelId, request.UserId, request.Message);

                // Hole ID und Username aus dem JWT
                int userId = GetUserId();
                string userName = GetUserName();

                // Nachricht in DB speichern
                var msgResponse = await _chatService.SendMessageAsync(
                    request.ChannelId,
                    userId, // aus JWT
                    request.Message
                );

                // Alle Clients in dem Channel benachrichtigen
                await Clients.Group(request.ChannelId.ToString()).SendAsync(
                    "ReceiveChannelMessage",
                    msgResponse.ChannelId,
                    msgResponse.SenderUserId,
                    userName,           // aus JWT
                    msgResponse.Content,
                    msgResponse.SentAt
                );

                _logger.LogDebug("Message sent successfully. Channel={ChannelId}, SenderUserId={SenderUserId}, SentAt={SentAt}",
                                 msgResponse.ChannelId, msgResponse.SenderUserId, msgResponse.SentAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessageToChannel. ChannelId={ChannelId}", request.ChannelId);
                throw;
            }
        }

        /// <summary>
        /// Joins a channel group => triggers "ChannelHistory" from server
        /// </summary>
        public async Task JoinChannel(JoinChannelRequest request)
        {
            _logger.LogInformation("JoinChannel called. ConnectionId={ConnectionId}, ChannelId={ChannelId}",
                                   Context.ConnectionId, request.ChannelId);

            await Groups.AddToGroupAsync(Context.ConnectionId, request.ChannelId.ToString());

            var messages = await _chatService.GetMessagesForChannelAsync(request.ChannelId);

            _logger.LogDebug("User joined channel {ChannelId}. Found {Count} messages.", request.ChannelId, messages.Count);

            await Clients.Caller.SendAsync("ChannelHistory", request.ChannelId, messages);
        }

        /// <summary>
        /// Leaves the channel
        /// </summary>
        public async Task LeaveChannel(LeaveChannelRequest request)
        {
            _logger.LogInformation("LeaveChannel called. ConnectionId={ConnectionId}, ChannelId={ChannelId}, RemoveMembership={RemoveMembership}",
                                   Context.ConnectionId, request.ChannelId, request.RemoveMembership);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, request.ChannelId.ToString());

            if (request.RemoveMembership)
            {
                int userId = GetUserId();
                await _chatService.RemoveUserFromChannelAsync(request.ChannelId, userId);
            }

            await Clients.Caller.SendAsync("LeftChannel", request.ChannelId, request.RemoveMembership);
        }

        /// <summary>
        /// Archives channel membership
        /// </summary>
        public async Task ArchiveChannel(ArchiveChannelRequest request)
        {
            _logger.LogInformation("ArchiveChannel called. ChannelId={ChannelId}", request.ChannelId);

            int userId = GetUserId();
            var success = await _chatService.ArchiveChannelForUserAsync(request.ChannelId, userId);
            await Clients.Caller.SendAsync("ChannelArchived", request.ChannelId, success);

            _logger.LogDebug("ArchiveChannel result for ChannelId={ChannelId}, UserId={UserId}: {Success}",
                             request.ChannelId, userId, success);
        }

        /// <summary>
        /// Returns userId from JWT
        /// </summary>
        private int GetUserId()
        {
            var claim = Context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            if (claim == null) return 0;
            return int.Parse(claim.Value);
        }

        /// <summary>
        /// Returns username from JWT
        /// </summary>
        private string GetUserName()
        {
            var claim = Context.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.UniqueName);
            return claim?.Value ?? "Unknown";
        }
    }
}
