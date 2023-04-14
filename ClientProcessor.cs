using ConsoleApp7;
using ConsoleApp8;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

public class ClientProcessor : IClientProcessor
{
    private string Login;


    private IStorage storage;
    private ConcurrentBag<TcpClient> clients;
    private ConcurrentBag<string> logins;
    
    public ClientProcessor(IStorage storage)
    {
        clients = new ConcurrentBag<TcpClient>();
        logins = new ConcurrentBag<string>();
        this.storage = storage;
    }

    public string GetLogin()
    {
        return Login;
    }
    public void Handshake(int port)
    {
        var firstChatPort = 5000;
        var lastChatPort = 5020;
        
        var usedPorts = IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Where(it => it.Port >= firstChatPort && it.Port <= lastChatPort && it.Port != port)
            .Select(it => it.Port);

        foreach (var usedPort in usedPorts)
        {
            TcpClient client = new TcpClient();
            client.Connect("localhost", usedPort); // Устанавливаем соединение с хостом и портом
            clients.Add(client); // Добавляем клиента в список
            NetworkStream stream = client.GetStream();
            byte[] messageBytes = Encoding.UTF8.GetBytes("Client connected with port " + port);
            stream.Write(messageBytes, 0, messageBytes.Length);
        }
    }

    public void AddClient(TcpClient client)
    {
        clients.Add(client);
    }

    public void AddLogin(string login)
    {
        logins.Add(login);
    }

    public void SetStartLogin()
    {
        Console.WriteLine("Введите логин: ");
        Login = Console.ReadLine();
    }

    public void BroadcastMessage(string message)
    {
        foreach (var client in clients)
        {
            NetworkStream stream = client.GetStream();
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            stream.Write(messageBytes, 0, messageBytes.Length);
        }
    }

    public void SayLogin()
    {
        BroadcastMessage($"Client connected with login {Login}");
    }

    public void StartChatting()
    {
        // TODO подумать над тем, как исправить эту логику
        while (logins.Count != clients.Count)
        {
            Thread.Sleep(100);
        }
        Console.WriteLine("Соединение установлено");
        while (logins.Contains(Login))
        {
            Console.WriteLine("Данный логин уже используется");
            Console.WriteLine("Введите логин еще раз: ");
            Login = Console.ReadLine();
        }

        storage.SetLoginStorage(Login);

        SayLogin();

        string line;
        while ((line = Console.ReadLine()) != "exit")
        {
            if (Regex.IsMatch(line, @"^del-mes\s+[0-9]+$"))
            {
                Match match = Regex.Match(line, @"^del-mes\s+(?<id>[0-9]+)$");
                int id = int.Parse(match.Groups["id"].Value);
                storage.DeleteMessageById(id);
                Console.WriteLine("Сообщение с id " + id + " Удалено");
            }
            else
            {
                // Отправляем сообщение всем доступным клиентам
                string refactorMessage = $"[{DateTime.Now}] {Login}[{storage.GetLastId()}]: \"{line}\"";
                BroadcastMessage(refactorMessage);
                storage.WriteIntoFile(refactorMessage);
            }
        }
    }
}