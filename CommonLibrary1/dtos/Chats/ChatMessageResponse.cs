using System;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Represents chat message data for the client
    /// </summary>
    public class ChatMessageResponse
    {
        public int Id { get; }
        public int ChannelId { get; }
        public int SenderUserId { get; }
        public string Content { get; }
        public DateTime SentAt { get; }

        public ChatMessageResponse(int id, int channelId, int senderUserId, string content, DateTime sentAt)
        {
            Id = id;
            ChannelId = channelId;
            SenderUserId = senderUserId;
            Content = content;
            SentAt = sentAt;
        }
    }
}
