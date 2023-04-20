using ConsoleApp7;
using ConsoleApp8;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
class ChatServer : IChatServer
{
    private IClientProcessor clientProcсessor;
    private IStorage storage;
    private IStorage bdStorage;

    public ChatServer(IClientProcessor clientProcсessor, IStorage storage, IStorage bdStorage)
    {
        this.clientProcсessor = clientProcсessor;
        this.storage = storage;
        this.bdStorage = bdStorage;
    }
    private int GetFreePort()
    {
        var firstChatPort = 5000;
        var lastChatPort = 5020;

        var usedPorts = IPGlobalProperties
            .GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Where(it => it.Port >= firstChatPort && it.Port <= lastChatPort)
            .Select(it => it.Port);

        for (int port = firstChatPort; port <= lastChatPort; port++)
        {
            if (!usedPorts.Contains(port))
            {
                return port;
            }
        }

        return 0;
    }

    private void HandleNewClientMessage(string newClientPort)
    {
            TcpClient newClient = new TcpClient();
            newClient.Connect("localhost", int.Parse(newClientPort));
        clientProcсessor.AddClient(newClient);
            NetworkStream streamLogin = newClient.GetStream();
            byte[] messageBytes = Encoding.UTF8.GetBytes("login " + clientProcсessor.GetLogin());
            streamLogin.Write(messageBytes, 0, messageBytes.Length);
    }

    public void Start()
    {
        int port = GetFreePort();

        clientProcсessor.SetStartLogin();
        Console.WriteLine("Устанавливается соединение с другими клиентами...");
        Thread listenerThread = new Thread(() =>
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            List<TcpClient> listeningClients = new List<TcpClient>();
            while (true)
            {
                // Принимаем новых клиентов и добавляем их в список
                if (listener.Pending())
                {
                    TcpClient client = listener.AcceptTcpClient();
                    listeningClients.Add(client);
                }

                // Обрабатываем данные от всех клиентов в списке
                foreach (TcpClient client in listeningClients)
                {
                    NetworkStream stream = client.GetStream();

                    // Если есть данные для чтения, обрабатываем их
                    if (stream.DataAvailable)
                    {
                        byte[] buffer = new byte[client.ReceiveBufferSize];
                        int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                        if (bytesRead == 0)
                        {
                            break;
                        }
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Regex regex = new Regex(@"Client connected with port (\d+)");
                        Match match = regex.Match(message);
                        if (match.Success)
                        {
                            HandleNewClientMessage(match.Groups[1].Value);
                            continue;
                        }
                        Regex regexLogin = new Regex(@"login (\w+)");
                        Match matchLogin = regexLogin.Match(message);
                        if (matchLogin.Success)
                        {
                            Console.WriteLine($"Подключен новый клиент: {matchLogin.Groups[1].Value}");
                            string clientLogin = matchLogin.Groups[1].Value;
                            clientProcсessor.AddLogin(clientLogin);
                            continue;
                        }
                        Regex regexLoginFrom = new Regex(@"Client connected with login (\w+)");
                        Match matchLoginFrom = regexLoginFrom.Match(message);
                        if (matchLoginFrom.Success)
                        {
                            Console.WriteLine($"Подключен новый клиент: {matchLoginFrom.Groups[1].Value}");
                            continue;
                        }
                        //bdStorage.WriteIntoStorage(message);
                        string refactorMessage = Regex.Replace(message, @"\[\d+\]", "[" + storage.GetLastId().ToString() + "]");
                        Console.WriteLine(refactorMessage);
                        storage.WriteIntoStorage(message);
                        //storage.WriteIntoConsole(refactorMessage);
                        //storage.WriteIntoFile(refactorMessage);
                    }
                }

                // Удаляем отключившихся клиентов из списка
                listeningClients.RemoveAll(c => !c.Connected);
            }
        });
        listenerThread.Start();
        clientProcсessor.Handshake(port);
        clientProcсessor.StartChatting();
    }
}