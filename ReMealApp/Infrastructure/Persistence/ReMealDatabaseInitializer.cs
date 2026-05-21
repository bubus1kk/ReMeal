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
                        ImagePath TEXT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        CONSTRAINT FK_FoodLots_FoodPoints_FoodPointId FOREIGN KEY (FoodPointId) REFERENCES FoodPoints (Id) ON DELETE CASCADE
                    );
                    """);

                EnsureColumn(
                    dbContext,
                    "FoodLots",
                    "ImagePath",
                    "ALTER TABLE FoodLots ADD COLUMN ImagePath TEXT NULL;");

                dbContext.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS LotComponents (
                        Id TEXT NOT NULL CONSTRAINT PK_LotComponents PRIMARY KEY,
                        LotId TEXT NOT NULL,
                        Name TEXT NOT NULL,
                        Quantity INTEGER NOT NULL,
                        Unit TEXT NOT NULL DEFAULT 'шт',
                        Composition TEXT NULL,
                        ImagePath TEXT NULL,
                        SortOrder INTEGER NOT NULL,
                        CONSTRAINT CK_LotComponents_Quantity_Positive CHECK (Quantity > 0),
                        CONSTRAINT FK_LotComponents_FoodLots_LotId FOREIGN KEY (LotId) REFERENCES FoodLots (Id) ON DELETE CASCADE
                    );
                    """);

                EnsureColumn(
                    dbContext,
                    "LotComponents",
                    "Unit",
                    "ALTER TABLE LotComponents ADD COLUMN Unit TEXT NOT NULL DEFAULT 'шт';");

                EnsureColumn(
                    dbContext,
                    "LotComponents",
                    "ImagePath",
                    "ALTER TABLE LotComponents ADD COLUMN ImagePath TEXT NULL;");

                DropColumnIfExists(dbContext, "LotComponents", "Details");

                dbContext.Database.ExecuteSqlRaw("""
                    INSERT INTO LotComponents (Id, LotId, Name, Quantity, Unit, Composition, ImagePath, SortOrder)
                    SELECT
                        lower(hex(randomblob(4))) || '-' ||
                        lower(hex(randomblob(2))) || '-' ||
                        lower(hex(randomblob(2))) || '-' ||
                        lower(hex(randomblob(2))) || '-' ||
                        lower(hex(randomblob(6))),
                        FoodLots.Id,
                        'Состав набора',
                        1,
                        'шт',
                        FoodLots.Composition,
                        NULL,
                        0
                    FROM FoodLots
                    WHERE trim(FoodLots.Composition) <> ''
                        AND NOT EXISTS (
                            SELECT 1
                            FROM LotComponents
                            WHERE LotComponents.LotId = FoodLots.Id
                        );
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    UPDATE FoodLots
                    SET Composition = COALESCE((
                        SELECT group_concat(DisplayText, '; ')
                        FROM (
                            SELECT
                                Name || ' — ' || Quantity || CASE
                                    WHEN trim(Unit) = '' THEN ''
                                    ELSE ' ' || Unit
                                END AS DisplayText
                            FROM LotComponents
                            WHERE LotComponents.LotId = FoodLots.Id
                            ORDER BY SortOrder, Name
                        )
                    ), '')
                    WHERE EXISTS (
                        SELECT 1
                        FROM LotComponents
                        WHERE LotComponents.LotId = FoodLots.Id
                    );
                    """);

                dbContext.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Login ON Users (Login);");
                dbContext.Database.ExecuteSqlRaw("DROP INDEX IF EXISTS IX_FoodPoints_OwnerId;");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FoodPoints_OwnerId ON FoodPoints (OwnerId);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FoodLots_FoodPointId ON FoodLots (FoodPointId);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FoodLots_PickupDeadline ON FoodLots (PickupDeadline);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FoodLots_Status ON FoodLots (Status);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_LotComponents_LotId ON LotComponents (LotId);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_LotComponents_LotId_SortOrder ON LotComponents (LotId, SortOrder);");
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

        private static void DropColumnIfExists(
            ReMealDbContext dbContext,
            string tableName,
            string columnName)
        {
            if (!ColumnExists(dbContext, tableName, columnName))
                return;

            dbContext.Database.ExecuteSqlRaw("ALTER TABLE " + tableName + " DROP COLUMN " + columnName + ";");
        }

        private static bool ColumnExists(
            ReMealDbContext dbContext,
            string tableName,
            string columnName)
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
                        return true;
                }
            }
            finally
            {
                if (shouldClose)
                    connection.Close();
            }

            return false;
        }
    }
}
