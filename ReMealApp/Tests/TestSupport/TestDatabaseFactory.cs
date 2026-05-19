using Microsoft.Data.Sqlite;

namespace Tests.TestSupport;

public static class TestDatabaseFactory
{
    public static SqliteConnection CreateInMemory()
    {
        var connection = new SqliteConnection("Data Source=:memory:");

        connection.Open();

        return connection;
    }
}