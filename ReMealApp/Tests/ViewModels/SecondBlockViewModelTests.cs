using Application.Services;
using Domain.Enums;
using ReMealApp.ViewModels.Catalog;
using ReMealApp.ViewModels.Shell;
using Tests.TestSupport;

namespace Tests.ViewModels
{
    [TestClass]
    public sealed class SecondBlockViewModelTests
    {
        [TestMethod]
        public async Task HomeViewModel_WhenCurrentUserIsPartner_ShowsPartnerTabsAndHidesCatalog()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            database.Auth.SetCurrentUser(partner);
            var home = CreateHomeViewModel(database);

            await home.InitializeAsync();

            Assert.IsTrue(home.IsPartner);
            Assert.IsFalse(home.IsCatalogVisible);
        }

        [TestMethod]
        public async Task HomeViewModel_WhenCurrentUserIsCustomer_ShowsCatalogAndHidesPartnerTabs()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var customer = await database.AddUserAsync(UserRole.StudentCustomer);
            database.Auth.SetCurrentUser(customer);
            var home = CreateHomeViewModel(database);

            await home.InitializeAsync();

            Assert.IsFalse(home.IsPartner);
            Assert.IsTrue(home.IsCatalogVisible);
        }

        [TestMethod]
        public async Task CatalogViewModel_LoadAsync_WhenNoLotsAvailable_ShowsEmptyState()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var viewModel = new CatalogViewModel(database.LotService);

            await viewModel.LoadAsync();

            Assert.IsFalse(viewModel.Lots.Any());
            Assert.AreEqual("Сейчас нет доступных наборов.", viewModel.StatusMessage);
        }

        [TestMethod]
        public async Task CatalogViewModel_LoadAsync_LoadsAvailableLotsWithFoodPointData()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner, "Catalog cafe");
            var lot = await database.AddLotAsync(foodPoint, "Catalog lot");
            var viewModel = new CatalogViewModel(database.LotService);

            await viewModel.LoadAsync();
            var result = viewModel.Lots.Single();

            Assert.AreEqual(lot.Id, result.Id);
            Assert.IsNotNull(result.FoodPoint);
            Assert.AreEqual("Catalog cafe", result.FoodPoint!.Name);
            Assert.AreEqual("Доступных наборов: 1", viewModel.StatusMessage);
        }

        [TestMethod]
        public async Task CreateLotViewModel_SaveCommand_UsesSelectedDateAndTime()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            await database.AddFoodPointAsync(partner);
            database.Auth.SetCurrentUser(partner);
            var home = CreateHomeViewModel(database);
            await home.InitializeAsync();

            var localDate = DateTime.Today.AddDays(1);
            var localTime = new TimeSpan(15, 30, 0);
            var expectedUtc = ToUtc(localDate, localTime);

            await home.CreateLot.PrepareCreateAsync();
            home.CreateLot.Title = "Timed lot";
            home.CreateLot.Description = "Dinner";
            home.CreateLot.Composition = "Main course";
            home.CreateLot.TotalQuantity = 3;
            home.CreateLot.Price = 50;
            home.CreateLot.PickupDeadline = new DateTimeOffset(localDate);
            home.CreateLot.PickupTime = localTime;

            await home.CreateLot.SaveCommand.ExecuteAsync(null);

            var lots = await database.LotService.GetCurrentPartnerLotsAsync();
            var result = lots.Single();

            Assert.AreEqual("Timed lot", result.Title);
            Assert.AreEqual(expectedUtc, result.PickupDeadline);
        }

        private static HomeViewModel CreateHomeViewModel(SqliteTestDatabase database)
        {
            var userProfileService = new UserProfileService(database.Auth, database.UserRepository);

            return new HomeViewModel(
                database.Auth,
                userProfileService,
                database.FoodPointService,
                database.LotService,
                () => { });
        }

        private static DateTime ToUtc(DateTime localDate, TimeSpan localTime)
        {
            var localDeadline = localDate.Date + localTime;
            var offset = TimeZoneInfo.Local.GetUtcOffset(localDeadline);
            return new DateTimeOffset(localDeadline, offset).UtcDateTime;
        }
    }
}
