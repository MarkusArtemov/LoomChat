namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Used when creating a new document
    /// </summary>
    public class CreateDocumentRequest
    {
        public CreateDocumentRequest(string name, int channelId, int ownerUserId, string description)
        {
            Name = name;
            ChannelId = channelId;
            OwnerUserId = ownerUserId;
            Description = description;
        }

        public string Name { get; }
        public int ChannelId { get; }
        public int OwnerUserId { get; }
        public string Description { get; }
    }
}
