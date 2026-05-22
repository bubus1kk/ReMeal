using Application.DTOs.Booking;
using Application.Interfaces;
using Domain.Enums;
using ReMealApp.ViewModels.Booking;

namespace Tests.BookingModule.ViewModels
{
    [TestClass]
    public sealed class BookingViewModelTests
    {
        [TestMethod]
        public async Task MyBookingsViewModel_LoadAsync_LoadsCurrentUserBookings()
        {
            var service = new FakeBookingService
            {
                UserBookings =
                {
                    new BookingDto
                    {
                        Id = Guid.NewGuid(),
                        LotTitle = "Lunch",
                        FoodPointName = "Cafe",
                        Quantity = 2,
                        PriceAtReservation = 100,
                        Status = BookingStatus.Active
                    }
                }
            };
            var viewModel = new MyBookingsViewModel(service);

            await viewModel.LoadAsync();

            Assert.HasCount(1, viewModel.Bookings);
            Assert.AreEqual("Lunch", viewModel.Bookings[0].LotTitle);
            Assert.AreEqual("Бронирований: 1", viewModel.StatusMessage);
        }

        [TestMethod]
        public async Task MyBookingsViewModel_CancelBookingCommand_CancelsBookingAndReloads()
        {
            var bookingId = Guid.NewGuid();
            var service = new FakeBookingService
            {
                UserBookings =
                {
                    new BookingDto
                    {
                        Id = bookingId,
                        LotTitle = "Dinner",
                        Status = BookingStatus.Active
                    }
                }
            };
            var viewModel = new MyBookingsViewModel(service);

            await viewModel.CancelBookingCommand.ExecuteAsync(bookingId);

            Assert.AreEqual(bookingId, service.CancelledBookingId);
            Assert.AreEqual(1, service.UserBookingsLoadCount);
            Assert.AreEqual("Бронирование отменено.", viewModel.StatusMessage);
        }

        [TestMethod]
        public async Task MyBookingsViewModel_WhenServiceThrows_ShowsFormattedError()
        {
            var service = new FakeBookingService
            {
                UserBookingsException = new InvalidOperationException("boom")
            };
            var viewModel = new MyBookingsViewModel(service);

            await viewModel.LoadAsync();

            Assert.IsGreaterThan(0, viewModel.StatusMessage.Length);
            Assert.IsEmpty(viewModel.Bookings);
        }

        [TestMethod]
        public async Task PartnerBookingsViewModel_LoadAsync_LoadsBookingsAndCalculatesAnalytics()
        {
            var service = new FakeBookingService
            {
                PartnerBookings =
                {
                    new BookingDto
                    {
                        Id = Guid.NewGuid(),
                        Quantity = 3,
                        Status = BookingStatus.Issued
                    },
                    new BookingDto
                    {
                        Id = Guid.NewGuid(),
                        Quantity = 2,
                        Status = BookingStatus.Cancelled
                    },
                    new BookingDto
                    {
                        Id = Guid.NewGuid(),
                        Quantity = 1,
                        Status = BookingStatus.Active
                    }
                }
            };
            var viewModel = new PartnerBookingsViewModel(service);

            await viewModel.LoadAsync();

            Assert.HasCount(3, viewModel.Bookings);
            Assert.AreEqual(1, viewModel.IssuedBookingsCount);
            Assert.AreEqual(3, viewModel.SavedPortions);
            Assert.AreEqual(1.2m, viewModel.PreventedWasteKg);
            Assert.AreEqual(1, viewModel.CancelledBookingsCount);
        }

        [TestMethod]
        public async Task PartnerBookingsViewModel_ConfirmBookingCommand_ConfirmsBookingAndReloads()
        {
            var bookingId = Guid.NewGuid();
            var service = new FakeBookingService
            {
                PartnerBookings =
                {
                    new BookingDto
                    {
                        Id = bookingId,
                        Status = BookingStatus.Active
                    }
                }
            };
            var viewModel = new PartnerBookingsViewModel(service);

            await viewModel.ConfirmBookingCommand.ExecuteAsync(bookingId);

            Assert.AreEqual(bookingId, service.ConfirmedBookingId);
            Assert.AreEqual(1, service.PartnerBookingsLoadCount);
            Assert.AreEqual("Выдача подтверждена.", viewModel.StatusMessage);
        }

        private sealed class FakeBookingService : IBookingService
        {
            public List<BookingDto> UserBookings { get; } = new();

            public List<BookingDto> PartnerBookings { get; } = new();

            public Exception? UserBookingsException { get; init; }

            public Guid? CancelledBookingId { get; private set; }

            public Guid? ConfirmedBookingId { get; private set; }

            public int UserBookingsLoadCount { get; private set; }

            public int PartnerBookingsLoadCount { get; private set; }

            public Task<BookingDto> BookLotAsync(
                Guid foodLotId,
                int quantity,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
            {
                CancelledBookingId = bookingId;
                return Task.CompletedTask;
            }

            public Task ConfirmBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
            {
                ConfirmedBookingId = bookingId;
                return Task.CompletedTask;
            }

            public Task<List<BookingDto>> GetCurrentUserBookingsAsync(CancellationToken cancellationToken = default)
            {
                UserBookingsLoadCount++;

                if (UserBookingsException is not null)
                    throw UserBookingsException;

                return Task.FromResult(UserBookings.ToList());
            }

            public Task<List<BookingDto>> GetCurrentPartnerBookingsAsync(CancellationToken cancellationToken = default)
            {
                PartnerBookingsLoadCount++;
                return Task.FromResult(PartnerBookings.ToList());
            }
        }
    }
}
