using System;
using System.Linq;
using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;
using System.Collections.Generic;

public static class ChannelMapper
{
    public static ChannelDto ToDto(Channel channel)
    {
        if (channel == null)
            throw new ArgumentNullException(nameof(channel));

        return new ChannelDto
        {
            Id = channel.Id,
            Name = channel.Name,
            CreatedAt = channel.CreatedAt,
            IsDmChannel = channel.IsDmChannel,
            ChannelMembers = channel.ChannelMembers
                .Select(member => new ChannelMemberDto
                {
                    ChannelId = member.ChannelId,
                    UserId = member.UserId,
                    IsArchived = member.IsArchived,
                    Role = member.Role
                })
                .ToList(),
            ChatMessages = channel.ChatMessages
                .Select(msg => ConvertToSingleChatMessageDto(msg))
                .ToList()
        };
    }

    private static ChatMessageDto ConvertToSingleChatMessageDto(ChatMessage msg)
    {
        if (msg is TextMessage txt)
        {
            return new ChatMessageDto
            {
                Id = txt.Id,
                ChannelId = txt.ChannelId,
                SenderUserId = txt.SenderUserId,
                SentAt = txt.SentAt,
                Type = MessageType.Text,
                Content = txt.Content
            };
        }
        else if (msg is PollMessage pm)
        {
            return new ChatMessageDto
            {
                Id = pm.Id,
                ChannelId = pm.ChannelId,
                SenderUserId = pm.SenderUserId,
                SentAt = pm.SentAt,
                Type = MessageType.Poll,
                PollId = pm.PollId,
                // Falls Sie pm.Poll haben, können Sie Title/Options hier setzen
                PollTitle = pm.Poll?.Title,
                IsClosed = pm.Poll?.IsClosed ?? false,
                PollOptions = pm.Poll?.Options.Select(o => o.OptionText).ToList()
                             ?? new List<string>()
            };
        }
        // Fallback, falls wir noch andere Subtypen hätten
        return new ChatMessageDto
        {
            Id = msg.Id,
            ChannelId = msg.ChannelId,
            SenderUserId = msg.SenderUserId,
            SentAt = msg.SentAt,
            Type = MessageType.Text,
            Content = "???(Unknown sub-class)???"
        };
    }
}
