using ReMeal.Domain.Entities;

namespace ReMeal.Application.Interfaces;

public interface IFoodPointRepository
{
    Task<FoodPoint?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<FoodPoint>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(FoodPoint foodPoint, CancellationToken cancellationToken = default);

    Task UpdateAsync(FoodPoint foodPoint, CancellationToken cancellationToken = default);

    Task DeleteAsync(FoodPoint foodPoint, CancellationToken cancellationToken = default);
}
