using Dapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SmartVault.Library;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SmartVault.DataGeneration
{
    partial class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

            var baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..");
            var databaseFilePath = Path.Combine(baseDirectory, "testdb.db");
            SQLiteConnection.CreateFile(databaseFilePath);

            var dbPath = databaseFilePath;
            EnsureTestDocumentExists(Path.Combine(baseDirectory, "TestDoc.txt"));

            using var connection = new SQLiteConnection(string.Format(configuration["ConnectionStrings:DefaultConnection"], dbPath));
            connection.Open();
            var files = Directory.GetFiles(@"..\..\..\..\BusinessObjectSchema").Take(3);
            foreach (var file in files)
            {
                var serializer = new XmlSerializer(typeof(BusinessObject));
                using var reader = new StreamReader(file);
                var businessObject = serializer.Deserialize(reader) as BusinessObject;
                await connection.ExecuteAsync(businessObject?.Script);
            }

            using var transaction = connection.BeginTransaction();
            var userInsert = "INSERT INTO User (Id, FirstName, LastName, DateOfBirth, AccountId, Username, Password, CreatedOn) VALUES (@Id, @FirstName, @LastName, @DateOfBirth, @AccountId, @Username, @Password, @CreatedOn)";
            var accountInsert = "INSERT INTO Account (Id, Name, CreatedOn) VALUES (@Id, @Name, @CreatedOn)";
            var documentInsert = "INSERT INTO Document (Id, Name, FilePath, Length, AccountId, CreatedOn) VALUES (@Id, @Name, @FilePath, @Length, @AccountId, @CreatedOn)";

            var random = new Random();
            var start = new DateTime(1985, 1, 1);
            var range = (DateTime.Today - start).Days;
            var users = new List<object>();
            var accounts = new List<object>();
            var documents = new List<object>();
            var documentPath = Path.GetFullPath(Path.Combine(baseDirectory, "TestDoc.txt"));
            var documentLength = new FileInfo(documentPath).Length;

            for (int i = 0, docId = 0; i < 100; i++)
            {
                var randomDate = start.AddDays(random.Next(range)).ToString("yyyy-MM-dd");
                var createdOn = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                users.Add(new { Id = i, FirstName = $"FName{i}", LastName = $"LName{i}", DateOfBirth = randomDate, AccountId = i, Username = $"UserName-{i}", Password = "e10adc3949ba59abbe56e057f20f883e", CreatedOn = createdOn });
                accounts.Add(new { Id = i, Name = $"Account{i}", CreatedOn = createdOn });

                for (int d = 0; d < 10000; d++, docId++)
                {
                    documents.Add(new { Id = docId, Name = $"Document{i}-{d}.txt", FilePath = documentPath, Length = documentLength, AccountId = i, CreatedOn = createdOn });
                }
            }

            connection.Execute(userInsert, users, transaction);
            connection.Execute(accountInsert, accounts, transaction);
            connection.Execute(documentInsert, documents, transaction);
            transaction.Commit();

            Console.WriteLine($"AccountCount: {JsonConvert.SerializeObject(connection.QuerySingle<int>("SELECT COUNT(*) FROM Account"))}");
            Console.WriteLine($"DocumentCount: {JsonConvert.SerializeObject(connection.QuerySingle<int>("SELECT COUNT(*) FROM Document"))}");
            Console.WriteLine($"UserCount: {JsonConvert.SerializeObject(connection.QuerySingle<int>("SELECT COUNT(*) FROM User"))}");
        }

        static void EnsureTestDocumentExists(string path)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, "This is a test document\n".PadRight(5000, 'X'));
            }
        }
    }
}
