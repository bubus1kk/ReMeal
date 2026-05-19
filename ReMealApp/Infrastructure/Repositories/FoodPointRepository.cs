using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class FoodPointRepository : IFoodPointRepository
    {
        private readonly ReMealDbContext _dbContext;

        public FoodPointRepository(ReMealDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<FoodPoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _dbContext.FoodPoints
                .Include(x => x.Lots)
                .Include(x => x.Owner)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<FoodPoint?> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            return _dbContext.FoodPoints
                .Include(x => x.Lots)
                .Include(x => x.Owner)
                .FirstOrDefaultAsync(x => x.OwnerId == ownerId, cancellationToken);
        }

        public Task<List<FoodPoint>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.FoodPoints
                .Include(x => x.Lots)
                .Include(x => x.Owner)
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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
