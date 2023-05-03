using ConsoleApp7;
using ConsoleApp8;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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

    private void HandleNewClientMessage(int newClientPort, int ourPort)
    {
            TcpClient newClient = new TcpClient();
            newClient.Connect("localhost", newClientPort);
            clientProcсessor.AddClient(newClient);
            var handshake = new UserInfo()
            {
                Login = clientProcсessor.GetLogin(),
                Port = ourPort,
                MessageType = "UserInfo"
            };
            

            var outputContent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(handshake));
            var outputContentLength = outputContent.Length;
            NetworkStream streamLogin = newClient.GetStream();
            streamLogin.Write(outputContent, 0, outputContentLength);
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

                        var Type = JsonConvert.DeserializeObject<Type>(message);

                        if (Type.MessageType == "Handshake")
                        {
                            var handshakeObject = JsonConvert.DeserializeObject<Handshake>(message);
                            HandleNewClientMessage(handshakeObject.Port, port);
                            continue;
                        }
                        if (Type.MessageType == "UserInfo")
                        {
                            var userInfoObject = JsonConvert.DeserializeObject<UserInfo>(message);
                            Console.WriteLine($"Подключен новый клиент: {userInfoObject.Login}");
                            string clientLogin = userInfoObject.Login;
                            int clientPort = userInfoObject.Port;
                            clientProcсessor.AddLogin(clientLogin);
                            clientProcсessor.AddCleintLoginMap(clientPort, clientLogin);
                            if (clientProcсessor.GetBotName() == "butler")
                            {
                                string refactorMessageBot = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}] {clientProcсessor.GetLogin()}[{storage.GetLastId()}]: \"{"Привет " + clientLogin}\"";
                                clientProcсessor.BroadcastMessage(refactorMessageBot);
                            }
                            continue;
                        }

                        if (Type.MessageType == "Bot")
                        {
                            var botObject = JsonConvert.DeserializeObject<Bot>(message);
                            
                            if (botObject.botCommand == "/weather" && clientProcсessor.GetBotName() == "weather")
                            {
                                var weatherData = await WeatherClass.GetWeatherAsync("Voronezh");
                                string weather = weatherData.MainWeather;
                                double temperature = weatherData.Temperature;
                                string messageWeather = $"Сейчас {weather}, температура {temperature}";
                                string refactorMessageBot = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}] {clientProcсessor.GetLogin()}[{storage.GetLastId()}]: \"{messageWeather}\"";
                                clientProcсessor.BroadcastMessage(refactorMessageBot);
                            }

                            if (botObject.botCommand == "/joke" && clientProcсessor.GetBotName() == "joker")
                            {
                                string joke = storage.GetRandomJoke();
                                string refactorMessageBot = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}] {clientProcсessor.GetLogin()}[{storage.GetLastId()}]: \"{joke}\"";
                                clientProcсessor.BroadcastMessage(refactorMessageBot);
                            }
                            continue;
                        }

                        var messageObject = JsonConvert.DeserializeObject<Message>(message);
                        string refactorMessage = Regex.Replace(messageObject.Text, @"\[\d+\]", "[" + storage.GetLastId().ToString() + "]");
                        if (!clientProcсessor.IsMute())
                        {
                            Console.WriteLine(refactorMessage);
                            storage.WriteIntoStorage(refactorMessage);
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

class Type
{
    public string MessageType;
}

class UserInfo
{
    public string Login { get; set; }

    public int Port { get; set; }
    public string MessageType;
}

class Handshake
{
    public int Port { get; set; }
    public string MessageType;
}

class Message
{
    public string Text { get; set; }
    public string MessageType;
}

class Bot
{
    public string botCommand { get; set; }
    public string MessageType;
}