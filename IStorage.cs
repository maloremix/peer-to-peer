using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp7
{
    public interface IStorage
    {
        void WriteIntoConsole(string message);
        void WriteIntoFile(string message);
        void DeleteMessageById(int id);
        void SetLoginStorage(string login);
        int GetLastId();
    }
}
