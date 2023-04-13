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
    private IClientProcessor clientProccessor;

    public ChatServer(IClientProcessor clientProcessor)
    {
        clientProccessor = clientProcessor;
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
        try
        {
            TcpClient newClient = new TcpClient();
            newClient.Connect("localhost", int.Parse(newClientPort));
            clientProccessor.AddClient(newClient);
            NetworkStream streamLogin = newClient.GetStream();
            byte[] messageBytes = Encoding.UTF8.GetBytes("login " + clientProccessor.GetLogin());
            streamLogin.Write(messageBytes, 0, messageBytes.Length);
        }
        catch (Exception e)
        {
            // Обработка ошибок
        }
    }

    public void Start()
    {
        int port = GetFreePort();

        clientProccessor.SetStartLogin();
        Console.WriteLine("Устанавливается соединение с другими клиентами...");
        Thread listenerThread = new Thread(() =>
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            List<TcpClient> listeningClients = new List<TcpClient>();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                
                
                
                

                // TODO не создавать поток под каждого клиента. Один поток должен работать со всеми клиентами
                Thread handleThread = new Thread(() =>
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();
                        
                        while (client.Connected)
                        {
                            while (true)
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
                                    clientProccessor.AddLogin(clientLogin);
                                    continue;
                                }
                                Regex regexLoginFrom = new Regex(@"Client connected with login (\w+)");
                                Match matchLoginFrom = regexLoginFrom.Match(message);
                                if (matchLoginFrom.Success)
                                {
                                    Console.WriteLine($"Подключен новый клиент: {matchLoginFrom.Groups[1].Value}");
                                    continue;
                                }

                                clientProccessor.WriteIntoConsoleAndFile(message);
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