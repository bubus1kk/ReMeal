using Application.DTOs.Admin;

namespace Application.Interfaces
{
    public interface IAdminService
    {
        Task EnsureAdministratorAccessAsync(CancellationToken cancellationToken = default);

        Task<List<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

        Task ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<List<AdminFoodPointDto>> GetFoodPointsAsync(CancellationToken cancellationToken = default);

        Task ActivateFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default);

        Task DeactivateFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default);

        Task<List<AdminLotDto>> GetLotsAsync(CancellationToken cancellationToken = default);

        Task CancelLotAsync(Guid lotId, CancellationToken cancellationToken = default);

        Task<List<AdminBookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default);
    }
}
