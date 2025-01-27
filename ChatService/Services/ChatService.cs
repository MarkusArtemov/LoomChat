using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using De.Hsfl.LoomChat.Chat.Persistence;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace De.Hsfl.LoomChat.Chat.Services
{
    /// <summary>
    /// Verwaltet Channels, Nachrichten (inkl. PollMessages) und Mitgliedschaften.
    /// </summary>
    public class ChatService
    {
        private readonly ChatDbContext _chatDbContext;
        private readonly PollService _pollService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(ChatDbContext chatDbContext,
                           PollService pollService,
                           ILogger<ChatService> logger)
        {
            _chatDbContext = chatDbContext;
            _pollService = pollService;
            _logger = logger;
        }

        /// <summary>
        /// Lädt alle regulären (nicht-DM) Channels.
        /// </summary>
        public async Task<GetChannelsResponse> GetAllChannels(GetChannelsRequest request)
        {
            _logger.LogInformation("GetAllChannels called. UserId={UserId}", request.UserId);

            var channels = await _chatDbContext.Channels
                .Include(c => c.ChannelMembers)
                .Include(c => c.ChatMessages)
                .Where(c => !c.IsDmChannel)
                .ToListAsync();

            var list = channels.Select(ChannelMapper.ToDto).ToList();
            return new GetChannelsResponse(list);
        }

        /// <summary>
        /// Lädt (Dummy) alle User - (in deinem Code ggf. anders implementiert).
        /// </summary>
        public async Task<GetUsersResponse> GetAllUsers(GetUsersRequest request)
        {
            _logger.LogInformation("GetAllUsers called (dummy).");
            // Dummy: leere Liste oder deine echte User-Abfrage
            return await Task.FromResult(new GetUsersResponse(new List<User>()));
        }

        /// <summary>
        /// Lädt alle DM-Channels des Users.
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
        /// Erzeugt einen neuen Channel (kein DM).
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
        /// Sendet eine ganz normale Text-Message in einen Channel.
        /// </summary>
        public async Task<ChatMessageDto> SendMessageAsync(int channelId, int senderUserId, string content)
        {
            _logger.LogInformation("SendMessageAsync. Channel={ChannelId}, User={UserId}, content={Content}",
                                   channelId, senderUserId, content);

            var message = new TextMessage
            {
                ChannelId = channelId,
                SenderUserId = senderUserId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _chatDbContext.ChatMessages.Add(message);
            await _chatDbContext.SaveChangesAsync();

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
        /// Lädt alle Nachrichten (TextMessages + PollMessages) eines Channels
        /// und versieht Polls pro User mit HasUserVoted.
        /// </summary>
        public async Task<List<ChatMessageDto>> GetMessagesForChannelAsync(int channelId, int currentUserId)
        {
            _logger.LogDebug("GetMessagesForChannelAsync. ChannelId={ChannelId}", channelId);

            var messages = await _chatDbContext.ChatMessages
                .Include(m => (m as PollMessage).Poll)
                    .ThenInclude(p => p.Options)
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
                    // Prüfe, ob der User schon für diese PollId gevotet hat
                    bool userVoted = await _pollService.HasUserVotedAsync(pm.PollId, currentUserId);

                    result.Add(new ChatMessageDto
                    {
                        Id = pm.Id,
                        ChannelId = pm.ChannelId,
                        SenderUserId = pm.SenderUserId,
                        SentAt = pm.SentAt,
                        Type = MessageType.Poll,
                        PollId = pm.PollId,
                        PollTitle = pm.Poll?.Title ?? "",
                        IsClosed = pm.Poll?.IsClosed ?? false,
                        PollOptions = pm.Poll?.Options.Select(o => o.OptionText).ToList()
                                      ?? new List<string>(),
                        HasUserVoted = userVoted
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

        /// <summary>
        /// Archiviert einen Channel für einen bestimmten User.
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
        /// Entfernt den User aus dem Channel (ChannelMember).
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

        /// <summary>
        /// Öffnet oder erstellt (falls nicht existiert) einen Direktnachrichten-Channel
        /// zwischen OwnId und OtherId.
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
        /// Falls du (zusätzlich) eine alte REST-Variante hast.
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
        /// Erstellt eine PollMessage (ChatMessage vom Typ Poll).
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

        /// <summary>
        /// Gibt das Poll-Objekt zur pollId zurück (inkl. Channel-Info), 
        /// um in ChatHub das ChannelId zu kennen.
        /// </summary>
        public async Task<Poll> GetPollByIdAsync(int pollId)
        {
            return await _chatDbContext.Polls
                .FirstOrDefaultAsync(p => p.Id == pollId);
        }
    }
}
