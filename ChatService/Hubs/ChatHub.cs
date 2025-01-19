using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using De.Hsfl.LoomChat.Chat.Services;
using De.Hsfl.LoomChat.Chat.Dtos.Requests;

namespace De.Hsfl.LoomChat.Chat.Hubs
{
    /// <summary>
    /// Manages real-time chat actions via SignalR
    /// </summary>
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Creates a new channel and informs the caller
        /// </summary>
        public async Task CreateChannel(CreateChannelRequest request)
        {
            int userId = GetUserId();
            var channelResponse = await _chatService.CreateChannelAsync(request.Name, userId);

            await Clients.Caller.SendAsync("ChannelCreated", channelResponse.Id, channelResponse.Name);
        }

        /// <summary>
        /// Sends a message to everyone in the channel
        /// </summary>
        public async Task SendMessageToChannel(SendMessageRequest request)
        {
            int userId = GetUserId();
            var msgResponse = await _chatService.SendMessageAsync(
                request.ChannelId,
                userId,
                request.Content
            );

            await Clients.Group(request.ChannelId.ToString())
                .SendAsync(
                    "ReceiveChannelMessage",
                    msgResponse.ChannelId,
                    msgResponse.SenderUserId,
                    msgResponse.Content,
                    msgResponse.SentAt
                );
        }

        /// <summary>
        /// Joins a SignalR group and returns the channel history
        /// </summary>
        public async Task JoinChannel(JoinChannelRequest request)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, request.ChannelId.ToString());

            var messages = await _chatService.GetMessagesForChannelAsync(request.ChannelId);
            await Clients.Caller.SendAsync("ChannelHistory", request.ChannelId, messages);
        }

        /// <summary>
        /// Leaves the SignalR group and removes membership if requested
        /// </summary>
        public async Task LeaveChannel(LeaveChannelRequest request)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, request.ChannelId.ToString());

            if (request.RemoveMembership)
            {
                int userId = GetUserId();
                await _chatService.RemoveUserFromChannelAsync(request.ChannelId, userId);
            }

            await Clients.Caller.SendAsync("LeftChannel", request.ChannelId, request.RemoveMembership);
        }

        /// <summary>
        /// Archives channel membership for the current user
        /// </summary>
        public async Task ArchiveChannel(ArchiveChannelRequest request)
        {
            int userId = GetUserId();
            var success = await _chatService.ArchiveChannelForUserAsync(request.ChannelId, userId);
            await Clients.Caller.SendAsync("ChannelArchived", request.ChannelId, success);
        }

        private int GetUserId()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}
