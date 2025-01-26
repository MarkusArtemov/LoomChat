using System;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class DocumentVersionResponse
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public DateTime CreatedAt { get; set; }

        public string FileExtension { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;

        public DocumentVersionResponse() { }

        public DocumentVersionResponse(
            int id,
            int documentId,
            int versionNumber,
            DateTime createdAt,
            string fileExtension,
            string fileType)
        {
            Id = id;
            DocumentId = documentId;
            VersionNumber = versionNumber;
            CreatedAt = createdAt;
            FileExtension = fileExtension;
            FileType = fileType;
        }

        // Vollständiger "Version-Name"
        public string FullVersionName => $"v{VersionNumber}{FileExtension}";
    }
}
