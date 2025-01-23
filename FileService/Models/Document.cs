namespace De.Hsfl.LoomChat.File.Models
{
    /// <summary>
    /// Represents a stored document
    /// </summary>
    public class Document
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int OwnerUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int ChannelId { get; set; }
        public string FileType { get; set; } = null!;
        public string FileExtension { get; set; } = ".bin";


        public ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();
    }
}
