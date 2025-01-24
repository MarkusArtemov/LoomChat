namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class CreateDocumentRequest
    {
        public string Name { get; set; } = string.Empty;
        public int ChannelId { get; set; }

        public CreateDocumentRequest() { }

        public CreateDocumentRequest(string name, int channelId)
        {
            Name = name;
            ChannelId = channelId;
        }
    }
}
