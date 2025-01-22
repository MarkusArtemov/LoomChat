using System;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Represents a single document version
    /// </summary>
    public class DocumentVersionResponse
    {
        public DocumentVersionResponse(int id, int documentId, int versionNumber, string storagePath, DateTime createdAt)
        {
            Id = id;
            DocumentId = documentId;
            VersionNumber = versionNumber;
            StoragePath = storagePath;
            CreatedAt = createdAt;
        }

        public int Id { get; }
        public int DocumentId { get; }
        public int VersionNumber { get; }
        public string StoragePath { get; }
        public DateTime CreatedAt { get; }
    }
}
