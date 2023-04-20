using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.Remoting.Contexts;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace ConsoleApp7
{

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
    }

    public class Message
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Text { get; set; }
        public int SenderId { get; set; }
        public User Sender { get; set; }
    }


    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Server=localhost;Port=5432;Database=FirstBD;User Id=postgres;Password=postgres;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>()
                .HasKey(m => m.Id);

            modelBuilder.Entity<Message>()
                .Property(m => m.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    class BDStorage : IBDStorage
    {
        private int lastId = 1;
        public BDStorage()
        {
            using (var context = new ApplicationDbContext())
            {
                context.Database.Migrate();
            }
        }
        public void AddMessage(string sender, string message, DateTime date)
        {
            using (var context = new ApplicationDbContext())
            {
                var sourceUser = context.Users.FirstOrDefault(u => u.Username == sender);

                if (sourceUser == null)
                {
                    throw new ArgumentException("Invalid source user.");
                }

                var newMessage = new Message
                {
                    Date = date,
                    Text = message,
                    SenderId = sourceUser.Id
                };

                context.Messages.Add(newMessage);
                context.SaveChanges();
                lastId++;
            }
        }
        public void WriteIntoConsole(string message)
        {
            Console.WriteLine(message);
            lastId++;
        }
        public void ReadHistory()
        {
            using (var context = new ApplicationDbContext())
            {
                var listMessages = context.Messages
                    .Include(m => m.Sender)
                    .OrderBy(m => m.Date)
                    .ToList();

                lastId = context.Messages.Count() + 1;
                var messageConsoleId = 1;
                foreach (var message in listMessages)
                {
                    Console.WriteLine($"[{message.Date:yyyy-MM-dd HH:mm:ss.ff}] {message.Sender.Username}[{messageConsoleId}]: \"{message.Text}\"");
                    messageConsoleId++;
                }
            }
        }

        public void SetLoginStorage(string login)
        {
            using (var context = new ApplicationDbContext())
            {
                if (context.Users.Any(u => u.Username == login))
                {
                    return; // запись уже есть в таблице, ничего не делаем
                }

                var user = new User
                {
                    Username = login
                };

                context.Users.Add(user);
                context.SaveChanges();
            }
        }

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

        public int GetLastId()
        {
            return lastId;
        }
    }
}
