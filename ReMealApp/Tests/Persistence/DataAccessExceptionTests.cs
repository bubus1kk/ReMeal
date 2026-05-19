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
