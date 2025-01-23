using De.Hsfl.LoomChat.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Client.Global
{
    public static class SessionStore
    {
        public static string JwtToken { get; set; }
        public static User User { get; set; }
    }
}
