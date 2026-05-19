using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class FoodLotRepository : IFoodLotRepository
    {
        private readonly ReMealDbContext _dbContext;

        public FoodLotRepository(ReMealDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<FoodLot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _dbContext.FoodLots
                .Include(x => x.FoodPoint)
                .ThenInclude(x => x!.Owner)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<List<FoodLot>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.FoodLots
                .Include(x => x.FoodPoint)
                .ThenInclude(x => x!.Owner)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public Task<List<FoodLot>> GetByFoodPointIdAsync(
            Guid foodPointId,
            CancellationToken cancellationToken = default)
        {
            return _dbContext.FoodLots
                .Include(x => x.FoodPoint)
                .ThenInclude(x => x!.Owner)
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

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
