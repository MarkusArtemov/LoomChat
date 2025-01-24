using System;
using System.Collections.Generic;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Full channel info, including members and messages
    /// </summary>
    public class ChannelDetailsResponse
    {
        public int Id { get; }
        public string Name { get; }
        public DateTime CreatedAt { get; }
        public List<ChannelMemberResponse> Members { get; }
        public List<ChatMessageResponse> Messages { get; }

        public ChannelDetailsResponse(
            int id,
            string name,
            DateTime createdAt,
            List<ChannelMemberResponse> members,
            List<ChatMessageResponse> messages
        )
        {
            Id = id;
            Name = name;
            CreatedAt = createdAt;
            Members = members;
            Messages = messages;
        }
    }
}
