using Microsoft.EntityFrameworkCore;
using ReMeal.Application.Interfaces;
using ReMeal.Domain.Entities;
using ReMeal.Domain.Enums;
using ReMeal.Infrastructure.Data;

namespace ReMeal.Infrastructure.Repositories;

public class FoodLotRepository : IFoodLotRepository
{
    private readonly AppDbContext _dbContext;

    public FoodLotRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<FoodLot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.FoodLots
            .Include(x => x.FoodPoint)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<List<FoodLot>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.FoodLots
            .Include(x => x.FoodPoint)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<FoodLot>> GetByFoodPointIdAsync(
        Guid foodPointId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.FoodLots
            .Where(x => x.FoodPointId == foodPointId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<FoodLot>> GetCandidatesForExpirationAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return _dbContext.FoodLots
            .Where(x =>
                x.Status == LotStatus.Active &&
                x.PickupDeadline <= now)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FoodLot lot, CancellationToken cancellationToken = default)
    {
        await _dbContext.FoodLots.AddAsync(lot, cancellationToken);
    }

    public Task UpdateAsync(FoodLot lot, CancellationToken cancellationToken = default)
    {
        _dbContext.FoodLots.Update(lot);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FoodLot lot, CancellationToken cancellationToken = default)
    {
        _dbContext.FoodLots.Remove(lot);
        return Task.CompletedTask;
    }
}
