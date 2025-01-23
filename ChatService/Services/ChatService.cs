using Microsoft.EntityFrameworkCore;
using De.Hsfl.LoomChat.Chat.Persistence;
using De.Hsfl.LoomChat.Common.Models;
using De.Hsfl.LoomChat.Common.Enums;
using AutoMapper;
using De.Hsfl.LoomChat.Chat.Dtos.Responses;
using De.Hsfl.LoomChat.Common.Dtos;
using Newtonsoft.Json;
using System.Text;

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

        public async Task<GetChannelsResponse> GetAllChannels(GetChannelsRequest request)
        {
            List<Channel> channels = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .Include(c => c.ChatMessages)
                .Where(c => c.IsDmChannel != true)
                .ToListAsync();

            var channelsDto = channels.Select(ChannelMapper.ToDto).ToList();
            return new GetChannelsResponse(channelsDto);
        }

        public async Task<GetUsersResponse> GetAllUsers(GetUsersRequest request)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5232/Auth/users";
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<GetUsersResponse>(responseBody);
                        return responseObj;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }

        public async Task<GetDirectChannelsResponse> GetAllDirectChannels(GetDirectChannelsRequest request)
        {
            List<Channel> channels = await _chatDbContext.Channels
                .Where(c => c.IsDmChannel == true) // Prüft, ob genau 2 Mitglieder im Channel sind
                .Include(c => c.ChannelMembers)         // Lädt die zugehörigen Mitglieder
                .Include(c => c.ChatMessages)           // Optional: Lädt die zugehörigen Nachrichten
                .ToListAsync();
            var channelsDto = channels.Select(ChannelMapper.ToDto).ToList();
            return new GetDirectChannelsResponse(channelsDto);
        }

        /// <summary>
        /// Creates a new channel and assigns the creator as owner
        /// </summary>
        public async Task<CreateChannelResponse> CreateChannelAsync(CreateChannelRequest request)
        {
            var channel = new Channel
            {
                Name = request.ChannelName,
                CreatedAt = DateTime.UtcNow
            };

            _chatDbContext.Channels.Add(channel);
            await _chatDbContext.SaveChangesAsync();

            var member = new ChannelMember
            {
                ChannelId = channel.Id,
                UserId = request.UserId,
                Role = ChannelRole.Owner
            };

            _chatDbContext.ChannelMembers.Add(member);
            await _chatDbContext.SaveChangesAsync();
            return new CreateChannelResponse(ChannelMapper.ToDto(channel));
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

        public async Task<OpenChatWithUserResponse> OpenChatWithUser(OpenChatWithUserRequest request)
        {
            var existingChannel = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .FirstOrDefaultAsync(c =>
                    c.IsDmChannel &&
                    c.ChannelMembers.Any(cm => cm.UserId == request.OwnId) &&
                    c.ChannelMembers.Any(cm => cm.UserId == request.OtherId));

            if (existingChannel != null)
            {
                return new OpenChatWithUserResponse(ChannelMapper.ToDto(existingChannel));
            }

            var newChannel = new Channel
            {
                Name = "Direktnachricht",
                IsDmChannel = true,
                CreatedAt = DateTime.UtcNow,
                ChannelMembers = new List<ChannelMember>
                {
                    new ChannelMember { UserId = request.OwnId },
                    new ChannelMember { UserId = request.OtherId }
                }
            };

            _chatDbContext.Channels.Add(newChannel);
            await _chatDbContext.SaveChangesAsync();

            return new OpenChatWithUserResponse(ChannelMapper.ToDto(newChannel));
        }

        public async Task<SendMessageResponse> SendMessage(SendMessageRequest request)
        {
            Channel channel = await _chatDbContext.Channels
            .Include (c => c.ChannelMembers)
            .Include(c => c.ChatMessages)
            .FirstOrDefaultAsync(c => c.Id == request.ChannelId);

            if (channel == null)
            {
                throw new Exception("Channel not found.");
            }

            ChatMessage msg = new ChatMessage
            {
                ChannelId = channel.Id,
                SenderUserId = request.UserId,
                Content = request.Message,
                SentAt = DateTime.UtcNow,
                Channel = channel
            };
            channel.ChatMessages.Add(msg);
            await _chatDbContext.SaveChangesAsync();
            var channelDto = ChannelMapper.ToDto(channel);
            return new SendMessageResponse(channelDto);
        }
    }
}
