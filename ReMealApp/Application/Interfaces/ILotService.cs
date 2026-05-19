using Application.DTOs.Lots;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface ILotService
    {
        Task<FoodLot> CreateLotAsync(CreateLotRequest request, CancellationToken cancellationToken = default);

        Task<FoodLot> UpdateLotAsync(UpdateLotRequest request, CancellationToken cancellationToken = default);

        Task DeleteLotAsync(Guid lotId, CancellationToken cancellationToken = default);

        Task<List<FoodLot>> GetAllLotsAsync(CancellationToken cancellationToken = default);

        Task<List<FoodLot>> GetAvailableLotsAsync(CancellationToken cancellationToken = default);

        Task<List<FoodLot>> GetCurrentPartnerLotsAsync(CancellationToken cancellationToken = default);

        Task<List<FoodLot>> GetFoodPointLotsAsync(Guid foodPointId, CancellationToken cancellationToken = default);

        Task<int> MarkExpiredLotsAsync(CancellationToken cancellationToken = default);

        Task<FoodLot?> GetCurrentPartnerLotAsync(Guid lotId, CancellationToken cancellationToken = default);

        Task<FoodLot?> GetLotAsync(Guid lotId, CancellationToken cancellationToken = default);
    }
}
