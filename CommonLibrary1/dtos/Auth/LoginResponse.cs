using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class LoginResponse
    {
        public string Token { get;}
        public int UserID { get;}

        public string Username { get; }

        public LoginResponse(string token, int userId, string username)
        {
            Token = token;
            UserID = userId;
            Username = username;
        }
    }
}
