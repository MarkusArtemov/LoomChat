using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.dtos
{
    public class PortResponse
    {
        public int Port { get; set; }
        public PortResponse(int port)
        {
            Port = port;
        }
    }
}
