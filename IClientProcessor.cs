using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp8
{
    public interface IClientProcessor
    {
        void Handshake(int port);
        void BroadcastMessage(string message);
        void StartChatting();
        void SetStartLogin();
        void AddLogin(string login);
        void AddClient(TcpClient client);

        string GetLogin();
    }
}