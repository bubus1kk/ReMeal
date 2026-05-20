using Application.DTOs.Profile;

namespace Application.Interfaces
{
    public interface IProfileStatisticsService
    {
        Task<PartnerProfileStatisticsDto> GetCurrentPartnerStatisticsAsync(CancellationToken cancellationToken = default);

        Task<AdminProfileStatisticsDto> GetAdminStatisticsAsync(CancellationToken cancellationToken = default);
    }
}
