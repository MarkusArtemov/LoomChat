using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using De.Hsfl.LoomChat.Chat.Persistence;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;

namespace De.Hsfl.LoomChat.Chat.Services
{
    /// <summary>
    /// Manages channels, messages, and memberships.
    /// No AutoMapper; we map manually to ChatMessageDto.
    /// </summary>
    public class ChatService
    {
        private readonly ChatDbContext _chatDbContext;
        private readonly ILogger<ChatService> _logger;

        public ChatService(ChatDbContext chatDbContext, ILogger<ChatService> logger)
        {
            _chatDbContext = chatDbContext;
            _logger = logger;
        }

        private HttpClient CreateHttpClientWithAuth()
        {
            // Platzhalter-Token
            var token = "DEIN_AKTUELLES_JWT";
            var client = new HttpClient();
            if (!string.IsNullOrWhiteSpace(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        /// <summary>
        /// Lädt alle öffentlichen (nicht DM) Channels + Messages und gibt ein DTO zurück.
        /// </summary>
        public async Task<GetChannelsResponse> GetAllChannels(GetChannelsRequest request)
        {
            _logger.LogInformation("GetAllChannels called. UserId={UserId}", request.UserId);

            var channels = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .Include(c => c.ChatMessages)
                .Where(c => !c.IsDmChannel)
                .ToListAsync();

            // Channel -> ChannelDto
            var list = channels.Select(ChannelMapper.ToDto).ToList();
            return new GetChannelsResponse(list);
        }

        /// <summary>
        /// Lädt alle User (evtl. via externem Auth-Service).
        /// Hier nur Dummy, der eine leere Liste zurückgibt.
        /// </summary>
        public async Task<GetUsersResponse> GetAllUsers(GetUsersRequest request)
        {
            _logger.LogInformation("GetAllUsers called (dummy).");
            return await Task.FromResult(new GetUsersResponse(new List<User>()));
        }

        /// <summary>
        /// Lädt alle direkten Channels (DM).
        /// </summary>
        public async Task<GetDirectChannelsResponse> GetAllDirectChannels(GetDirectChannelsRequest request)
        {
            _logger.LogInformation("GetAllDirectChannels called. UserId={UserId}", request.UserId);

            var channels = await _chatDbContext.Channels
                .Where(c => c.IsDmChannel)
                .Include(c => c.ChannelMembers)
                .Include(c => c.ChatMessages)
                .ToListAsync();

            var dtos = channels.Select(ChannelMapper.ToDto).ToList();
            return new GetDirectChannelsResponse(dtos);
        }

        /// <summary>
        /// Erstellt einen neuen Channel und macht den Ersteller zum Owner.
        /// </summary>
        public async Task<CreateChannelResponse> CreateChannelAsync(CreateChannelRequest request)
        {
            _logger.LogInformation("CreateChannelAsync. UserId={UserId}, ChannelName={ChannelName}",
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
                Role = Common.Enums.ChannelRole.Owner
            };
            _chatDbContext.ChannelMembers.Add(member);
            await _chatDbContext.SaveChangesAsync();

            return new CreateChannelResponse(ChannelMapper.ToDto(channel));
        }

        /// <summary>
        /// Sendet eine reine Text-Nachricht in den Channel.
        /// </summary>
        public async Task<ChatMessageDto> SendMessageAsync(int channelId, int senderUserId, string content)
        {
            _logger.LogInformation("SendMessageAsync. Channel={ChannelId}, User={UserId}", channelId, senderUserId);

            var message = new TextMessage
            {
                ChannelId = channelId,
                SenderUserId = senderUserId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _chatDbContext.ChatMessages.Add(message);
            await _chatDbContext.SaveChangesAsync();

            _logger.LogDebug("Created TextMessage Id={MessageId} in Channel={ChannelId}", message.Id, channelId);

            // Manuelles Mapping -> ChatMessageDto
            return new ChatMessageDto
            {
                Id = message.Id,
                ChannelId = message.ChannelId,
                SenderUserId = message.SenderUserId,
                SentAt = message.SentAt,
                Type = MessageType.Text,
                Content = message.Content
            };
        }

        /// <summary>
        /// Wichtig: Hier beim Laden poll-spezifische Daten mit einbinden!
        /// </summary>
        public async Task<List<ChatMessageDto>> GetMessagesForChannelAsync(int channelId)
        {
            _logger.LogDebug("GetMessagesForChannelAsync. ChannelId={ChannelId}", channelId);

            // HIER IST DIE ENTSCHEIDENDE ZEILE:
            var messages = await _chatDbContext.ChatMessages
                .Include(m => (m as PollMessage).Poll)           // Poll-Objekt mitladen
                .ThenInclude(p => p.Options)                     // Und auch die PollOptions
                .Where(m => m.ChannelId == channelId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var result = new List<ChatMessageDto>();
            foreach (var msg in messages)
            {
                if (msg is TextMessage txt)
                {
                    result.Add(new ChatMessageDto
                    {
                        Id = txt.Id,
                        ChannelId = txt.ChannelId,
                        SenderUserId = txt.SenderUserId,
                        SentAt = txt.SentAt,
                        Type = MessageType.Text,
                        Content = txt.Content
                    });
                }
                else if (msg is PollMessage pm)
                {
                    result.Add(new ChatMessageDto
                    {
                        Id = pm.Id,
                        ChannelId = pm.ChannelId,
                        SenderUserId = pm.SenderUserId,
                        SentAt = pm.SentAt,
                        Type = MessageType.Poll,
                        PollId = pm.PollId,
                        PollTitle = pm.Poll?.Title,
                        IsClosed = pm.Poll?.IsClosed ?? false,
                        PollOptions = pm.Poll?.Options.Select(o => o.OptionText).ToList()
                                      ?? new List<string>()
                    });
                }
                else
                {
                    // Fallback
                    result.Add(new ChatMessageDto
                    {
                        Id = msg.Id,
                        ChannelId = msg.ChannelId,
                        SenderUserId = msg.SenderUserId,
                        SentAt = msg.SentAt,
                        Type = MessageType.Text,
                        Content = "[Unknown message type]"
                    });
                }
            }
            return result;
        }

        // Archivieren etc. => minimal
        public async Task<bool> ArchiveChannelForUserAsync(int channelId, int userId)
        {
            var membership = await _chatDbContext.ChannelMembers
                .FirstOrDefaultAsync(cm => cm.ChannelId == channelId && cm.UserId == userId);
            if (membership == null) return false;

            membership.IsArchived = true;
            await _chatDbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveUserFromChannelAsync(int channelId, int userId)
        {
            var membership = await _chatDbContext.ChannelMembers
                .FirstOrDefaultAsync(cm => cm.ChannelId == channelId && cm.UserId == userId);
            if (membership == null) return false;

            _chatDbContext.ChannelMembers.Remove(membership);
            await _chatDbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Öffnet / erstellt einen DM-Channel zwischen OwnId und OtherId.
        /// </summary>
        public async Task<OpenChatWithUserResponse> OpenChatWithUser(OpenChatWithUserRequest request)
        {
            var existing = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .FirstOrDefaultAsync(c =>
                    c.IsDmChannel &&
                    c.ChannelMembers.Any(cm => cm.UserId == request.OwnId) &&
                    c.ChannelMembers.Any(cm => cm.UserId == request.OtherId));

            if (existing != null)
            {
                return new OpenChatWithUserResponse(ChannelMapper.ToDto(existing));
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

        /// <summary>
        /// REST: sendet Text-Nachricht, danach ChannelDto zurück.
        /// </summary>
        public async Task<SendMessageResponse> SendMessage(SendMessageRequest request)
        {
            var channel = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .Include(c => c.ChatMessages)
                .FirstOrDefaultAsync(c => c.Id == request.ChannelId);

            if (channel == null)
                throw new Exception("Channel not found.");

            var msg = new TextMessage
            {
                ChannelId = channel.Id,
                SenderUserId = request.UserId,
                Content = request.Message,
                SentAt = DateTime.UtcNow
            };
            channel.ChatMessages.Add(msg);
            await _chatDbContext.SaveChangesAsync();

            var dto = ChannelMapper.ToDto(channel);
            return new SendMessageResponse(dto);
        }

        /// <summary>
        /// Legt eine neue PollMessage in der DB an.
        /// </summary>
        public async Task CreatePollMessageAsync(int channelId, int senderUserId, int pollId)
        {
            var pollMsg = new PollMessage
            {
                ChannelId = channelId,
                SenderUserId = senderUserId,
                PollId = pollId,
                SentAt = DateTime.UtcNow
            };
            _chatDbContext.ChatMessages.Add(pollMsg);
            await _chatDbContext.SaveChangesAsync();
        }
    }
}
