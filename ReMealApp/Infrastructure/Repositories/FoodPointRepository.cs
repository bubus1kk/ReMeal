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
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.FoodPoints
                    .Include(x => x.Lots)
                    .Include(x => x.Owner)
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
                "получить точку питания по id");
        }

        public Task<FoodPoint?> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.FoodPoints
                    .Include(x => x.Lots)
                    .Include(x => x.Owner)
                    .FirstOrDefaultAsync(x => x.OwnerId == ownerId, cancellationToken),
                "получить точку питания партнера");
        }

        public Task<List<FoodPoint>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.FoodPoints
                    .Include(x => x.Lots)
                    .Include(x => x.Owner)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync(cancellationToken),
                "получить список точек питания");
        }

        public async Task AddAsync(FoodPoint foodPoint, CancellationToken cancellationToken = default)
        {
            await DataAccessGuard.ExecuteAsync(
                async () => await _dbContext.FoodPoints.AddAsync(foodPoint, cancellationToken),
                "добавить точку питания");
        }

        public Task UpdateAsync(FoodPoint foodPoint, CancellationToken cancellationToken = default)
        {
            DataAccessGuard.Execute(
                () => _dbContext.FoodPoints.Update(foodPoint),
                "обновить точку питания");

            return Task.CompletedTask;
        }

        public Task DeleteAsync(FoodPoint foodPoint, CancellationToken cancellationToken = default)
        {
            DataAccessGuard.Execute(
                () => _dbContext.FoodPoints.Remove(foodPoint),
                "удалить точку питания");

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.SaveChangesAsync(cancellationToken),
                "сохранить изменения точки питания");
        }
    }
}
