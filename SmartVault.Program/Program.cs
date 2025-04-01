using System.IO;
using System;
using System.Data.SQLite;
using Dapper;
using System.Linq;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Threading.Tasks;
using System.Threading;
namespace SmartVault.Program
{
    partial class Program
    {
        static void Main(string[] args)
        {
            string accountId;
            if (args.Length == 0)
            {
                Console.Write("Enter Account ID: ");
                accountId = Console.ReadLine();
            }
            else
            {
                accountId = args[0];
            }

            if (string.IsNullOrWhiteSpace(accountId))
            {
                Console.WriteLine("Invalid Account ID. Exiting...");
                return;
            }

            WriteEveryThirdFileToFile(accountId);
            GetAllFileSizes();
        }


        private static void GetAllFileSizes()
        {
            long totalSize = 0;
            int totalFiles = 0;
            var pathCache = new ConcurrentDictionary<string, long>();

            var baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..");
            var databaseFilePath = Path.Combine(baseDirectory, "testdb.db");

            using var connection = new SQLiteConnection($"Data Source={databaseFilePath}");
            connection.Open();

            var filePaths = connection.Query<string>(
                "SELECT FilePath FROM Document");

            Parallel.ForEach(filePaths, path =>
            {
                Interlocked.Increment(ref totalFiles);

                if (pathCache.TryGetValue(path, out long currentSize))
                {
                    Interlocked.Add(ref totalSize, currentSize);
                }
                else
                {
                    long size = new FileInfo(path).Length;
                    pathCache[path] = size;
                    Interlocked.Add(ref totalSize, size);
                }
            });

            Console.WriteLine($"Total Size of {totalFiles} files: " + totalSize);
        }



        private static void WriteEveryThirdFileToFile(string accountId)
        {
            var baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..");
            var databaseFilePath = Path.Combine(baseDirectory, "testdb.db");
            var outputFilePath = Path.Combine(baseDirectory, "MergedFiles.txt");

            if (File.Exists(outputFilePath)) File.Delete(outputFilePath);

            using var connection = new SQLiteConnection($"Data Source={databaseFilePath}");
            var filePaths = connection.Query<string>(
                "SELECT FilePath FROM Document WHERE AccountId = @AccountId",
                new { AccountId = accountId });

            using (var writer = new StreamWriter(outputFilePath))
            {
                for (int i = 2; i < filePaths.Count(); i += 3)
                {
                    var content = File.ReadAllText(filePaths.ElementAt(i));
                    if (content.Contains("Smith Property"))
                    {
                        writer.WriteLine(content);
                    }
                }
            }

            Console.WriteLine($"Arquivo gerado: {outputFilePath}");
        }

    }
}