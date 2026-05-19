using Application.Exceptions;
using Domain.Entities;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Persistence
{
    [TestClass]
    public sealed class DataAccessExceptionTests
    {
        [TestMethod]
        public async Task EnsureSchema_WhenDatabaseAlreadyHasAnyTable_CreatesMissingUsersTable()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ReMealDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new ReMealDbContext(options);
            await dbContext.Database.ExecuteSqlRawAsync("CREATE TABLE ExistingTable (Id TEXT NOT NULL PRIMARY KEY);");

            ReMealDatabaseInitializer.EnsureSchema(dbContext);

            var repository = new UserRepository(dbContext);
            await repository.AddAsync(CreateUser("new-user"));
            await repository.SaveChangesAsync();

            var user = await repository.GetByLoginAsync("new-user");

            Assert.IsNotNull(user);
            Assert.AreEqual("new-user", user.Login);
        }

        [TestMethod]
        public async Task Recovery_WhenDatabaseFileIsInvalid_MovesFileAndAllowsNewSchema()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "RecoveryTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var databasePath = Path.Combine(directory, "remeal.db");
            await File.WriteAllTextAsync(databasePath, "not a sqlite database");

            var options = new DbContextOptionsBuilder<ReMealDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            await using (var brokenContext = new ReMealDbContext(options))
            {
                try
                {
                    ReMealDatabaseInitializer.EnsureSchema(brokenContext);
                    Assert.Fail("Ожидалось исключение DataAccessException.");
                }
                catch (DataAccessException exception)
                {
                    Assert.IsTrue(ReMealDatabaseRecovery.CanRecover(exception));
                }
            }

            var backupPath = ReMealDatabaseRecovery.MoveDatabaseToBackup(databasePath);

            Assert.IsFalse(File.Exists(databasePath));
            Assert.IsTrue(File.Exists(backupPath));

            await using var recoveredContext = new ReMealDbContext(options);
            ReMealDatabaseInitializer.EnsureSchema(recoveredContext);

            var repository = new UserRepository(recoveredContext);
            await repository.AddAsync(CreateUser("after-recovery"));
            await repository.SaveChangesAsync();

            Assert.IsNotNull(await repository.GetByLoginAsync("after-recovery"));
        }

        [TestMethod]
        public async Task SaveChangesAsync_WhenLoginIsDuplicated_ThrowsDataAccessException()
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ReMealDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new ReMealDbContext(options);
            ReMealDatabaseInitializer.EnsureSchema(dbContext);

            var repository = new UserRepository(dbContext);
            await repository.AddAsync(CreateUser("same-login"));
            await repository.SaveChangesAsync();

            await repository.AddAsync(CreateUser("same-login"));

            try
            {
                await repository.SaveChangesAsync();
                Assert.Fail("Ожидалось исключение DataAccessException.");
            }
            catch (DataAccessException exception)
            {
                StringAssert.Contains(exception.Message, "Нарушено ограничение данных");
            }
        }

        private static User CreateUser(string login)
        {
            return new User
            {
                Login = login,
                PasswordHash = "hash",
                FullName = "Test User",
                Email = $"{login}@example.test",
                Phone = "+10000000000"
            };
        }
    }
}
