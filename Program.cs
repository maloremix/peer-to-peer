using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleApp8
{
    internal class Program
    {
        static ServiceProvider serviceProvider;
        static void Main(string[] args)
        {
            serviceProvider = new ServiceCollection()
                .AddSingleton<IClientProcessor, ClientProcessor>()
                .AddSingleton<IChatServer, ChatServer>()
                .BuildServiceProvider();

            // TODO убрать все блоки try catch
            var chatServer = serviceProvider.GetService<IChatServer>();
            chatServer.Start();
        }
    }
}
