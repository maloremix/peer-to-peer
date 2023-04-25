using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp7
{
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    public class Storage : IStorage
    {
        private BinaryWriter Writer { get; set; }
        private int lastId = 1;
        private string Login { get; set; }

        public void SetLoginStorage(string login)
        {
            Login = login;
            ReadHistory(Login);
            Writer = new BinaryWriter(new FileStream($"{Login}.dat", FileMode.Append, FileAccess.Write, FileShare.None));
        }

        public int GetLastId()
        {
            return lastId;
        }

        private void ReadHistory(string login)
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
                        lastId = int.Parse(match.Groups[1].Value) + 1;
                    }
                }
            }
        }


        public void WriteIntoStorage(string message)
        {
            if (Writer == null)
            {
                throw new InvalidOperationException("Writer is not initialized.");
            }
            Writer.Write(message);
            Writer.Flush();
            lastId++;
        }

        public void DeleteMessageById(int id)
        {
            if (Writer == null)
            {
                throw new InvalidOperationException("Writer is not initialized.");
            }
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

        public void ReadHistory(int count, DateTime? before, DateTime? after)
        {
            throw new NotImplementedException();
        }
        public string GetRandomJoke()
        {
            throw new NotImplementedException();
        }
    }
}
