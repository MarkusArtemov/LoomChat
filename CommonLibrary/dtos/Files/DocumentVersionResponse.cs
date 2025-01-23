using System;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Represents a single document version
    /// </summary>
    public class DocumentVersionResponse
    {
        public DocumentVersionResponse(int id, int documentId, int versionNumber, DateTime createdAt)
        {
            Id = id;
            DocumentId = documentId;
            VersionNumber = versionNumber;
            CreatedAt = createdAt;
        }

        public int Id { get; }
        public int DocumentId { get; }
        public int VersionNumber { get; }
        public DateTime CreatedAt { get; }
    }
}