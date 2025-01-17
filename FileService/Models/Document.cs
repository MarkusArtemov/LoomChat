namespace De.Hsfl.LoomChat.File.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int OwnerUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }
        public int ChannelId { get; set; }

        public ICollection<DocumentVersion> DocumentVersions { get; set; }
            = new List<DocumentVersion>();
    }
}
