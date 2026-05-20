using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public static class ReMealDatabaseInitializer
    {
        public static void EnsureSchema(ReMealDbContext dbContext)
        {
            DataAccessGuard.Execute(() =>
            {
                dbContext.Database.EnsureCreated();

                dbContext.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS Users (
                        Id TEXT NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
                        Login TEXT NOT NULL,
                        PasswordHash TEXT NOT NULL,
                        FullName TEXT NOT NULL,
                        Email TEXT NOT NULL,
                        Phone TEXT NOT NULL,
                        AvatarPath TEXT NOT NULL DEFAULT '',
                        Role TEXT NOT NULL DEFAULT 'StudentCustomer'
                    );
                    """);

                EnsureColumn(
                    dbContext,
                    "Users",
                    "AvatarPath",
                    "ALTER TABLE Users ADD COLUMN AvatarPath TEXT NOT NULL DEFAULT '';");

                dbContext.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS FoodPoints (
                        Id TEXT NOT NULL CONSTRAINT PK_FoodPoints PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Address TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        Phone TEXT NOT NULL,
                        OwnerId TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        IsActive INTEGER NOT NULL,
                        CONSTRAINT FK_FoodPoints_Users_OwnerId FOREIGN KEY (OwnerId) REFERENCES Users (Id) ON DELETE CASCADE
                    );
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS FoodLots (
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
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        CONSTRAINT FK_FoodLots_FoodPoints_FoodPointId FOREIGN KEY (FoodPointId) REFERENCES FoodPoints (Id) ON DELETE CASCADE
                    );
                    """);

                dbContext.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Login ON Users (Login);");
                dbContext.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS IX_FoodPoints_OwnerId;");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FoodPoints_OwnerId ON FoodPoints (OwnerId);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FoodLots_FoodPointId ON FoodLots (FoodPointId);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FoodLots_PickupDeadline ON FoodLots (PickupDeadline);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FoodLots_Status ON FoodLots (Status);");
            }, "подготовить схему базы данных");
        }

        private static void EnsureColumn(
            ReMealDbContext dbContext,
            string tableName,
            string columnName,
            string addColumnSql)
        {
            var connection = dbContext.Database.GetDbConnection();
            var shouldClose = connection.State == System.Data.ConnectionState.Closed;

            if (shouldClose)
                connection.Open();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"PRAGMA table_info({tableName});";

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                        return;
                }
            }
            finally
            {
                if (shouldClose)
                    connection.Close();
            }

            dbContext.Database.ExecuteSqlRaw(addColumnSql);
        }
    }
}
