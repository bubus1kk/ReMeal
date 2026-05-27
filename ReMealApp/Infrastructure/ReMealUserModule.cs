using Application.Exceptions;
using Application.Interfaces;
using Application.Services;
using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public sealed class ReMealUserModule
    {
        public ReMealUserModule(
            IAuthService authService,
            IUserProfileService userProfileService,
            IFoodPointService foodPointService,
            ILotService lotService,
            IBookingService bookingService,
            IAdminService adminService,
            IProfileStatisticsService profileStatisticsService,
            IGeocodingService geocodingService,
            IMapService mapService,
            IPartnerAnalyticsService partnerAnalyticsService)
        {
            AuthService = authService;
            UserProfileService = userProfileService;
            FoodPointService = foodPointService;
            LotService = lotService;
            BookingService = bookingService;
            AdminService = adminService;
            ProfileStatisticsService = profileStatisticsService;
            GeocodingService = geocodingService;
            MapService = mapService;
            PartnerAnalyticsService = partnerAnalyticsService;
        }

        public IAuthService AuthService { get; }

        public IUserProfileService UserProfileService { get; }

        public IFoodPointService FoodPointService { get; }

        public ILotService LotService { get; }

        public IBookingService BookingService { get; }

        public IAdminService AdminService { get; }

        public IProfileStatisticsService ProfileStatisticsService { get; }

        public IGeocodingService GeocodingService { get; }

        public IMapService MapService { get; }

        public IPartnerAnalyticsService PartnerAnalyticsService { get; }

        public static ReMealUserModule CreateDefault()
        {
            return DataAccessGuard.Execute(() =>
            {
                var databasePath = ReMealDatabasePath.GetDefaultPath();

                var options = new DbContextOptionsBuilder<ReMealDbContext>()
                    .UseSqlite($"Data Source={databasePath}")
                    .Options;

                var dbContext = new ReMealDbContext(options);

                try
                {
                    ReMealDatabaseInitializer.EnsureSchema(dbContext);
                }
                catch (DataAccessException ex)
                    when (ReMealDatabaseRecovery.CanRecover(ex))
                {
                    dbContext.Dispose();

                    ReMealDatabaseRecovery.MoveDatabaseToBackup(databasePath);

                    dbContext = new ReMealDbContext(options);

                    ReMealDatabaseInitializer.EnsureSchema(dbContext);
                }

                IUserRepository userRepository = new UserRepository(dbContext);
                IPasswordHasher passwordHasher = new PasswordHasher();
                IRememberedUserStore rememberedUserStore = RememberedUserStore.CreateDefault();
                IAuthService authService = new AuthService(userRepository, passwordHasher, rememberedUserStore);
                IUserProfileService userProfileService = new UserProfileService(authService, userRepository);

                IFoodPointRepository foodPointRepository = new FoodPointRepository(dbContext);
                IFoodLotRepository foodLotRepository = new FoodLotRepository(dbContext);
                IFoodPointService foodPointService = new FoodPointService(foodPointRepository, authService);
                ILotService lotService = new LotService(foodPointRepository, foodLotRepository, authService);
                IBookingRepository bookingRepository = new BookingRepository(dbContext);
                IBookingService bookingService = new BookingService(bookingRepository, authService);
                IAdminService adminService = new AdminService(
                    authService,
                    userRepository,
                    foodPointRepository,
                    foodLotRepository,
                    bookingRepository);
                IProfileStatisticsService profileStatisticsService = new ProfileStatisticsService(
                    authService,
                    userRepository,
                    foodPointRepository,
                    foodLotRepository);
                IGeocodingService geocodingService = new NominatimGeocodingService(new HttpClient());
                IMapService mapService = new MapService(lotService, geocodingService);
                IPartnerAnalyticsService partnerAnalyticsService =
                    new PartnerAnalyticsService(
                        foodLotRepository,
                        authService,
                        bookingRepository);

                return new ReMealUserModule(
                    authService,
                    userProfileService,
                    foodPointService,
                    lotService,
                    bookingService,
                    adminService,
                    profileStatisticsService,
                    geocodingService,
                    mapService,
                    partnerAnalyticsService);
            }, "инициализировать доступ к данным приложения");
        }
    }
}
