using ConsoleApp8;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

public class ClientProcessor : IClientProcessor
{
    public string login { get; set; }

    private ConcurrentBag<TcpClient> clients;
    private ConcurrentBag<string> logins;
    public ClientProcessor(ConcurrentBag<TcpClient> clients, ConcurrentBag<string> logins)
    {
        this.clients = clients;
        this.logins = logins;
    }

    public void handshake(int port)
    {
        // Производим хендшейк с каждым портом от 5000 до 5020
        for (int remotePort = 5000; remotePort <= 5005; remotePort++)
        {
            if (remotePort == port)
            {
                continue;
            }
            try
            {
                TcpClient client = new TcpClient();
                client.Connect("localhost", remotePort); // Устанавливаем соединение с хостом и портом
                clients.Add(client); // Добавляем клиента в список
                NetworkStream stream = client.GetStream();
                byte[] messageBytes = Encoding.ASCII.GetBytes("Client connected with port " + port);
                stream.Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception e)
            {
                // Обработка ошибок
            }
        }
    }

    public void BroadcastMessage(string message)
    {
        foreach (var client in clients)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                stream.Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception e)
            {
                // Обработка ошибок
            }
        }
    }
    public void startChatting()
    {
        while (logins.Count != clients.Count)
        {
            Thread.Sleep(100);
        }
        string login;
        Console.WriteLine("Введите логин: ");
        while (logins.Contains(this.login = Console.ReadLine()))
        {
            Console.WriteLine("Данный логин уже используется");
            Console.WriteLine("Введите логин еще раз: ");
        }

        string line;
        while ((line = Console.ReadLine()) != "exit")
        {
            // Отправляем сообщение всем доступным клиентам
            BroadcastMessage(line);
        }
    }
}