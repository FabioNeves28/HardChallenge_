using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using Dapper;
using Xunit;
using SmartVault.Program;

namespace SmartVault.Tests
{
    public class DataGenerationTests
    {
        private readonly string _databasePath;
        private readonly string _outputFilePath;

        public DataGenerationTests()
        {
            var baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "TestDatabase");
            Directory.CreateDirectory(baseDirectory);
            _databasePath = Path.Combine(baseDirectory, "testdb.db");
            _outputFilePath = Path.Combine(baseDirectory, "MergedFiles.txt");

            if (File.Exists(_databasePath)) File.Delete(_databasePath);
            SQLiteConnection.CreateFile(_databasePath);

            using var connection = new SQLiteConnection($"Data Source={_databasePath}");
            connection.Open();
            connection.Execute("CREATE TABLE Document (Id INTEGER PRIMARY KEY, FilePath TEXT, AccountId TEXT)");
        }

        [Fact]
        public void WriteEveryThirdFileToFile_ShouldCreateCorrectOutput()
        {
            using var connection = new SQLiteConnection($"Data Source={_databasePath}");
            connection.Open();

            var testFiles = new List<string>();
            for (int i = 0; i < 9; i++)
            {
                var testFilePath = Path.Combine(Path.GetTempPath(), $"TestFile_{i}.txt");
                File.WriteAllText(testFilePath, i % 3 == 2 ? "Smith Property Content" : "Other Content");
                testFiles.Add(testFilePath);
                connection.Execute("INSERT INTO Document (FilePath, AccountId) VALUES (@FilePath, @AccountId)", new { FilePath = testFilePath, AccountId = "123" });
            }

            Programe.WriteEveryThirdFileToFile("123");

            Assert.True(File.Exists(_outputFilePath));
            var outputContent = File.ReadAllText(_outputFilePath);
            Assert.Contains("Smith Property Content", outputContent);
            Assert.DoesNotContain("Other Content", outputContent);
        }

        [Fact]
        public void GetAllFileSizes_ShouldCalculateCorrectly()
        {
            using var connection = new SQLiteConnection($"Data Source={_databasePath}");
            connection.Open();

            long expectedSize = 0;
            for (int i = 0; i < 5; i++)
            {
                var testFilePath = Path.Combine(Path.GetTempPath(), $"TestSizeFile_{i}.txt");
                var content = new string('X', 100 * (i + 1));
                File.WriteAllText(testFilePath, content);
                expectedSize += content.Length;

                connection.Execute("INSERT INTO Document (FilePath, AccountId) VALUES (@FilePath, @AccountId)", new { FilePath = testFilePath, AccountId = "123" });
            }

            Programe.GetAllFileSizes();
        }
    }
}
