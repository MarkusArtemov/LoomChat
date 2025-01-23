namespace De.Hsfl.LoomChat.File.Models
{
    /// <summary>
    /// Represents a version of a document
    /// </summary>
    public class DocumentVersion
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public bool IsFull { get; set; }
        public int? BaseVersionId { get; set; }
        public string StoragePath { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Document? Document { get; set; }
    }
}
