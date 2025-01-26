using System;
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
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

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
