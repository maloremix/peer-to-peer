using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        // Запрашиваем у пользователя номер порта для подключения
        Console.Write("Введите номер порта: ");
        int port = int.Parse(Console.ReadLine());

        // Создаем список клиентов
        var clients = new ConcurrentBag<TcpClient>();

        // Создаем новый поток для прослушивания входящих сообщений
        Thread listenerThread = new Thread(() =>
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

                // Добавляем клиента в список клиентов
                Console.WriteLine($"Подключен новый клиент: {client.Client.RemoteEndPoint}");

                // Создаем новый поток для обработки сообщений
                Thread handleThread = new Thread(() =>
                {
                    try
                    {
                        NetworkStream stream = client.GetStream();

                        while (client.Connected) // Цикл для обработки сообщений до отключения клиента
                        {
                            byte[] buffer = new byte[client.ReceiveBufferSize];
                            int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                            if (bytesRead == 0)
                            {
                                // Если клиент отключился, выходим из цикла
                                break;
                            }
                            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                            Console.WriteLine(message);
                            Regex regex = new Regex(@"Client connected with port (\d+)");
                            Match match = regex.Match(message);
                            if (match.Success)
                            {
                                int newClientPort = int.Parse(match.Groups[1].Value);
                                try
                                {
                                    TcpClient newClient = new TcpClient();
                                    newClient.Connect("localhost", newClientPort); // Устанавливаем соединение с хостом и портом
                                    clients.Add(newClient); // Добавляем клиента в список
                                }
                                catch (Exception e)
                                {
                                    // Обработка ошибок
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
                        // Закрываем клиентский сокет после обработки сообщений
                        client.Close();
                    }
                });
                handleThread.Start();
            }
        });
        listenerThread.Start();

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

        string line;
        while ((line = Console.ReadLine()) != "exit")
        {
            // Отправляем сообщение всем доступным клиентам
            foreach (var client in clients)
            {
                try
                {
                    NetworkStream stream = client.GetStream();
                    byte[] messageBytes = Encoding.ASCII.GetBytes(line);
                    stream.Write(messageBytes, 0, messageBytes.Length);
                }
                catch (Exception e)
                {
                    // Обработка ошибок
                }
            }
        }

        listenerThread.Abort();
    }
}