using Application.DTOs.FoodPoints;
using Domain.Entities;

namespace Application.Interfaces
{
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

        Task<FoodPoint?> GetCurrentPartnerFoodPointAsync(CancellationToken cancellationToken = default);

        Task<List<FoodPoint>> GetAllFoodPointsAsync(CancellationToken cancellationToken = default);

        Task<FoodPoint?> GetFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default);
    }
}
