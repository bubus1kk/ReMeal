using ReMeal.Domain.Entities;

namespace ReMeal.Application.Interfaces;

public interface IFoodLotRepository
{
    Task<FoodLot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<FoodLot>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<FoodLot>> GetByFoodPointIdAsync(Guid foodPointId, CancellationToken cancellationToken = default);

    Task<List<FoodLot>> GetCandidatesForExpirationAsync(CancellationToken cancellationToken = default);

    Task AddAsync(FoodLot lot, CancellationToken cancellationToken = default);

    Task UpdateAsync(FoodLot lot, CancellationToken cancellationToken = default);

    Task DeleteAsync(FoodLot lot, CancellationToken cancellationToken = default);
}
