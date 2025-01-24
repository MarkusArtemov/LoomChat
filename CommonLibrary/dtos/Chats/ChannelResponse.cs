using System;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Basic channel info for the client
    /// </summary>
    public class ChannelResponse
    {
        public int Id { get; }
        public string Name { get; }
        public DateTime CreatedAt { get; }

        public ChannelResponse(int id, string name, DateTime createdAt)
        {
            Id = id;
            Name = name;
            CreatedAt = createdAt;
        }
    }
}
