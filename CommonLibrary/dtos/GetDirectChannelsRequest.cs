namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Used when creating a new channel
    /// </summary>
    public class GetDirectChannelsRequest
    {
        public GetDirectChannelsRequest(int userId) 
        {
            UserId = userId;
        }
        public int UserId { get; set; }
    }
}