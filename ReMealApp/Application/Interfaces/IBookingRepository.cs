using Domain.Entities;

namespace Application.Interfaces;

public interface IBookingRepository
{
    Task AddAsync(Booking booking, CancellationToken cancellationToken = default);

    Task<Booking?> GetByIdWithDetailsAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<List<Booking>> GetUserBookingsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<List<Booking>> GetPartnerBookingsAsync(Guid partnerId, CancellationToken cancellationToken = default);

    Task<int> CountActiveUserBookingsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<FoodLot?> GetLotByIdAsync(Guid foodLotId, CancellationToken cancellationToken = default);

    Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default);

    Task UpdateLotAsync(FoodLot lot, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
