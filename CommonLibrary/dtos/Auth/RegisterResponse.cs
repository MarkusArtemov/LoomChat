using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class RegisterResponse
    {
        public string Token { get; }
        public int UserID { get; }

        public string Username { get; }

        public RegisterResponse(string token, int userId, string username)
        {
            Token = token;
            UserID = userId;
            Username = username;
        }
    }
}
