using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp8
{
    public interface IClientProcessor
    {
        string login { get; set; }
        void handshake(int port);
        void BroadcastMessage(string message);
        void startChatting();
    }
}