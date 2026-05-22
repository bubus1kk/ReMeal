using Application.DTOs.Booking;

namespace Application.Interfaces;

public interface IBookingService
{
    Task<bool> BookLotAsync(Guid userId, Guid foodLotId, int quantity);

    Task<bool> CancelBookingAsync(Guid bookingId);

    Task<bool> ConfirmBookingAsync(Guid bookingId);

    Task<bool> RejectBookingAsync(Guid bookingId);

    Task<List<BookingDto>> GetUserBookingsAsync(Guid userId);

    Task<List<BookingDto>> GetPartnerBookingsAsync(Guid partnerId);
}