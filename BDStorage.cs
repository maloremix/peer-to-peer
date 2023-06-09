﻿using Microsoft.EntityFrameworkCore;
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

    public class Joke
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Joke> Jokes { get; set; }
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

    class BDStorage : IStorage
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

                var existingMessage = context.Messages
                    .Include(m => m.Sender)
                    .FirstOrDefault(m => m.Date == date && m.Text == message && m.Sender.Username == sender);
                if (existingMessage == null)
                {
                    var newMessage = new Message
                    {
                        Date = date,
                        Text = message,
                        SenderId = sourceUser.Id
                    };

                    context.Messages.Add(newMessage);
                    context.SaveChanges();
                }
                lastId++;
            }
        }
        public void ReadHistory(int count = 100, DateTime? before = null, DateTime? after = null)
        {
            using (var context = new ApplicationDbContext())
            {
                IQueryable<Message> messagesQuery = context.Messages
                    .Include(m => m.Sender)
                    .OrderBy(m => m.Date);

                if (before.HasValue)
                {
                    messagesQuery = messagesQuery.Where(m => m.Date < before.Value);
                }

                if (after.HasValue)
                {
                    messagesQuery = messagesQuery.Where(m => m.Date > after.Value);
                }

                messagesQuery = messagesQuery.Take(Math.Min(count, 1000));

                var listMessages = messagesQuery.ToList();

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
            ReadHistory(1000);
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
        public void WriteIntoStorage(string message)
        {
            string patternLoginMessage = @"^\[(.*)\]\s*(\w+)\[\d+\]:\s*""(.*)""$";
            Match matchLoginMessage = Regex.Match(message, patternLoginMessage);
            if (matchLoginMessage.Success)
            {
                DateTime date = DateTime.Parse(matchLoginMessage.Groups[1].Value);
                string login = matchLoginMessage.Groups[2].Value;
                string messageToBd = matchLoginMessage.Groups[3].Value;
                AddMessage(login, messageToBd, date);
            }
        }

        public int GetLastId()
        {
            return lastId;
        }

        public void DeleteMessageById(int id)
        {
            using (var context = new ApplicationDbContext())
            {
                var messageToDelete = context.Messages.FirstOrDefault(m => m.Id == id);
                if (messageToDelete != null)
                {
                    context.Messages.Remove(messageToDelete);
                    context.SaveChanges();
                }
            }
        }

        public string GetRandomJoke()
        {
            using (var db = new ApplicationDbContext())
            {
                var count = db.Jokes.Count();
                var random = new Random();
                var joke = db.Jokes.Skip(random.Next(0, count)).FirstOrDefault();
                return joke.Text;
            }
        }
    }
}
