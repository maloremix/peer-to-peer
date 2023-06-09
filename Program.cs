﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp7;
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
                .AddSingleton<IStorage, BDStorage>()
                .BuildServiceProvider();

            var chatServer = serviceProvider.GetService<IChatServer>();
            chatServer.Start();
        }
    }
}
