using Domain.Entities;

namespace Application.Interfaces;

public interface IBookingRepository
{
    Task AddAsync(Booking booking);

    Task<Booking?> GetByIdAsync(Guid bookingId);

    Task<List<Booking>> GetUserBookingsAsync(Guid userId);

    Task<List<Booking>> GetPartnerBookingsAsync(Guid partnerId);

    Task UpdateAsync(Booking booking);

    Task<FoodLot?> GetLotByIdAsync(Guid foodLotId);

    Task UpdateLotAsync(FoodLot lot);
}