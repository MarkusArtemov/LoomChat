using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = null;
        public string PasswordHash { get; set; } = null;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Token { get; set; } = null;

        public User()
        {

        }

        public User(int id, string username)
        {
            Username = username;
            Id = id;
        }
    }
}
