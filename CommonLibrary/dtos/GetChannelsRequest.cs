namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Used when creating a new channel
    /// </summary>
    public class GetChannelsRequest
    {

        public GetChannelsRequest(int userId) 
        { 
            UserId = userId;
        }
        public int UserId { get; set; }
    }
}