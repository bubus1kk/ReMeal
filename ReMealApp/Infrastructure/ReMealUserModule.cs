using Application.Interfaces;
using Application.Services;
using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public sealed class ReMealUserModule
    {
        public ReMealUserModule(IAuthService authService, IUserProfileService userProfileService)
        {
            AuthService = authService;
            UserProfileService = userProfileService;
        }

        public IAuthService AuthService { get; }

        public IUserProfileService UserProfileService { get; }

        public static ReMealUserModule CreateDefault()
        {
            var options = new DbContextOptionsBuilder<ReMealDbContext>()
                .UseSqlite($"Data Source={ReMealDatabasePath.GetDefaultPath()}")
                .Options;

            var dbContext = new ReMealDbContext(options);
            dbContext.Database.EnsureCreated();

            IUserRepository userRepository = new UserRepository(dbContext);
            IPasswordHasher passwordHasher = new PasswordHasher();
            IRememberedUserStore rememberedUserStore = RememberedUserStore.CreateDefault();
            IAuthService authService = new AuthService(userRepository, passwordHasher, rememberedUserStore);
            IUserProfileService userProfileService = new UserProfileService(authService, userRepository);

            return new ReMealUserModule(authService, userProfileService);
        }
    }
}
