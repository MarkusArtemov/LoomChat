using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public int UserID { get; set; }

        public string Username { get; set; }

        public LoginResponse(string token, int userId, string username)
        {
            Token = token;
            UserID = userId;
            Username = username;
        }
    }
}
