using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly ReMealDbContext _context;

    public BookingRepository(ReMealDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);

        await _context.SaveChangesAsync();
    }

    public async Task<Booking?> GetByIdAsync(Guid bookingId)
    {
        return await _context.Bookings
            .Include(x => x.User)
            .Include(x => x.FoodLot)
            .FirstOrDefaultAsync(x => x.Id == bookingId);
    }

    public async Task<List<Booking>> GetUserBookingsAsync(Guid userId)
    {
        return await _context.Bookings
            .Include(x => x.FoodLot)
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetPartnerBookingsAsync(Guid partnerId)
    {
        return await _context.Bookings
            .Include(x => x.User)
            .Include(x => x.FoodLot)
                .ThenInclude(x => x.FoodPoint)
            .Where(x => x.FoodLot.FoodPoint.OwnerId == partnerId)
            .ToListAsync();
    }

    public async Task UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);

        await _context.SaveChangesAsync();
    }

    public async Task<FoodLot?> GetLotByIdAsync(Guid foodLotId)
    {
        return await _context.FoodLots
            .FirstOrDefaultAsync(x => x.Id == foodLotId);
    }

    public async Task UpdateLotAsync(FoodLot lot)
    {
        _context.FoodLots.Update(lot);

        await _context.SaveChangesAsync();
    }

}