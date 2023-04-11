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
        void Handshake(int port);
        void BroadcastMessage(string message);
        void StartChatting();
        void WriteIntoConsoleAndFile(string message);
        void WriteIntoFile(string message);
        void SetStartLogin();
        void DeleteMessageById(int id);
    }
}