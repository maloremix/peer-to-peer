using ConsoleApp8;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

public class ClientProcessor : IClientProcessor
{
    private string Login;

    private ConcurrentBag<TcpClient> clients;
    private ConcurrentBag<string> logins;
    private BinaryWriter Writer;
    private int LastId = 1;
    public ClientProcessor(ConcurrentBag<TcpClient> clients, ConcurrentBag<string> logins)
    {
        this.clients = clients;
        this.logins = logins;
    }

    public string GetLogin()
    {
        return Login;
    }
    public void Handshake(int port)
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
                byte[] messageBytes = Encoding.UTF8.GetBytes("Client connected with port " + port);
                stream.Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception e)
            {
                // Обработка ошибок
            }
        }
    }

    public void SetStartLogin()
    {
        Login = Console.ReadLine();
        Console.WriteLine("Устанавливается соединение с другими клиентами...");
    }

    public void BroadcastMessage(string message)
    {
        foreach (var client in clients)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                stream.Write(messageBytes, 0, messageBytes.Length);
            }
            catch (Exception e)
            {
                // Обработка ошибок
            }
        }
    }


    public void readHistory(string login)
    {
        string filePath = $"{login}.dat";
        if (File.Exists(filePath))
        {
            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                string lastLine = "";
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    string line = reader.ReadString();
                    lastLine = line;
                    Console.WriteLine(line);
                }

                // Ищем идентификаторы в последней строке
                Regex regex = new Regex(@"\[(\d+)\]");
                Match match = regex.Match(lastLine);
                if (match.Success)
                {
                    LastId = int.Parse(match.Groups[1].Value) + 1;
                }
            }
        }
    }

    public void WriteIntoConsoleAndFile(string message)
    {
        string refactorMessage = $"[{DateTime.Now}] {Login}[{LastId}]: \"{message}\"";
        Console.WriteLine(refactorMessage);
        Writer.Write(refactorMessage);
        Writer.Flush();
        LastId++;
    }
    public void WriteIntoFile(string message)
    {
        string refactorMessage = $"[{DateTime.Now}] {Login}[{LastId}]: \"{message}\"";
        Writer.Write(refactorMessage);
        Writer.Flush();
        LastId++;
    }

    public void DeleteMessageById(int id)
    {
        Writer.Dispose();
        string filePath = $"{Login}.dat";
        string tempFilePath = $"{Login}_temp.dat";

        if (File.Exists(filePath))
        {
            using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(tempFilePath, FileMode.Create)))
                {
                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        string line = reader.ReadString();

                        // Получаем id из строки с помощью регулярного выражения
                        Match match = Regex.Match(line, @"\[(\d+)\]:");
                        if (match.Success)
                        {
                            int messageId = int.Parse(match.Groups[1].Value);

                            // Если id совпадает с искомым, то не записываем строку во временный файл
                            if (messageId == id)
                            {
                                continue;
                            }
                        }

                        // Записываем строку во временный файл
                        writer.Write(line);
                    }
                }
            }

            // Удаляем старый файл и переименовываем временный файл
            File.Delete(filePath);
            File.Move(tempFilePath, filePath);
            Writer = new BinaryWriter(new FileStream($"{Login}.dat", FileMode.Append, FileAccess.Write, FileShare.None));
        }
    }

    public void StartChatting()
    {
        while (logins.Count != clients.Count)
        {
            Thread.Sleep(100);
        }
        while (logins.Contains(Login))
        {
            Console.WriteLine("Данный логин уже используется");
            Console.WriteLine("Введите логин еще раз: ");
            Login = Console.ReadLine();
        }

        Console.WriteLine("Соединение установлено");

        readHistory(Login);

        Writer = new BinaryWriter(new FileStream($"{Login}.dat", FileMode.Append, FileAccess.Write, FileShare.None));

        string line;
        while ((line = Console.ReadLine()) != "exit")
        {
            if (Regex.IsMatch(line, @"^del-mes\s+[0-9]+$"))
            {
                Match match = Regex.Match(line, @"^del-mes\s+(?<id>[0-9]+)$");
                int id = int.Parse(match.Groups["id"].Value);
                DeleteMessageById(id);
                Console.WriteLine("Сообщение с id " + id + " Удалено");
            }
            else
            {
                // Отправляем сообщение всем доступным клиентам
                BroadcastMessage(line);
                WriteIntoFile(line);
            }
        }
    }
}