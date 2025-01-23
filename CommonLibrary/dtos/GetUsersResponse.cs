using De.Hsfl.LoomChat.Common.Models;
using System.Collections.Generic;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    /// <summary>
    /// Used when creating a new channel
    /// </summary>
    public class GetUsersResponse
    {

        public GetUsersResponse(List<User> users) 
        {
            Users = users;
        }
        public List<User> Users { get; set; }
    }
}