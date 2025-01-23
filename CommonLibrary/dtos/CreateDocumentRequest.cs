namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Used when creating a new document
    /// </summary>
    public class CreateDocumentRequest
    {
        public CreateDocumentRequest(string name, int channelId)
        {
            Name = name;
            ChannelId = channelId;
        }

        public string Name { get; }
        public int ChannelId { get; }
    }
}
