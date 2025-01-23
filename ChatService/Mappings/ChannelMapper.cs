using De.Hsfl.LoomChat.Common.Dtos;
using De.Hsfl.LoomChat.Common.Models;

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
                .Select(msg => new ChatMessageDto 
                {
                    Id = msg.Id,
                    ChannelId = msg.ChannelId,
                    SenderUserId = msg.SenderUserId,
                    Content = msg.Content,
                    SentAt = msg.SentAt,
                })
                .ToList(),
        };
    }
}