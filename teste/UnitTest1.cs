using Xunit;

namespace teste
{
    public class UnitTest1
    {
        [Fact]
        public void ShouldWriteEveryThirdFileToFile()
        {
            Program.WriteEveryThirdFileToFile("1");
            Assert.True(File.Exists("MergedFiles.txt"));
        }

        [Fact]
        public void ShouldCalculateCorrectFileSizes()
        {
            Program.GetAllFileSizes();
            using var connection = new SQLiteConnection("Data Source=testdb.sqlite;");
            var fileSize = connection.ExecuteScalar<long>("SELECT SUM(Length) FROM Document");
            Assert.True(fileSize > 0);
        }
    }
}
