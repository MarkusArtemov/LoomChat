using System;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class DocumentVersionResponse
    {
        public int Id { get; }
        public int DocumentId { get; }
        public int VersionNumber { get; }
        public DateTime CreatedAt { get; }
        public string FileExtension { get; }
        public string FileType { get; }

        public DocumentVersionResponse(int id, int documentId, int versionNumber, DateTime createdAt, string fileExtension, string fileType)
        {
            Id = id;
            DocumentId = documentId;
            VersionNumber = versionNumber;
            CreatedAt = createdAt;
            FileExtension = fileExtension;
            FileType = fileType;
        }
    }
}
