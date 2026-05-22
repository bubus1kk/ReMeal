using Domain.Entities;
using Domain.Enums;
using Tests.TestSupport;

namespace Tests.BookingModule.Services
{
    [TestClass]
    public sealed class BookingServiceTests
    {
        [TestMethod]
        public async Task BookLotAsync_WhenCustomerBooksActiveLot_CreatesBookingAndDecreasesAvailableQuantity()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, customer, lot) = await CreateBookableLotAsync(database, totalQuantity: 5, price: 125);
            database.Auth.SetCurrentUser(customer);

            var booking = await database.BookingService.BookLotAsync(lot.Id, 2);
            var savedLot = await database.FoodLotRepository.GetByIdAsync(lot.Id);
            var savedBooking = await database.BookingRepository.GetByIdWithDetailsAsync(booking.Id);

            Assert.AreEqual(customer.Id, savedBooking?.UserId);
            Assert.AreEqual(lot.Id, savedBooking?.FoodLotId);
            Assert.AreEqual(2, booking.Quantity);
            Assert.AreEqual(125m, booking.PriceAtReservation);
            Assert.AreEqual(BookingStatus.Active, booking.Status);
            Assert.IsNotNull(savedLot);
            Assert.AreEqual(5, savedLot.TotalQuantity);
            Assert.AreEqual(3, savedLot.AvailableQuantity);
            Assert.AreEqual(LotStatus.Active, savedLot.Status);
        }

        [TestMethod]
        public async Task BookLotAsync_WhenQuantityExhaustsLot_MarksLotSoldOut()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, customer, lot) = await CreateBookableLotAsync(database, totalQuantity: 2);
            database.Auth.SetCurrentUser(customer);

            await database.BookingService.BookLotAsync(lot.Id, 2);
            var savedLot = await database.FoodLotRepository.GetByIdAsync(lot.Id);

            Assert.IsNotNull(savedLot);
            Assert.AreEqual(0, savedLot.AvailableQuantity);
            Assert.AreEqual(LotStatus.SoldOut, savedLot.Status);
        }

        [TestMethod]
        public async Task BookLotAsync_UsesCurrentUserAndDoesNotAcceptManualUserId()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, firstCustomer, lot) = await CreateBookableLotAsync(database);
            var secondCustomer = await database.AddUserAsync(UserRole.StudentCustomer);
            database.Auth.SetCurrentUser(secondCustomer);

            var booking = await database.BookingService.BookLotAsync(lot.Id, 1);
            var savedBooking = await database.BookingRepository.GetByIdWithDetailsAsync(booking.Id);

            Assert.AreEqual(secondCustomer.Id, savedBooking?.UserId);
            Assert.AreNotEqual(firstCustomer.Id, savedBooking?.UserId);
        }

        [TestMethod]
        public async Task BookLotAsync_WhenUserIsNotCustomer_ThrowsUnauthorizedAccessException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (partner, _, lot) = await CreateBookableLotAsync(database);
            database.Auth.SetCurrentUser(partner);

            await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
                database.BookingService.BookLotAsync(lot.Id, 1));
        }

        [TestMethod]
        public async Task BookLotAsync_WhenQuantityIsInvalid_ThrowsArgumentOutOfRangeException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, customer, lot) = await CreateBookableLotAsync(database);
            database.Auth.SetCurrentUser(customer);

            await AssertEx.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                database.BookingService.BookLotAsync(lot.Id, 0));
        }

        [TestMethod]
        public async Task BookLotAsync_WhenQuantityExceedsAvailable_ThrowsAndKeepsLotQuantity()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, customer, lot) = await CreateBookableLotAsync(database, totalQuantity: 2);
            database.Auth.SetCurrentUser(customer);

            await AssertEx.ThrowsAsync<InvalidOperationException>(() =>
                database.BookingService.BookLotAsync(lot.Id, 3));

            var savedLot = await database.FoodLotRepository.GetByIdAsync(lot.Id);
            Assert.IsNotNull(savedLot);
            Assert.AreEqual(2, savedLot.AvailableQuantity);
            Assert.AreEqual(LotStatus.Active, savedLot.Status);
        }

        [TestMethod]
        public async Task BookLotAsync_WhenLotIsCancelled_ThrowsInvalidOperationException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, customer, lot) = await CreateBookableLotAsync(database);
            lot.Cancel();
            await database.FoodLotRepository.UpdateAsync(lot);
            await database.FoodLotRepository.SaveChangesAsync();
            database.Auth.SetCurrentUser(customer);

            await AssertEx.ThrowsAsync<InvalidOperationException>(() =>
                database.BookingService.BookLotAsync(lot.Id, 1));
        }

        [TestMethod]
        public async Task BookLotAsync_WhenLotIsExpired_MarksExpiredAndThrowsInvalidOperationException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, customer, lot) = await CreateBookableLotAsync(database);
            await MakeLotExpiredCandidateAsync(database, lot.Id);
            database.Auth.SetCurrentUser(customer);

            await AssertEx.ThrowsAsync<InvalidOperationException>(() =>
                database.BookingService.BookLotAsync(lot.Id, 1));

            var savedLot = await database.FoodLotRepository.GetByIdAsync(lot.Id);
            Assert.IsNotNull(savedLot);
            Assert.AreEqual(LotStatus.Expired, savedLot.Status);
        }

        [TestMethod]
        public async Task BookLotAsync_WhenUserAlreadyHasFiveActiveBookings_ThrowsInvalidOperationException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, customer, lot) = await CreateBookableLotAsync(database, totalQuantity: 6);
            database.Auth.SetCurrentUser(customer);

            for (var index = 0; index < 5; index++)
                await database.BookingService.BookLotAsync(lot.Id, 1);

            await AssertEx.ThrowsAsync<InvalidOperationException>(() =>
                database.BookingService.BookLotAsync(lot.Id, 1));

            var savedLot = await database.FoodLotRepository.GetByIdAsync(lot.Id);
            Assert.IsNotNull(savedLot);
            Assert.AreEqual(1, savedLot.AvailableQuantity);
        }

        [TestMethod]
        public async Task CancelBookingAsync_WhenBookingIsActive_ReturnsAvailableQuantityWithoutChangingTotal()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, customer, lot) = await CreateBookableLotAsync(database, totalQuantity: 3);
            database.Auth.SetCurrentUser(customer);
            var booking = await database.BookingService.BookLotAsync(lot.Id, 3);

            await database.BookingService.CancelBookingAsync(booking.Id);

            var savedLot = await database.FoodLotRepository.GetByIdAsync(lot.Id);
            var savedBooking = await database.BookingRepository.GetByIdWithDetailsAsync(booking.Id);

            Assert.IsNotNull(savedLot);
            Assert.AreEqual(3, savedLot.TotalQuantity);
            Assert.AreEqual(3, savedLot.AvailableQuantity);
            Assert.AreEqual(LotStatus.Active, savedLot.Status);
            Assert.IsNotNull(savedBooking);
            Assert.AreEqual(BookingStatus.Cancelled, savedBooking.Status);
            Assert.IsNotNull(savedBooking.CancelledAt);
        }

        [TestMethod]
        public async Task CancelBookingAsync_WhenBookingBelongsToOtherCustomer_ThrowsUnauthorizedAccessException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, firstCustomer, lot) = await CreateBookableLotAsync(database);
            var secondCustomer = await database.AddUserAsync(UserRole.StudentCustomer);
            database.Auth.SetCurrentUser(firstCustomer);
            var booking = await database.BookingService.BookLotAsync(lot.Id, 1);
            database.Auth.SetCurrentUser(secondCustomer);

            await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
                database.BookingService.CancelBookingAsync(booking.Id));
        }

        [TestMethod]
        public async Task CancelBookingAsync_WhenBookingIsIssued_ThrowsInvalidOperationException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (partner, customer, lot) = await CreateBookableLotAsync(database);
            database.Auth.SetCurrentUser(customer);
            var booking = await database.BookingService.BookLotAsync(lot.Id, 1);
            database.Auth.SetCurrentUser(partner);
            await database.BookingService.ConfirmBookingAsync(booking.Id);
            database.Auth.SetCurrentUser(customer);

            await AssertEx.ThrowsAsync<InvalidOperationException>(() =>
                database.BookingService.CancelBookingAsync(booking.Id));
        }

        [TestMethod]
        public async Task ConfirmBookingAsync_WhenBookingBelongsToPartner_MarksIssuedWithoutChangingLotQuantity()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (partner, customer, lot) = await CreateBookableLotAsync(database, totalQuantity: 4);
            database.Auth.SetCurrentUser(customer);
            var booking = await database.BookingService.BookLotAsync(lot.Id, 2);
            database.Auth.SetCurrentUser(partner);

            await database.BookingService.ConfirmBookingAsync(booking.Id);

            var savedLot = await database.FoodLotRepository.GetByIdAsync(lot.Id);
            var savedBooking = await database.BookingRepository.GetByIdWithDetailsAsync(booking.Id);

            Assert.IsNotNull(savedLot);
            Assert.AreEqual(4, savedLot.TotalQuantity);
            Assert.AreEqual(2, savedLot.AvailableQuantity);
            Assert.IsNotNull(savedBooking);
            Assert.AreEqual(BookingStatus.Issued, savedBooking.Status);
            Assert.IsNotNull(savedBooking.IssuedAt);
        }

        [TestMethod]
        public async Task ConfirmBookingAsync_WhenPartnerDoesNotOwnLot_ThrowsUnauthorizedAccessException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, customer, lot) = await CreateBookableLotAsync(database);
            var otherPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            database.Auth.SetCurrentUser(customer);
            var booking = await database.BookingService.BookLotAsync(lot.Id, 1);
            database.Auth.SetCurrentUser(otherPartner);

            await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
                database.BookingService.ConfirmBookingAsync(booking.Id));
        }

        [TestMethod]
        public async Task ConfirmBookingAsync_WhenBookingIsCancelled_ThrowsInvalidOperationException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (partner, customer, lot) = await CreateBookableLotAsync(database);
            database.Auth.SetCurrentUser(customer);
            var booking = await database.BookingService.BookLotAsync(lot.Id, 1);
            await database.BookingService.CancelBookingAsync(booking.Id);
            database.Auth.SetCurrentUser(partner);

            await AssertEx.ThrowsAsync<InvalidOperationException>(() =>
                database.BookingService.ConfirmBookingAsync(booking.Id));
        }

        [TestMethod]
        public async Task GetCurrentUserBookingsAsync_ReturnsOnlyCurrentCustomerBookings()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, firstCustomer, firstLot) = await CreateBookableLotAsync(database, lotTitle: "First");
            var secondCustomer = await database.AddUserAsync(UserRole.StudentCustomer);
            var foodPoint = firstLot.FoodPoint ?? await database.AddFoodPointAsync(await database.AddUserAsync(UserRole.FoodPointRepresentative));
            var secondLot = await database.AddLotAsync(foodPoint, "Second");
            database.Auth.SetCurrentUser(firstCustomer);
            var ownBooking = await database.BookingService.BookLotAsync(firstLot.Id, 1);
            database.Auth.SetCurrentUser(secondCustomer);
            await database.BookingService.BookLotAsync(secondLot.Id, 1);
            database.Auth.SetCurrentUser(firstCustomer);

            var bookings = await database.BookingService.GetCurrentUserBookingsAsync();

            var result = bookings.Single();
            Assert.AreEqual(ownBooking.Id, result.Id);
            Assert.AreEqual("First", result.LotTitle);
        }

        [TestMethod]
        public async Task GetCurrentPartnerBookingsAsync_ReturnsOnlyBookingsForOwnedLots()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var firstPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var secondPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var customer = await database.AddUserAsync(UserRole.StudentCustomer);
            var firstPoint = await database.AddFoodPointAsync(firstPartner, "First cafe");
            var secondPoint = await database.AddFoodPointAsync(secondPartner, "Second cafe");
            var firstLot = await database.AddLotAsync(firstPoint, "First lot");
            var secondLot = await database.AddLotAsync(secondPoint, "Second lot");
            database.Auth.SetCurrentUser(customer);
            var ownBooking = await database.BookingService.BookLotAsync(firstLot.Id, 1);
            await database.BookingService.BookLotAsync(secondLot.Id, 1);
            database.Auth.SetCurrentUser(firstPartner);

            var bookings = await database.BookingService.GetCurrentPartnerBookingsAsync();

            var result = bookings.Single();
            Assert.AreEqual(ownBooking.Id, result.Id);
            Assert.AreEqual("First lot", result.LotTitle);
            Assert.AreEqual("First cafe", result.FoodPointName);
        }

        [TestMethod]
        public async Task BookingKeepsPriceAtReservation_WhenLotPriceChangesLater()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var (_, customer, lot) = await CreateBookableLotAsync(database, price: 99);
            database.Auth.SetCurrentUser(customer);
            var booking = await database.BookingService.BookLotAsync(lot.Id, 1);

            var savedLot = await database.FoodLotRepository.GetByIdAsync(lot.Id)
                ?? throw new InvalidOperationException("Тестовый лот не найден.");
            savedLot.Update(savedLot.Title, savedLot.Description, savedLot.Composition, 199, savedLot.PickupDeadline);
            await database.FoodLotRepository.UpdateAsync(savedLot);
            await database.FoodLotRepository.SaveChangesAsync();

            var bookings = await database.BookingService.GetCurrentUserBookingsAsync();
            var result = bookings.Single(x => x.Id == booking.Id);

            Assert.AreEqual(99m, result.PriceAtReservation);
            Assert.AreEqual(99m, result.TotalPrice);
        }

        private static async Task<(User Partner, User Customer, FoodLot Lot)> CreateBookableLotAsync(
            SqliteTestDatabase database,
            int totalQuantity = 5,
            decimal price = 99,
            string lotTitle = "Dinner box")
        {
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var customer = await database.AddUserAsync(UserRole.StudentCustomer);
            var foodPoint = await database.AddFoodPointAsync(partner, "Booking cafe");
            var lot = await database.AddLotAsync(foodPoint, lotTitle, totalQuantity, price);

            return (partner, customer, lot);
        }

        private static async Task MakeLotExpiredCandidateAsync(SqliteTestDatabase database, Guid lotId)
        {
            var lot = await database.FoodLotRepository.GetByIdAsync(lotId)
                ?? throw new InvalidOperationException("Тестовый лот не найден.");

            SetPrivateProperty(lot, nameof(FoodLot.PickupDeadline), DateTime.UtcNow.AddHours(-1));
            SetPrivateProperty(lot, nameof(FoodLot.Status), LotStatus.Active);

            await database.FoodLotRepository.UpdateAsync(lot);
            await database.FoodLotRepository.SaveChangesAsync();
            database.DbContext.ChangeTracker.Clear();
        }

        private static void SetPrivateProperty<TValue>(FoodLot lot, string propertyName, TValue value)
        {
            var property = typeof(FoodLot).GetProperty(propertyName)
                ?? throw new InvalidOperationException($"Свойство {propertyName} не найдено.");

            property.SetValue(lot, value);
        }
    }
}
