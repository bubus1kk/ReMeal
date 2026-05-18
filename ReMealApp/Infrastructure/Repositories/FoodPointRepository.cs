using Microsoft.EntityFrameworkCore;
using ReMeal.Application.Interfaces;
using ReMeal.Domain.Entities;
using ReMeal.Infrastructure.Data;

namespace ReMeal.Infrastructure.Repositories;

public class FoodPointRepository : IFoodPointRepository
{
    private readonly AppDbContext _dbContext;

    public FoodPointRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<FoodPoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.FoodPoints
            .Include(x => x.Lots)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<List<FoodPoint>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.FoodPoints
            .Include(x => x.Lots)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FoodPoint foodPoint, CancellationToken cancellationToken = default)
    {
        await _dbContext.FoodPoints.AddAsync(foodPoint, cancellationToken);
    }

    public Task UpdateAsync(FoodPoint foodPoint, CancellationToken cancellationToken = default)
    {
        _dbContext.FoodPoints.Update(foodPoint);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FoodPoint foodPoint, CancellationToken cancellationToken = default)
    {
        _dbContext.FoodPoints.Remove(foodPoint);
        return Task.CompletedTask;
    }
}
