using Application.DTOs.Analytics;

namespace Application.Interfaces
{
    public interface IPartnerAnalyticsService
    {
        Task<PartnerAnalyticsDashboardDto> GetDashboardAsync(
            AnalyticsPeriod period,
            Guid? foodPointId = null,
            CancellationToken cancellationToken = default);
    }
}