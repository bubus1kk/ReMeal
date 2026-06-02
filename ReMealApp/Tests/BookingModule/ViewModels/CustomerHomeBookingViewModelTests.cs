using Domain.Enums;
using ReMealApp.ViewModels.Shell;
using Tests.TestSupport;

namespace Tests.BookingModule.ViewModels;

[TestClass]
public sealed class CustomerHomeBookingViewModelTests
{
    [TestMethod]
    public async Task BookLotCommand_WhenBookingSucceeds_DoesNotShowSuccessNotification()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
        var customer = await database.AddUserAsync(UserRole.StudentCustomer);
        var foodPoint = await database.AddFoodPointAsync(partner);
        await database.AddLotAsync(foodPoint, totalQuantity: 2);
        database.Auth.SetCurrentUser(customer);
        var viewModel = CreateViewModel(database);

        await viewModel.LoadAsync();
        await viewModel.BookLotCommand.ExecuteAsync(viewModel.AvailableLots.Single());

        Assert.AreEqual(string.Empty, viewModel.StatusMessage);
        Assert.IsFalse(viewModel.IsBookingLimitDialogOpen);
        Assert.HasCount(1, viewModel.ActiveBookings);
    }

    [TestMethod]
    public async Task BookLotCommand_WhenActiveBookingLimitReached_OpensLimitDialog()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
        var customer = await database.AddUserAsync(UserRole.StudentCustomer);
        var foodPoint = await database.AddFoodPointAsync(partner);
        var lot = await database.AddLotAsync(foodPoint, totalQuantity: 10);
        database.Auth.SetCurrentUser(customer);

        for (var index = 0; index < 5; index++)
            await database.BookingService.BookLotAsync(lot.Id, 1);

        var viewModel = CreateViewModel(database);

        await viewModel.LoadAsync();
        await viewModel.BookLotCommand.ExecuteAsync(viewModel.AvailableLots.Single());

        Assert.IsTrue(viewModel.IsBookingLimitDialogOpen);
        Assert.AreEqual("У вас уже есть 5 активных бронирований.", viewModel.BookingLimitMessage);
        Assert.AreEqual(string.Empty, viewModel.StatusMessage);

        viewModel.CloseBookingLimitDialogCommand.Execute(null);

        Assert.IsFalse(viewModel.IsBookingLimitDialogOpen);
    }

    private static CustomerHomeViewModel CreateViewModel(SqliteTestDatabase database)
    {
        return new CustomerHomeViewModel(
            database.Auth,
            database.LotService,
            database.BookingService,
            _ => { },
            _ => Task.CompletedTask);
    }
}
