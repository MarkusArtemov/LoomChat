using System;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class DocumentResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OwnerUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FileType { get; set; } = string.Empty;
        public int ChannelId { get; set; }

        // z.B. "User_123" oder realer Username
        public string OwnerName { get; set; } = string.Empty;

        public string FileExtension { get; set; } = string.Empty;

        public DocumentResponse() { }

        public DocumentResponse(
            int id,
            string name,
            int ownerUserId,
            DateTime createdAt,
            string fileType,
            int channelId,
            string ownerName,
            string fileExtension)
        {
            Id = id;
            Name = name;
            OwnerUserId = ownerUserId;
            CreatedAt = createdAt;
            FileType = fileType;
            ChannelId = channelId;
            OwnerName = ownerName;
            FileExtension = fileExtension;
        }

        // <-- NEU: Voller Name incl. Extension -->
        public string FullName => Name + FileExtension;
    }
}
