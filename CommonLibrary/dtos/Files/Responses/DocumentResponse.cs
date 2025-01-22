using System;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Represents the main document data
    /// </summary>
    public class DocumentResponse
    {
        public DocumentResponse(int id, string name, int ownerUserId, DateTime createdAt, string description, int channelId)
        {
            Id = id;
            Name = name;
            OwnerUserId = ownerUserId;
            CreatedAt = createdAt;
            Description = description;
            ChannelId = channelId;
        }

        public int Id { get; }
        public string Name { get; }
        public int OwnerUserId { get; }
        public DateTime CreatedAt { get; }
        public string Description { get; }
        public int ChannelId { get; }
    }
}
