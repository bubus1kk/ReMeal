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
                        Role TEXT NOT NULL DEFAULT 'StudentCustomer',
                        IsActive INTEGER NOT NULL DEFAULT 1
                    );
                    """);

                EnsureColumn(
                    dbContext,
                    "Users",
                    "AvatarPath",
                    "ALTER TABLE Users ADD COLUMN AvatarPath TEXT NOT NULL DEFAULT '';");

                EnsureColumn(
                    dbContext,
                    "Users",
                    "IsActive",
                    "ALTER TABLE Users ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1;");

                dbContext.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS LotStatuses (
                        Id INTEGER NOT NULL CONSTRAINT PK_LotStatuses PRIMARY KEY,
                        Name TEXT NOT NULL
                    );
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    INSERT OR IGNORE INTO LotStatuses (Id, Name)
                    VALUES
                        (0, 'Активный'),
                        (1, 'Распродан'),
                        (2, 'Истек'),
                        (3, 'Отменен');
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS BookingStatuses (
                        Id INTEGER NOT NULL CONSTRAINT PK_BookingStatuses PRIMARY KEY,
                        Name TEXT NOT NULL
                    );
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    INSERT OR IGNORE INTO BookingStatuses (Id, Name)
                    VALUES
                        (0, 'Активное'),
                        (1, 'Отменено'),
                        (2, 'Выдано');
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS FoodPoints (
                        Id TEXT NOT NULL CONSTRAINT PK_FoodPoints PRIMARY KEY,
                        Name TEXT NOT NULL,
                        Address TEXT NOT NULL,
                        Description TEXT NOT NULL,
                        Phone TEXT NOT NULL,
                        Latitude REAL NULL,
                        Longitude REAL NULL,
                        OwnerId TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        IsActive INTEGER NOT NULL,
                        CONSTRAINT FK_FoodPoints_Users_OwnerId FOREIGN KEY (OwnerId) REFERENCES Users (Id) ON DELETE CASCADE
                    );
                    """);

                EnsureColumn(
                    dbContext,
                    "FoodPoints",
                    "Latitude",
                    "ALTER TABLE FoodPoints ADD COLUMN Latitude REAL NULL;");

                EnsureColumn(
                    dbContext,
                    "FoodPoints",
                    "Longitude",
                    "ALTER TABLE FoodPoints ADD COLUMN Longitude REAL NULL;");

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
                        CONSTRAINT FK_FoodLots_FoodPoints_FoodPointId FOREIGN KEY (FoodPointId) REFERENCES FoodPoints (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_FoodLots_LotStatuses_Status FOREIGN KEY (Status) REFERENCES LotStatuses (Id) ON DELETE RESTRICT
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
                    CREATE TABLE IF NOT EXISTS Bookings (
                        Id TEXT NOT NULL CONSTRAINT PK_Bookings PRIMARY KEY,
                        UserId TEXT NOT NULL,
                        FoodLotId TEXT NOT NULL,
                        Quantity INTEGER NOT NULL,
                        PriceAtReservation TEXT NOT NULL,
                        Status INTEGER NOT NULL,
                        ReservedAt TEXT NOT NULL,
                        CancelledAt TEXT NULL,
                        IssuedAt TEXT NULL,
                        CONSTRAINT FK_Bookings_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_Bookings_FoodLots_FoodLotId FOREIGN KEY (FoodLotId) REFERENCES FoodLots (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_Bookings_BookingStatuses_Status FOREIGN KEY (Status) REFERENCES BookingStatuses (Id) ON DELETE RESTRICT
                    );
                    """);

                EnsureColumn(
                    dbContext,
                    "Bookings",
                    "PriceAtReservation",
                    "ALTER TABLE Bookings ADD COLUMN PriceAtReservation TEXT NOT NULL DEFAULT '0';");

                EnsureColumn(
                    dbContext,
                    "Bookings",
                    "ReservedAt",
                    "ALTER TABLE Bookings ADD COLUMN ReservedAt TEXT NOT NULL DEFAULT '0001-01-01T00:00:00.0000000Z';");

                EnsureColumn(
                    dbContext,
                    "Bookings",
                    "CancelledAt",
                    "ALTER TABLE Bookings ADD COLUMN CancelledAt TEXT NULL;");

                EnsureColumn(
                    dbContext,
                    "Bookings",
                    "IssuedAt",
                    "ALTER TABLE Bookings ADD COLUMN IssuedAt TEXT NULL;");

                if (ColumnExists(dbContext, "Bookings", "BookingDate"))
                {
                    dbContext.Database.ExecuteSqlRaw("""
                        UPDATE Bookings
                        SET ReservedAt = BookingDate
                        WHERE ReservedAt = '0001-01-01T00:00:00.0000000Z'
                            OR ReservedAt IS NULL;
                        """);
                }

                dbContext.Database.ExecuteSqlRaw("""
                    UPDATE Bookings
                    SET PriceAtReservation = COALESCE((
                        SELECT FoodLots.Price
                        FROM FoodLots
                        WHERE FoodLots.Id = Bookings.FoodLotId
                    ), PriceAtReservation)
                    WHERE PriceAtReservation = '0'
                        OR PriceAtReservation = 0;
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    UPDATE Bookings
                    SET
                        Status = 1,
                        CancelledAt = COALESCE(CancelledAt, ReservedAt)
                    WHERE Status = 3;
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    UPDATE FoodLots
                    SET Status = 0
                    WHERE Status NOT IN (0, 1, 2, 3);
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    UPDATE Bookings
                    SET Status = 0
                    WHERE Status NOT IN (0, 1, 2);
                    """);

                EnsureStatusForeignKeyConstraints(dbContext);

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
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Bookings_UserId ON Bookings (UserId);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Bookings_FoodLotId ON Bookings (FoodLotId);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Bookings_Status ON Bookings (Status);");
                dbContext.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Bookings_ReservedAt ON Bookings (ReservedAt);");
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

        private static void EnsureStatusForeignKeyConstraints(ReMealDbContext dbContext)
        {
            var rebuiltFoodLots = false;

            if (!ForeignKeyExists(dbContext, "FoodLots", "Status", "LotStatuses", "Id"))
            {
                RebuildFoodLotsTable(dbContext);
                rebuiltFoodLots = true;
            }

            if (rebuiltFoodLots || !ForeignKeyExists(dbContext, "Bookings", "Status", "BookingStatuses", "Id"))
                RebuildBookingsTable(dbContext);
        }

        private static bool ForeignKeyExists(
            ReMealDbContext dbContext,
            string tableName,
            string columnName,
            string principalTableName,
            string principalColumnName)
        {
            var connection = dbContext.Database.GetDbConnection();
            var shouldClose = connection.State == System.Data.ConnectionState.Closed;

            if (shouldClose)
                connection.Open();

            try
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
            }
            finally
            {
                if (shouldClose)
                    connection.Close();
            }

            return false;
        }

        private static void RebuildFoodLotsTable(ReMealDbContext dbContext)
        {
            dbContext.Database.ExecuteSqlRaw("PRAGMA foreign_keys=OFF;");

            try
            {
                dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS __FoodLots_rebuild;");

                dbContext.Database.ExecuteSqlRaw("""
                    CREATE TABLE __FoodLots_rebuild (
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
                        CONSTRAINT FK_FoodLots_FoodPoints_FoodPointId FOREIGN KEY (FoodPointId) REFERENCES FoodPoints (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_FoodLots_LotStatuses_Status FOREIGN KEY (Status) REFERENCES LotStatuses (Id) ON DELETE RESTRICT
                    );
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    INSERT INTO __FoodLots_rebuild (
                        Id,
                        FoodPointId,
                        Title,
                        Description,
                        Composition,
                        TotalQuantity,
                        AvailableQuantity,
                        Price,
                        PickupDeadline,
                        Status,
                        ImagePath,
                        CreatedAt,
                        UpdatedAt)
                    SELECT
                        Id,
                        FoodPointId,
                        Title,
                        Description,
                        Composition,
                        TotalQuantity,
                        AvailableQuantity,
                        Price,
                        PickupDeadline,
                        Status,
                        ImagePath,
                        CreatedAt,
                        UpdatedAt
                    FROM FoodLots;
                    """);

                dbContext.Database.ExecuteSqlRaw("DROP TABLE FoodLots;");
                dbContext.Database.ExecuteSqlRaw("ALTER TABLE __FoodLots_rebuild RENAME TO FoodLots;");
            }
            finally
            {
                dbContext.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");
            }
        }

        private static void RebuildBookingsTable(ReMealDbContext dbContext)
        {
            dbContext.Database.ExecuteSqlRaw("PRAGMA foreign_keys=OFF;");

            try
            {
                dbContext.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS __Bookings_rebuild;");

                dbContext.Database.ExecuteSqlRaw("""
                    CREATE TABLE __Bookings_rebuild (
                        Id TEXT NOT NULL CONSTRAINT PK_Bookings PRIMARY KEY,
                        UserId TEXT NOT NULL,
                        FoodLotId TEXT NOT NULL,
                        Quantity INTEGER NOT NULL,
                        PriceAtReservation TEXT NOT NULL,
                        Status INTEGER NOT NULL,
                        ReservedAt TEXT NOT NULL,
                        CancelledAt TEXT NULL,
                        IssuedAt TEXT NULL,
                        CONSTRAINT FK_Bookings_Users_UserId FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_Bookings_FoodLots_FoodLotId FOREIGN KEY (FoodLotId) REFERENCES FoodLots (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_Bookings_BookingStatuses_Status FOREIGN KEY (Status) REFERENCES BookingStatuses (Id) ON DELETE RESTRICT
                    );
                    """);

                dbContext.Database.ExecuteSqlRaw("""
                    INSERT INTO __Bookings_rebuild (
                        Id,
                        UserId,
                        FoodLotId,
                        Quantity,
                        PriceAtReservation,
                        Status,
                        ReservedAt,
                        CancelledAt,
                        IssuedAt)
                    SELECT
                        Id,
                        UserId,
                        FoodLotId,
                        Quantity,
                        PriceAtReservation,
                        Status,
                        ReservedAt,
                        CancelledAt,
                        IssuedAt
                    FROM Bookings;
                    """);

                dbContext.Database.ExecuteSqlRaw("DROP TABLE Bookings;");
                dbContext.Database.ExecuteSqlRaw("ALTER TABLE __Bookings_rebuild RENAME TO Bookings;");
            }
            finally
            {
                dbContext.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");
            }
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
