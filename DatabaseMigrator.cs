using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp7
{
    class DatabaseMigrator
    {
        public void Migrate()
        {
            var datFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dat");

            var messageRegex = new Regex(@"\[(.*?)\]\s(.*?)\[.*?\]:\s""(.*?)""");

            foreach (var datFile in datFiles)
            {
                using (var fileStream = File.Open(datFile, FileMode.Open))
                {
                    // Используем BufferedStream для ускорения чтения данных
                    using (var bufferedStream = new BufferedStream(fileStream))
                    {
                        using (var binaryReader = new BinaryReader(bufferedStream))
                        {
                            // Читаем данные из бинарного файла
                            while (binaryReader.PeekChar() > -1)
                            {
                                var line = binaryReader.ReadString();
                                var match = messageRegex.Match(line);
                                if (match.Success)
                                {
                                    var date = DateTime.Parse(match.Groups[1].Value);
                                    var sender = match.Groups[2].Value;
                                    var text = match.Groups[3].Value;

                                    using (var context = new ApplicationDbContext())
                                    {
                                        // проверяем, существует ли сообщение с такими же полями в базе данных
                                        var existingMessage = context.Messages
                                            .Include(m => m.Sender)
                                            .FirstOrDefault(m => m.Date == date && m.Text == text && m.Sender.Username == sender);

                                        // проверяем, существует ли пользователь с таким именем в базе данных
                                        var existingUser = context.Users.FirstOrDefault(u => u.Username == sender);

                                        if (existingMessage == null)
                                        {
                                            // добавляем сообщение в базу данных
                                            var newMessage = new Message
                                            {
                                                Date = date,
                                                Text = text,
                                                Sender = existingUser ?? new User { Username = sender }
                                            };

                                            context.Messages.Add(newMessage);
                                            context.SaveChanges();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
