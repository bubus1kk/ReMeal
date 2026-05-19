using Application.Interfaces;
using Application.Services;
using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public sealed class ReMealUserModule
    {
        public ReMealUserModule(
            IAuthService authService,
            IUserProfileService userProfileService,
            IFoodPointService foodPointService,
            ILotService lotService)
        {
            AuthService = authService;
            UserProfileService = userProfileService;
            FoodPointService = foodPointService;
            LotService = lotService;
        }

        public IAuthService AuthService { get; }

        public IUserProfileService UserProfileService { get; }

        public IFoodPointService FoodPointService { get; }

        public ILotService LotService { get; }

        public static ReMealUserModule CreateDefault()
        {
            return DataAccessGuard.Execute(() =>
            {
                var options = new DbContextOptionsBuilder<ReMealDbContext>()
                    .UseSqlite($"Data Source={ReMealDatabasePath.GetDefaultPath()}")
                    .Options;

                var dbContext = new ReMealDbContext(options);
                ReMealDatabaseInitializer.EnsureSchema(dbContext);

                IUserRepository userRepository = new UserRepository(dbContext);
                IPasswordHasher passwordHasher = new PasswordHasher();
                IRememberedUserStore rememberedUserStore = RememberedUserStore.CreateDefault();
                IAuthService authService = new AuthService(userRepository, passwordHasher, rememberedUserStore);
                IUserProfileService userProfileService = new UserProfileService(authService, userRepository);

                IFoodPointRepository foodPointRepository = new FoodPointRepository(dbContext);
                IFoodLotRepository foodLotRepository = new FoodLotRepository(dbContext);
                IFoodPointService foodPointService = new FoodPointService(foodPointRepository, authService);
                ILotService lotService = new LotService(foodPointRepository, foodLotRepository, authService);

                return new ReMealUserModule(authService, userProfileService, foodPointService, lotService);
            }, "инициализировать доступ к данным приложения");
        }
    }
}
