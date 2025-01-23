using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class RegisterRequest
    {
        public string Username { get;}
        public string Password { get; }

        public RegisterRequest(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
