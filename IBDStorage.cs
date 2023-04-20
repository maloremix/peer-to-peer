using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp7
{
    internal interface IBDStorage
    {
        void ReadHistory();
        void WriteIntoConsole(string message);
        void AddMessage(string source, string message, DateTime date);
        void SetLoginStorage(string login);
        void Migrate();
        int GetLastId();
    }
}
