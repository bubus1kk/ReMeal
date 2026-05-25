using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly ReMealDbContext _context;

    public BookingRepository(ReMealDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        await DataAccessGuard.ExecuteAsync(
            async () => await _context.Bookings.AddAsync(booking, cancellationToken),
            "добавить бронирование");
    }

    public Task<Booking?> GetByIdWithDetailsAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        return DataAccessGuard.ExecuteAsync(
            () => _context.Bookings
                .Include(x => x.User)
                .Include(x => x.FoodLot)
                    .ThenInclude(x => x!.FoodPoint)
                .FirstOrDefaultAsync(x => x.Id == bookingId, cancellationToken),
            "получить бронирование");
    }

    public Task<List<Booking>> GetUserBookingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return DataAccessGuard.ExecuteAsync(
            () => _context.Bookings
                .Include(x => x.User)
                .Include(x => x.FoodLot)
                    .ThenInclude(x => x!.FoodPoint)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.ReservedAt)
                .ToListAsync(cancellationToken),
            "получить бронирования пользователя");
    }

    public Task<List<Booking>> GetPartnerBookingsAsync(
        Guid partnerId,
        CancellationToken cancellationToken = default)
    {
        return DataAccessGuard.ExecuteAsync(
            () => _context.Bookings
                .Include(x => x.User)
                .Include(x => x.FoodLot)
                    .ThenInclude(x => x!.FoodPoint)
                .Where(x => x.FoodLot != null &&
                    x.FoodLot.FoodPoint != null &&
                    x.FoodLot.FoodPoint.OwnerId == partnerId)
                .OrderByDescending(x => x.ReservedAt)
                .ToListAsync(cancellationToken),
                "получить бронирования партнера");
    }

    public Task<List<Booking>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return DataAccessGuard.ExecuteAsync(
            () => _context.Bookings
                .Include(x => x.User)
                .Include(x => x.FoodLot)
                    .ThenInclude(x => x!.FoodPoint)
                .OrderByDescending(x => x.ReservedAt)
                .ToListAsync(cancellationToken),
            "получить список бронирований");
    }

    public Task<int> CountActiveUserBookingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return DataAccessGuard.ExecuteAsync(
            () => _context.Bookings.CountAsync(
                x => x.UserId == userId && x.Status == BookingStatus.Active,
                cancellationToken),
            "посчитать активные бронирования пользователя");
    }

    public Task<FoodLot?> GetLotByIdAsync(
        Guid foodLotId,
        CancellationToken cancellationToken = default)
    {
        return DataAccessGuard.ExecuteAsync(
            () => _context.FoodLots
                .Include(x => x.FoodPoint)
                .FirstOrDefaultAsync(x => x.Id == foodLotId, cancellationToken),
            "получить лот для бронирования");
    }

    public Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        DataAccessGuard.Execute(
            () =>
            {
                if (_context.Entry(booking).State == EntityState.Detached)
                    _context.Bookings.Update(booking);
            },
            "обновить бронирование");

        return Task.CompletedTask;
    }

    public Task UpdateLotAsync(FoodLot lot, CancellationToken cancellationToken = default)
    {
        DataAccessGuard.Execute(
            () =>
            {
                if (_context.Entry(lot).State == EntityState.Detached)
                    _context.FoodLots.Update(lot);
            },
            "обновить лот бронирования");

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return DataAccessGuard.ExecuteAsync(
            async () =>
            {
                await _context.SaveChangesAsync(cancellationToken);
                _context.ChangeTracker.Clear();
            },
            "сохранить изменения бронирования");
    }
}
