using ConsoleApp8;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
class ChatServer : IChatServer
{
    private ConcurrentBag<TcpClient> clients;
    private ConcurrentBag<string> logins;
    private IClientProcessor clientProccessor;

    public ChatServer()
    {
        clients = new ConcurrentBag<TcpClient>();
        logins = new ConcurrentBag<string>();
        clientProccessor = new ClientProcessor(clients, logins);
    }

    public int GetFreePort()
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

    public void Start()
    {
        int port = GetFreePort();
        Console.WriteLine("Введите логин: ");
        clientProccessor.SetStartLogin();
        Thread listenerThread = new Thread(() =>
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                Console.WriteLine($"Подключен новый клиент: {client.Client.RemoteEndPoint}");

                Thread handleThread = new Thread(() =>
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();

                        while (client.Connected)
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
                                int newClientPort = int.Parse(match.Groups[1].Value);
                                try
                                {
                                    TcpClient newClient = new TcpClient();
                                    newClient.Connect("localhost", newClientPort);
                                    clients.Add(newClient);
                                    NetworkStream streamLogin = newClient.GetStream();
                                    byte[] messageBytes = Encoding.UTF8.GetBytes("login " + clientProccessor.GetLogin());
                                    streamLogin.Write(messageBytes, 0, messageBytes.Length);
                                }
                                catch (Exception e)
                                {
                                    // Обработка ошибок
                                }
                            }
                            else
                            {
                                Regex regexLogin = new Regex(@"login (\w+)");
                                Match matchLogin = regexLogin.Match(message);
                                if (matchLogin.Success)
                                {
                                    string clientLogin = matchLogin.Groups[1].Value;
                                    logins.Add(clientLogin);
                                } else
                                {
                                    clientProccessor.WriteIntoConsoleAndFile(message);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // Обработка ошибок
                    }
                    finally
                    {
                        client.Close();
                    }
                });
                handleThread.Start();
            }
        });
        listenerThread.Start();
        clientProccessor.Handshake(port);
        clientProccessor.StartChatting();
    }
}