using ConsoleApp7;
using ConsoleApp8;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

class ClientProcessor : IClientProcessor
{
    private string Login;
    private bool mute;

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

    public bool IsMute()
    {
        return mute;
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

    private void ConsoleClientWrite(string refactorMessage)
    {
        Console.SetCursorPosition(0, Console.CursorTop - 1); // переместить курсор в начало предыдущей строки
        Console.Write(new string(' ', Console.WindowWidth)); // очистить предыдущее сообщение
        Console.SetCursorPosition(0, Console.CursorTop - 1); // переместить курсор в начало предыдущей строки
        Console.WriteLine(refactorMessage); // вывести отформатированное сообщение
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
        //storage.SetLoginStorage(Login);

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
            else if (Regex.IsMatch(line, @"^/migrate$"))
            {
                var databaseMigrator = new DatabaseMigrator();
                databaseMigrator.Migrate();
                Console.WriteLine("Миграция проведена успешно");
            }
            else if (Regex.IsMatch(line, @"^/mute$"))
            {
                mute = true;
            }
            else if (Regex.IsMatch(line, @"^/mute\s+(\d+)\s*(s|m)?$"))
            {
                // Отключение приема сообщений на указанное время
                mute = true;

                Match match = Regex.Match(line, @"^/mute\s+(\d+)\s*(s|m)?$");
                int time = int.Parse(match.Groups[1].Value);
                string timeUnit = match.Groups[2].Value;

                if (timeUnit == "m")
                {
                    time *= 60;
                }
                var timer = new System.Timers.Timer(time * 1000);
                timer.Elapsed += (sender, e) =>
                {
                    mute = false;
                    timer.Dispose(); // Освобождаем ресурсы таймера
                };
                timer.AutoReset = false; // Запускаем таймер только один раз
                timer.Start();
            }
            else if (Regex.IsMatch(line, @"^/unmute$"))
            {
                mute = false;
            }
            else if (Regex.IsMatch(line, @"^\/history(\s+--count=\d{1,4})?(\s+--before=\d{4}\.\d{2}\.\d{2}T\d{2}:\d{2})?(\s+--after=\d{4}\.\d{2}\.\d{2}T\d{2}:\d{2})?$"))
            {
                var countMatch = Regex.Match(line, @"-count=(\d{1,4})");
                var beforeMatch = Regex.Match(line, @"--before=(\d{4}\.\d{2}\.\d{2}T\d{2}:\d{2})");
                var afterMatch = Regex.Match(line, @"--after=(\d{4}\.\d{2}\.\d{2}T\d{2}:\d{2})");

                int count = 100;
                DateTime? before = null;
                DateTime? after = null;

                if (countMatch.Success)
                {
                    count = Math.Min(int.Parse(countMatch.Groups[1].Value), 1000);
                }

                if (beforeMatch.Success)
                {
                    before = DateTime.ParseExact(beforeMatch.Groups[1].Value, "yyyy.MM.ddTHH:mm", CultureInfo.InvariantCulture);
                }

                if (afterMatch.Success)
                {
                    after = DateTime.ParseExact(afterMatch.Groups[1].Value, "yyyy.MM.ddTHH:mm", CultureInfo.InvariantCulture);
                }

                storage.ReadHistory(count, before, after);
            }
            else
            {
                // Отправляем сообщение всем доступным клиентам
                string refactorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.ff}] {Login}[{storage.GetLastId()}]: \"{line}\"";
                ConsoleClientWrite(refactorMessage);
                storage.WriteIntoStorage(refactorMessage);
                BroadcastMessage(refactorMessage);
                //storage.WriteIntoFile(refactorMessage);
            }
        }
    }
}