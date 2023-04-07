using System;
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
        var clients = new List<TcpClient>();

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

                // Создаем новый поток для обработки сообщения
                Thread handleThread = new Thread(() =>
                {
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                    string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Regex regex = new Regex(@"Клиент подключился с портом (\d+)");
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
                            Console.WriteLine($"Ошибка подключения к порту {newClientPort}: {e.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }

                    client.Close();
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
                NetworkStream stream = client.GetStream();
                byte[] messageBytes = Encoding.ASCII.GetBytes("Клиент подключился с портом " + port);
                stream.Write(messageBytes, 0, messageBytes.Length);
                clients.Add(client);
            }
            catch (Exception e)
            {
                // Обработка ошибок
                Console.WriteLine($"Ошибка подключения к порту {remotePort}: {e.Message}");
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
                    // Читаем ответ от клиента
                    byte[] buffer = new byte[client.ReceiveBufferSize];
                    int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(response);
                }
                catch (Exception e)
                {
                    // Обработка ошибок
                }
            }
        }
    }
}
                