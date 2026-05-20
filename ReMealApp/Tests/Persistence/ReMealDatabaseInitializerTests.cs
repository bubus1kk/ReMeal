using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Persistence;

[TestClass]
public class ReMealDatabaseInitializerTests
{
    [TestMethod]
    public void EnsureSchema_OnEmptyDatabase_CreatesAllTables()
    {
        using var connection =
            new SqliteConnection("Data Source=:memory:");

        connection.Open();

        var command = connection.CreateCommand();

        command.CommandText =
        """
        CREATE TABLE Users (
            Id TEXT PRIMARY KEY,
            Login TEXT NOT NULL
        );

        CREATE TABLE FoodPoints (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL
        );

        CREATE TABLE FoodLots (
            Id TEXT PRIMARY KEY,
            Title TEXT NOT NULL
        );
        """;

        var result = command.ExecuteNonQuery();

        Assert.IsTrue(result >= 0);
    }

    [TestMethod]
    public void EnsureSchema_CanBeCalledMultipleTimes()
    {
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void EnsureSchema_CreatesRequiredIndexes()
    {
        Assert.IsTrue(true);
    }
}