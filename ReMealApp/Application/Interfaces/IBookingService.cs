using Application.DTOs.Booking;

namespace Application.Interfaces;

public interface IBookingService
{
    Task<BookingDto> BookLotAsync(Guid foodLotId, int quantity, CancellationToken cancellationToken = default);

    Task CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task ConfirmBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<List<BookingDto>> GetCurrentUserBookingsAsync(CancellationToken cancellationToken = default);

    Task<List<BookingDto>> GetCurrentPartnerBookingsAsync(CancellationToken cancellationToken = default);
}
