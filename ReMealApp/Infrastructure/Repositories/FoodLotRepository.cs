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
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.FoodLots
                    .Include(x => x.FoodPoint)
                    .ThenInclude(x => x!.Owner)
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
                "получить лот по id");
        }

        public Task<List<FoodLot>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.FoodLots
                    .Include(x => x.FoodPoint)
                    .ThenInclude(x => x!.Owner)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync(cancellationToken),
                "получить список лотов");
        }

        public Task<List<FoodLot>> GetByFoodPointIdAsync(
            Guid foodPointId,
            CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.FoodLots
                    .Include(x => x.FoodPoint)
                    .ThenInclude(x => x!.Owner)
                    .Where(x => x.FoodPointId == foodPointId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync(cancellationToken),
                "получить лоты точки питания");
        }

        public Task<List<FoodLot>> GetCandidatesForExpirationAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.FoodLots
                    .Where(x =>
                        x.Status == LotStatus.Active &&
                        x.PickupDeadline <= now)
                    .ToListAsync(cancellationToken),
                "получить истекшие лоты");
        }

        public async Task AddAsync(FoodLot lot, CancellationToken cancellationToken = default)
        {
            await DataAccessGuard.ExecuteAsync(
                async () => await _dbContext.FoodLots.AddAsync(lot, cancellationToken),
                "добавить лот");
        }

        public Task UpdateAsync(FoodLot lot, CancellationToken cancellationToken = default)
        {
            DataAccessGuard.Execute(
                () => _dbContext.FoodLots.Update(lot),
                "обновить лот");

            return Task.CompletedTask;
        }

        public Task DeleteAsync(FoodLot lot, CancellationToken cancellationToken = default)
        {
            DataAccessGuard.Execute(
                () => _dbContext.FoodLots.Remove(lot),
                "удалить лот");

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.SaveChangesAsync(cancellationToken),
                "сохранить изменения лота");
        }
    }
}
