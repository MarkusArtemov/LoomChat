using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Dtos
{
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public RegisterRequest(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
