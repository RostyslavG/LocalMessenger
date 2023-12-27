using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ChatUser
    {
        public string Nick { get; set; }
        public TcpClient Client { get; set; }
    }
}
