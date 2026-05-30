using Application.Services;
using Domain.Enums;
using ReMealApp.ViewModels.Analytics;
using ReMealApp.ViewModels.Shell;
using Tests.TestSupport;

namespace Tests.AnalyticsModule.ViewModels;

[TestClass]
public sealed class HomeAnalyticsNavigationTests
{
    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        AvaloniaTestApplication.EnsureInitialized();
    }

    [TestMethod]
    public async Task ShowAnalyticsCommand_LoadsAnalyticsDashboardAndSelectsAnalyticsSection()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
        var customer = await database.AddUserAsync(UserRole.StudentCustomer);
        var foodPoint = await database.AddFoodPointAsync(partner, "Analytics cafe");
        var lot = await database.AddLotAsync(foodPoint, "Analytics lot", totalQuantity: 5);

        database.Auth.SetCurrentUser(customer);
        var booking = await database.BookingService.BookLotAsync(lot.Id, 3);
        database.Auth.SetCurrentUser(partner);
        await database.BookingService.ConfirmBookingAsync(booking.Id);

        var home = CreateHomeViewModel(database);

        await home.InitializeAsync();
        await home.ShowAnalyticsCommand.ExecuteAsync(null);

        Assert.AreEqual(HomeViewModel.AnalyticsSection, home.SelectedSectionKey);
        Assert.IsTrue(home.IsAnalyticsSelected);
        Assert.IsInstanceOfType<PartnerAnalyticsViewModel>(home.CurrentSectionViewModel);
        Assert.AreSame(home.Analytics, home.CurrentSectionViewModel);
        Assert.AreEqual(1, home.Analytics.IssuedBookingsCount);
        Assert.AreEqual(3, home.Analytics.SavedPortionsCount);
        Assert.AreEqual("Analytics lot", home.Analytics.MostPopularLot);
        Assert.AreEqual("Analytics cafe", home.Analytics.BestFoodPoint);
    }

    private static HomeViewModel CreateHomeViewModel(
        SqliteTestDatabase database)
    {
        var userProfileService =
            new UserProfileService(
                database.Auth,
                database.UserRepository);

        var profileStatisticsService =
            new ProfileStatisticsService(
                database.Auth,
                database.UserRepository,
                database.FoodPointRepository,
                database.FoodLotRepository);

        return new HomeViewModel(
            database.Auth,
            userProfileService,
            database.FoodPointService,
            database.LotService,
            database.BookingService,
            profileStatisticsService,
            database.PartnerAnalyticsService,
            _ => { },
            () => { });
    }
}
