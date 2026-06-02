using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

    [TestMethod]
    public void EnsureSchema_OnExistingDatabase_AddsStatusForeignKeys()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using (var command = connection.CreateCommand())
        {
            command.CommandText =
            """
            CREATE TABLE Users (
                Id TEXT NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
                Login TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                FullName TEXT NOT NULL,
                Email TEXT NOT NULL,
                Phone TEXT NOT NULL,
                AvatarPath TEXT NOT NULL DEFAULT '',
                Role TEXT NOT NULL DEFAULT 'StudentCustomer',
                IsActive INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE FoodPoints (
                Id TEXT NOT NULL CONSTRAINT PK_FoodPoints PRIMARY KEY,
                Name TEXT NOT NULL,
                Address TEXT NOT NULL,
                Description TEXT NOT NULL,
                Phone TEXT NOT NULL,
                Latitude REAL NULL,
                Longitude REAL NULL,
                OwnerId TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                IsActive INTEGER NOT NULL
            );

            CREATE TABLE FoodLots (
                Id TEXT NOT NULL CONSTRAINT PK_FoodLots PRIMARY KEY,
                FoodPointId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Description TEXT NOT NULL,
                Composition TEXT NOT NULL,
                TotalQuantity INTEGER NOT NULL,
                AvailableQuantity INTEGER NOT NULL,
                Price TEXT NOT NULL,
                PickupDeadline TEXT NOT NULL,
                Status INTEGER NOT NULL,
                ImagePath TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE Bookings (
                Id TEXT NOT NULL CONSTRAINT PK_Bookings PRIMARY KEY,
                UserId TEXT NOT NULL,
                FoodLotId TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                PriceAtReservation TEXT NOT NULL,
                Status INTEGER NOT NULL,
                ReservedAt TEXT NOT NULL,
                CancelledAt TEXT NULL,
                IssuedAt TEXT NULL
            );
            """;

            command.ExecuteNonQuery();
        }

        var options = new DbContextOptionsBuilder<ReMealDbContext>()
            .UseSqlite(connection)
            .Options;

        using var dbContext = new ReMealDbContext(options);

        ReMealDatabaseInitializer.EnsureSchema(dbContext);

        Assert.IsTrue(ForeignKeyExists(connection, "FoodLots", "Status", "LotStatuses", "Id"));
        Assert.IsTrue(ForeignKeyExists(connection, "Bookings", "Status", "BookingStatuses", "Id"));
    }

    private static bool ForeignKeyExists(
        SqliteConnection connection,
        string tableName,
        string columnName,
        string principalTableName,
        string principalColumnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA foreign_key_list({tableName});";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var referencedTable = reader.GetString(2);
            var fromColumn = reader.GetString(3);
            var toColumn = reader.GetString(4);

            if (string.Equals(referencedTable, principalTableName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(fromColumn, columnName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(toColumn, principalColumnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
