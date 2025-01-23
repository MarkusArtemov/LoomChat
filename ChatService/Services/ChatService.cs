using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;
using De.Hsfl.LoomChat.Chat.Persistence;
using De.Hsfl.LoomChat.Common.Enums;
using AutoMapper;

namespace De.Hsfl.LoomChat.Chat.Services
{
    /// <summary>
    /// Manages channels, messages, and memberships.
    /// </summary>
    public class ChatService
    {
        private readonly ChatDbContext _chatDbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<ChatService> _logger;

        public ChatService(ChatDbContext chatDbContext, IMapper mapper, ILogger<ChatService> logger)
        {
            _chatDbContext = chatDbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<GetChannelsResponse> GetAllChannels(GetChannelsRequest request)
        {
            _logger.LogInformation("GetAllChannels called. UserId={UserId}", request.UserId);

            List<Channel> channels = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .Include(c => c.ChatMessages)
                .Where(c => c.IsDmChannel != true)
                .ToListAsync();

            var channelsDto = channels.Select(ChannelMapper.ToDto).ToList();

            _logger.LogDebug("Returning {Count} channels (non-DM)", channelsDto.Count);
            return new GetChannelsResponse(channelsDto);
        }

        public async Task<GetUsersResponse> GetAllUsers(GetUsersRequest request)
        {
            _logger.LogInformation("GetAllUsers called (will forward to Auth-Service).");

            using (var client = new HttpClient())
            {
                try
                {
                    var url = "http://localhost:5232/Auth/users";
                    var jsonContent = new StringContent(
                        JsonConvert.SerializeObject(request),
                        Encoding.UTF8,
                        "application/json"
                    );
                    var response = await client.PostAsync(url, jsonContent);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var responseObj = JsonConvert.DeserializeObject<GetUsersResponse>(responseBody);

                        _logger.LogDebug("Received {Count} users from Auth service.", responseObj?.Users?.Count);
                        return responseObj;
                    }
                    else
                    {
                        _logger.LogWarning("Call to Auth service /Auth/users was not successful: {StatusCode}", response.StatusCode);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching users from Auth service.");
                    return null;
                }
            }
        }

        public async Task<GetDirectChannelsResponse> GetAllDirectChannels(GetDirectChannelsRequest request)
        {
            _logger.LogInformation("GetAllDirectChannels called. UserId={UserId}", request.UserId);

            List<Channel> channels = await _chatDbContext.Channels
                .Where(c => c.IsDmChannel == true)
                .Include(c => c.ChannelMembers)
                .Include(c => c.ChatMessages)
                .ToListAsync();

            var channelsDto = channels.Select(ChannelMapper.ToDto).ToList();
            _logger.LogDebug("Returning {Count} DM channels", channelsDto.Count);

            return new GetDirectChannelsResponse(channelsDto);
        }

        /// <summary>
        /// Creates a new channel and assigns the creator as owner
        /// </summary>
        public async Task<CreateChannelResponse> CreateChannelAsync(CreateChannelRequest request)
        {
            _logger.LogInformation("CreateChannelAsync called. UserId={UserId}, ChannelName={ChannelName}",
                                   request.UserId, request.ChannelName);

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

            _logger.LogDebug("Created channel with Id={ChannelId} for UserId={UserId}", channel.Id, request.UserId);

            return new CreateChannelResponse(ChannelMapper.ToDto(channel));
        }

        /// <summary>
        /// Sends a new message and returns it as a response
        /// </summary>
        public async Task<ChatMessageResponse> SendMessageAsync(int channelId, int senderUserId, string content)
        {
            _logger.LogInformation("SendMessageAsync called. ChannelId={ChannelId}, SenderUserId={SenderUserId}", channelId, senderUserId);

            var message = new ChatMessage
            {
                ChannelId = channelId,
                SenderUserId = senderUserId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _chatDbContext.ChatMessages.Add(message);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogDebug("Created message with Id={MessageId} in Channel={ChannelId}", message.Id, channelId);

            return _mapper.Map<ChatMessageResponse>(message);
        }

        /// <summary>
        /// Returns all messages of a channel as responses
        /// </summary>
        public async Task<List<ChatMessageResponse>> GetMessagesForChannelAsync(int channelId)
        {
            _logger.LogDebug("GetMessagesForChannelAsync called. ChannelId={ChannelId}", channelId);

            var messages = await _chatDbContext.ChatMessages
                .Where(m => m.ChannelId == channelId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            _logger.LogDebug("Found {Count} messages for ChannelId={ChannelId}", messages.Count, channelId);

            return _mapper.Map<List<ChatMessageResponse>>(messages);
        }

        /// <summary>
        /// Returns full channel info (members, messages)
        /// </summary>
        public async Task<ChannelDetailsResponse?> GetChannelDetailsAsync(int channelId)
        {
            _logger.LogInformation("GetChannelDetailsAsync called. ChannelId={ChannelId}", channelId);

            var channel = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .Include(c => c.ChatMessages)
                .FirstOrDefaultAsync(c => c.Id == channelId);

            if (channel == null)
            {
                _logger.LogWarning("Channel with Id={ChannelId} not found.", channelId);
                return null;
            }

            channel.ChatMessages = channel.ChatMessages.OrderBy(m => m.SentAt).ToList();

            _logger.LogDebug("Returning details for channel {ChannelId}, with {MsgCount} messages",
                             channelId, channel.ChatMessages.Count);

            return _mapper.Map<ChannelDetailsResponse>(channel);
        }

        /// <summary>
        /// Marks the membership as archived for the given user
        /// </summary>
        public async Task<bool> ArchiveChannelForUserAsync(int channelId, int userId)
        {
            _logger.LogInformation("ArchiveChannelForUserAsync called. ChannelId={ChannelId}, UserId={UserId}", channelId, userId);

            var membership = await _chatDbContext.ChannelMembers
                .FirstOrDefaultAsync(cm => cm.ChannelId == channelId && cm.UserId == userId);

            if (membership == null)
            {
                _logger.LogWarning("ChannelMember not found for ChannelId={ChannelId}, UserId={UserId}", channelId, userId);
                return false;
            }

            membership.IsArchived = true;
            await _chatDbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Removes channel membership for the given user
        /// </summary>
        public async Task<bool> RemoveUserFromChannelAsync(int channelId, int userId)
        {
            _logger.LogInformation("RemoveUserFromChannelAsync called. ChannelId={ChannelId}, UserId={UserId}", channelId, userId);

            var membership = await _chatDbContext.ChannelMembers
                .FirstOrDefaultAsync(cm => cm.ChannelId == channelId && cm.UserId == userId);

            if (membership == null)
            {
                _logger.LogWarning("ChannelMember not found for ChannelId={ChannelId}, UserId={UserId}", channelId, userId);
                return false;
            }

            _chatDbContext.ChannelMembers.Remove(membership);
            await _chatDbContext.SaveChangesAsync();
            return true;
        }

        public async Task<OpenChatWithUserResponse> OpenChatWithUser(OpenChatWithUserRequest request)
        {
            _logger.LogInformation("OpenChatWithUser called. OwnId={OwnId}, OtherId={OtherId}",
                                   request.OwnId, request.OtherId);

            var existingChannel = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .FirstOrDefaultAsync(c =>
                    c.IsDmChannel &&
                    c.ChannelMembers.Any(cm => cm.UserId == request.OwnId) &&
                    c.ChannelMembers.Any(cm => cm.UserId == request.OtherId));

            if (existingChannel != null)
            {
                _logger.LogDebug("Existing DM channel found: ChannelId={ChannelId}", existingChannel.Id);
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

            _logger.LogInformation("Created new DM channel with Id={ChannelId} for users {OwnId} and {OtherId}",
                                   newChannel.Id, request.OwnId, request.OtherId);

            return new OpenChatWithUserResponse(ChannelMapper.ToDto(newChannel));
        }

        public async Task<SendMessageResponse> SendMessage(SendMessageRequest request)
        {
            _logger.LogInformation("SendMessage (REST) called. ChannelId={ChannelId}, UserId={UserId}, Message={Message}",
                                   request.ChannelId, request.UserId, request.Message);

            Channel channel = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .Include(c => c.ChatMessages)
                .FirstOrDefaultAsync(c => c.Id == request.ChannelId);

            if (channel == null)
            {
                _logger.LogError("Channel not found for ChannelId={ChannelId}. Cannot send message.", request.ChannelId);
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
            _logger.LogDebug("Message stored (Id={MessageId}) in channel {ChannelId}.", msg.Id, channel.Id);

            return new SendMessageResponse(channelDto);
        }
    }
}
