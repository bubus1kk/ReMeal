using ReMeal.Application.DTOs.FoodPoints;
using ReMeal.Domain.Entities;

namespace ReMeal.Application.Interfaces;

public interface IFoodPointService
{
    Task<FoodPoint> CreateFoodPointAsync(
        CreateFoodPointRequest request,
        CancellationToken cancellationToken = default);

    Task<FoodPoint> UpdateFoodPointAsync(
        UpdateFoodPointRequest request,
        CancellationToken cancellationToken = default);

    Task DeactivateFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default);

    Task DeleteFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default);

    Task<List<FoodPoint>> GetAllFoodPointsAsync(CancellationToken cancellationToken = default);

    Task<FoodPoint?> GetFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default);
}
