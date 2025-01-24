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
                _logger.LogInformation(
                    "SendMessageToChannel called. ChannelId={ChannelId}, RequestUserId={RequestUserId}, Message={Message}",
                    request.ChannelId, request.UserId, request.Message
                );

                int userId = GetUserId();
                string userName = GetUserName();

                var msgResponse = await _chatService.SendMessageAsync(
                    request.ChannelId,
                    userId,
                    request.Message
                );

                await Clients.Group(request.ChannelId.ToString()).SendAsync(
                    "ReceiveChannelMessage",
                    msgResponse.ChannelId,
                    msgResponse.SenderUserId,
                    userName,
                    msgResponse.Content,
                    msgResponse.SentAt
                );

                _logger.LogDebug(
                    "Message sent successfully. Channel={ChannelId}, SenderUserId={SenderUserId}, SentAt={SentAt}",
                    msgResponse.ChannelId, msgResponse.SenderUserId, msgResponse.SentAt
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
                "JoinChannel called. ConnectionId={ConnectionId}, ChannelId={ChannelId}",
                Context.ConnectionId, request.ChannelId
            );

            await Groups.AddToGroupAsync(Context.ConnectionId, request.ChannelId.ToString());
            var messages = await _chatService.GetMessagesForChannelAsync(request.ChannelId);

            _logger.LogDebug("User joined channel {ChannelId}. Found {Count} messages.",
                             request.ChannelId, messages.Count);

            await Clients.Caller.SendAsync("ChannelHistory", request.ChannelId, messages);
        }

        public async Task LeaveChannel(LeaveChannelRequest request)
        {
            _logger.LogInformation(
                "LeaveChannel called. ConnectionId={ConnectionId}, ChannelId={ChannelId}, RemoveMembership={RemoveMembership}",
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
            _logger.LogInformation("ArchiveChannel called. ChannelId={ChannelId}", request.ChannelId);

            int userId = GetUserId();
            var success = await _chatService.ArchiveChannelForUserAsync(request.ChannelId, userId);
            await Clients.Caller.SendAsync("ChannelArchived", request.ChannelId, success);

            _logger.LogDebug(
                "ArchiveChannel result for ChannelId={ChannelId}, UserId={UserId}: {Success}",
                request.ChannelId, userId, success
            );
        }

        private int GetUserId()
        {
            // Logge den rohen Token, der per QueryString ankommt:
            var rawToken = Context.GetHttpContext()?.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(rawToken))
            {
                _logger.LogInformation("Raw Token in ChatHub: {RawToken}", rawToken);
            }

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
