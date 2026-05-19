using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.TestSupport
{
    internal sealed class SqliteTestDatabase : IAsyncDisposable
    {
        private int _userNumber;

        private SqliteTestDatabase(SqliteConnection connection, ReMealDbContext dbContext)
        {
            Connection = connection;
            DbContext = dbContext;
            Auth = new TestAuthService();
            UserRepository = new UserRepository(dbContext);
            FoodPointRepository = new FoodPointRepository(dbContext);
            FoodLotRepository = new FoodLotRepository(dbContext);
            FoodPointService = new FoodPointService(FoodPointRepository, Auth);
            LotService = new LotService(FoodPointRepository, FoodLotRepository, Auth);
        }

        public SqliteConnection Connection { get; }

        public ReMealDbContext DbContext { get; }

        public TestAuthService Auth { get; }

        public UserRepository UserRepository { get; }

        public IFoodPointRepository FoodPointRepository { get; }

        public IFoodLotRepository FoodLotRepository { get; }

        public FoodPointService FoodPointService { get; }

        public LotService LotService { get; }

        public static async Task<SqliteTestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ReMealDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new ReMealDbContext(options);
            ReMealDatabaseInitializer.EnsureSchema(dbContext);

            return new SqliteTestDatabase(connection, dbContext);
        }

        public async Task<User> AddUserAsync(UserRole role, string? login = null)
        {
            _userNumber++;

            var user = new User
            {
                Login = login ?? $"user-{_userNumber}",
                PasswordHash = "hash",
                FullName = $"Test User {_userNumber}",
                Email = $"user-{_userNumber}@example.test",
                Phone = "+10000000000",
                Role = role
            };

            await UserRepository.AddAsync(user);
            await UserRepository.SaveChangesAsync();

            return user;
        }

        public async Task<FoodPoint> AddFoodPointAsync(User owner, string? name = null)
        {
            var foodPoint = new FoodPoint(
                name ?? $"Food Point {owner.Login}",
                "Campus street, 1",
                "Fresh food point",
                "+10000000000",
                owner.Id);

            await FoodPointRepository.AddAsync(foodPoint);
            await FoodPointRepository.SaveChangesAsync();

            return foodPoint;
        }

        public async Task<FoodLot> AddLotAsync(
            FoodPoint foodPoint,
            string? title = null,
            int totalQuantity = 5,
            decimal price = 99,
            DateTime? pickupDeadline = null)
        {
            var lot = new FoodLot(
                foodPoint.Id,
                title ?? $"Lot {Guid.NewGuid():N}",
                "Dinner set",
                "Soup, salad, dessert",
                totalQuantity,
                price,
                pickupDeadline ?? DateTime.UtcNow.AddHours(3));

            await FoodLotRepository.AddAsync(lot);
            await FoodLotRepository.SaveChangesAsync();

            return lot;
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
