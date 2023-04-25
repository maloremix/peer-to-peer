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
    private DateTime messageTime;
    public ChatServer(IClientProcessor clientProcсessor, IStorage storage)
    {
        this.clientProcсessor = clientProcсessor;
        this.storage = storage;
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

    private void HandleNewClientMessage(string newClientPort, int ourPort)
    {
            TcpClient newClient = new TcpClient();
            newClient.Connect("localhost", int.Parse(newClientPort));
            clientProcсessor.AddClient(newClient);
            NetworkStream streamLogin = newClient.GetStream();
            byte[] messageBytes = Encoding.UTF8.GetBytes("login " + clientProcсessor.GetLogin() + " and port " + ourPort);
            streamLogin.Write(messageBytes, 0, messageBytes.Length);
    }

    public void Start()
    {
        int port = GetFreePort();

        clientProcсessor.SetStartLogin();
        Console.WriteLine("Устанавливается соединение с другими клиентами...");
        Thread listenerThread = new Thread(async () =>
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
                            HandleNewClientMessage(match.Groups[1].Value, port);
                            continue;
                        }
                        Regex regexLogin = new Regex(@"login (\S+) and port (\d+)");
                        Match matchLogin = regexLogin.Match(message);
                        if (matchLogin.Success)
                        {
                            Console.WriteLine($"Подключен новый клиент: {matchLogin.Groups[1].Value}");
                            string clientLogin = matchLogin.Groups[1].Value;
                            int clientPort = int.Parse(matchLogin.Groups[2].Value);
                            clientProcсessor.AddLogin(clientLogin);
                            clientProcсessor.AddCleintLoginMap(clientPort, clientLogin);
                            if (clientProcсessor.GetBotName() == "butler")
                            {
                                string refactorMessageBot = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}] {clientProcсessor.GetLogin()}[{storage.GetLastId()}]: \"{"Привет " + clientLogin}\"";
                                clientProcсessor.BroadcastMessage(refactorMessageBot);
                            }
                            continue;
                        }
                        Regex regexLoginFrom = new Regex(@"Client connected with login (\S+) and port (\d+)");
                        Match matchLoginFrom = regexLoginFrom.Match(message);
                        if (matchLoginFrom.Success)
                        {
                            string clientLogin = matchLogin.Groups[1].Value;
                            int clientPort = int.Parse(matchLogin.Groups[2].Value);
                            clientProcсessor.AddCleintLoginMap(clientPort, clientLogin);
                            Console.WriteLine($"Подключен новый клиент: {clientLogin}");
                            if (clientProcсessor.GetBotName() == "butler")
                            {
                                string refactorMessageBot = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}] {clientProcсessor.GetLogin()}[{storage.GetLastId()}]: \"{"Привет " + clientLogin}\"";
                                clientProcсessor.BroadcastMessage(refactorMessageBot);
                            }
                            continue;
                        }

                        Regex regexWeather = new Regex(@"^/weather$");
                        Match matchWeather = regexWeather.Match(message);
                        if (matchWeather.Success)
                        {
                            if (clientProcсessor.GetBotName() == "weather")
                            {
                                var weatherData = await WeatherClass.GetWeatherAsync("Voronezh");
                                string weather = weatherData.MainWeather;
                                double temperature = weatherData.Temperature;
                                string messageWeather = $"Сейчас {weather}, температура {temperature}";
                                string refactorMessageBot = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}] {clientProcсessor.GetLogin()}[{storage.GetLastId()}]: \"{messageWeather}\"";
                                clientProcсessor.BroadcastMessage(refactorMessageBot);
                            }
                            continue;
                        }
                        Regex regexJoke = new Regex(@"^/joke$");
                        Match matchJoke = regexJoke.Match(message);
                        if (matchJoke.Success)
                        {
                            if (clientProcсessor.GetBotName() == "joker")
                            {
                                string joke = storage.GetRandomJoke();
                                string refactorMessageBot = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}] {clientProcсessor.GetLogin()}[{storage.GetLastId()}]: \"{joke}\"";
                                clientProcсessor.BroadcastMessage(refactorMessageBot);
                            }
                            continue;
                        }
                        string refactorMessage = Regex.Replace(message, @"\[\d+\]", "[" + storage.GetLastId().ToString() + "]");
                        if (!clientProcсessor.IsMute())
                        {
                            Console.WriteLine(refactorMessage);
                            storage.WriteIntoStorage(message);
                        }
                        if (clientProcсessor.GetBotName() == "joker")
                        {
                            TimeSpan timeElapsed = DateTime.Now.Subtract(messageTime); // сколько времени прошло с момента отправки сообщения
                            if (timeElapsed.TotalMinutes > 5) // если прошло более 5 минут
                            {
                                string refactorMessageBot = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}] {clientProcсessor.GetLogin()}[{storage.GetLastId()}]: \"Что-то у вас тут скучно. Ловите анекдот\"";
                                clientProcсessor.BroadcastMessage(refactorMessageBot);
                                string joke = storage.GetRandomJoke();
                                string refactorMessageJoke = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}] {clientProcсessor.GetLogin()}[{storage.GetLastId()}]: \"{joke}\"";
                                clientProcсessor.BroadcastMessage(refactorMessageJoke);
                            }
                        }
                        messageTime = DateTime.Now;
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