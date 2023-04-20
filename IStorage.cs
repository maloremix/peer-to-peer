using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp7
{
    public interface IStorage
    {
        void DeleteMessageById(int id);
        void WriteIntoStorage(string message);
        void SetLoginStorage(string login);
        int GetLastId();
    }
}
