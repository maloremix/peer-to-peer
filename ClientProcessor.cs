﻿using ConsoleApp8;
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

    private ConcurrentBag<TcpClient> clients;
    private ConcurrentBag<string> logins;
    private BinaryWriter Writer;
    private int LastId = 1;
    
    public ClientProcessor()
    {
        clients = new ConcurrentBag<TcpClient>();
        logins = new ConcurrentBag<string>();
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

    public void SayLogin()
    {
        BroadcastMessage($"Client connected with login {Login}");
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

    // TODO разбить логику отображения и записи сообщений в хранилище
    public void WriteIntoConsoleAndFile(string message)
    {
        string refactorMessage = Regex.Replace(message, @"\[\d+\]", "[" + LastId.ToString() + "]");
        Console.WriteLine(refactorMessage);
        Writer.Write(refactorMessage);
        Writer.Flush();
        LastId++;
    }
    public void WriteIntoFile(string message)
    {
        Writer.Write(message);
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
        // TODO подумать над тем, как исправить эту логику
        //while (logins.Count != clients.Count)
        //{
        //    Thread.Sleep(100);
        //}
        while (logins.Contains(Login))
        {
            Console.WriteLine("Данный логин уже используется");
            Console.WriteLine("Введите логин еще раз: ");
            Login = Console.ReadLine();
        }

        Console.WriteLine("Соединение установлено");
        SayLogin();

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
                string refactorMessage = $"[{DateTime.Now}] {Login}[{LastId}]: \"{line}\"";
                BroadcastMessage(refactorMessage);
                WriteIntoFile(refactorMessage);
            }
        }
    }
}