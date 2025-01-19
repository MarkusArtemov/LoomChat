using Microsoft.EntityFrameworkCore;
using De.Hsfl.LoomChat.Chat.Persistence;
using De.Hsfl.LoomChat.Chat.Models;
using De.Hsfl.LoomChat.Chat.Enums;
using AutoMapper;
using De.Hsfl.LoomChat.Chat.Dtos.Responses;

namespace De.Hsfl.LoomChat.Chat.Services
{
    /// <summary>
    /// Manages channels, messages, and memberships.
    /// </summary>
    public class ChatService
    {
        private readonly ChatDbContext _chatDbContext;
        private readonly IMapper _mapper;

        public ChatService(ChatDbContext chatDbContext, IMapper mapper)
        {
            _chatDbContext = chatDbContext;
            _mapper = mapper;
        }

        /// <summary>
        /// Creates a new channel and assigns the creator as owner
        /// </summary>
        public async Task<ChannelResponse> CreateChannelAsync(string name, int creatorUserId)
        {
            var channel = new Channel
            {
                Name = name,
                CreatedAt = DateTime.UtcNow
            };

            _chatDbContext.Channels.Add(channel);
            await _chatDbContext.SaveChangesAsync();

            var member = new ChannelMember
            {
                ChannelId = channel.Id,
                UserId = creatorUserId,
                Role = ChannelRole.Owner
            };

            _chatDbContext.ChannelMembers.Add(member);
            await _chatDbContext.SaveChangesAsync();

            return _mapper.Map<ChannelResponse>(channel);
        }

        /// <summary>
        /// Sends a new message and returns it as a response
        /// </summary>
        public async Task<ChatMessageResponse> SendMessageAsync(int channelId, int senderUserId, string content)
        {
            var message = new ChatMessage
            {
                ChannelId = channelId,
                SenderUserId = senderUserId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _chatDbContext.ChatMessages.Add(message);
            await _chatDbContext.SaveChangesAsync();

            return _mapper.Map<ChatMessageResponse>(message);
        }

        /// <summary>
        /// Returns all messages of a channel as responses
        /// </summary>
        public async Task<List<ChatMessageResponse>> GetMessagesForChannelAsync(int channelId)
        {
            var messages = await _chatDbContext.ChatMessages
                .Where(m => m.ChannelId == channelId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            return _mapper.Map<List<ChatMessageResponse>>(messages);
        }

        /// <summary>
        /// Returns full channel info (members, messages)
        /// </summary>
        public async Task<ChannelDetailsResponse?> GetChannelDetailsAsync(int channelId)
        {
            var channel = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .Include(c => c.ChatMessages)
                .FirstOrDefaultAsync(c => c.Id == channelId);

            if (channel == null) return null;

            channel.ChatMessages = channel.ChatMessages.OrderBy(m => m.SentAt).ToList();

            return _mapper.Map<ChannelDetailsResponse>(channel);
        }

        /// <summary>
        /// Marks the membership as archived for the given user
        /// </summary>
        public async Task<bool> ArchiveChannelForUserAsync(int channelId, int userId)
        {
            var membership = await _chatDbContext.ChannelMembers
                .FirstOrDefaultAsync(cm => cm.ChannelId == channelId && cm.UserId == userId);

            if (membership == null) return false;

            membership.IsArchived = true;
            await _chatDbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Removes channel membership for the given user
        /// </summary>
        public async Task<bool> RemoveUserFromChannelAsync(int channelId, int userId)
        {
            var membership = await _chatDbContext.ChannelMembers
                .FirstOrDefaultAsync(cm => cm.ChannelId == channelId && cm.UserId == userId);

            if (membership == null) return false;

            _chatDbContext.ChannelMembers.Remove(membership);
            await _chatDbContext.SaveChangesAsync();
            return true;
        }
    }
}
