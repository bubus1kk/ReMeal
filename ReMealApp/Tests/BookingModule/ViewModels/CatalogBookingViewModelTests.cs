using Domain.Enums;
using ReMealApp.ViewModels.Catalog;
using Tests.TestSupport;

namespace Tests.BookingModule.ViewModels
{
    [TestClass]
    public sealed class CatalogBookingViewModelTests
    {
        [TestMethod]
        public async Task BookLotCommand_WhenBookingSucceeds_CreatesBookingAndRefreshesCatalog()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var customer = await database.AddUserAsync(UserRole.StudentCustomer);
            var foodPoint = await database.AddFoodPointAsync(partner, "Catalog cafe");
            var lot = await database.AddLotAsync(foodPoint, "Catalog lot", totalQuantity: 2);
            database.Auth.SetCurrentUser(customer);
            var viewModel = new CatalogViewModel(database.LotService, database.BookingService);

            await viewModel.BookLotCommand.ExecuteAsync(lot.Id);

            var savedLot = await database.FoodLotRepository.GetByIdAsync(lot.Id);
            var bookings = await database.BookingRepository.GetUserBookingsAsync(customer.Id);

            Assert.AreEqual("Бронирование создано.", viewModel.StatusMessage);
            Assert.IsNotNull(savedLot);
            Assert.AreEqual(1, savedLot.AvailableQuantity);
            Assert.HasCount(1, bookings);
            Assert.HasCount(1, viewModel.Lots);
        }

        [TestMethod]
        public async Task BookLotCommand_WhenBookingFails_ShowsErrorMessage()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var customer = await database.AddUserAsync(UserRole.StudentCustomer);
            var foodPoint = await database.AddFoodPointAsync(partner);
            var lot = await database.AddLotAsync(foodPoint, totalQuantity: 1);
            lot.Cancel();
            await database.FoodLotRepository.UpdateAsync(lot);
            await database.FoodLotRepository.SaveChangesAsync();
            database.Auth.SetCurrentUser(customer);
            var viewModel = new CatalogViewModel(database.LotService, database.BookingService);

            await viewModel.BookLotCommand.ExecuteAsync(lot.Id);

            Assert.IsGreaterThan(0, viewModel.StatusMessage.Length);
            Assert.AreNotEqual("Бронирование создано.", viewModel.StatusMessage);
        }
    }
}
