namespace Application.DTOs.Profile
{
    public sealed class AdminProfileStatisticsDto
    {
        public List<ProfileKpiDto> Kpis { get; set; } = new();

        public List<AdminRoleDistributionDto> RoleDistribution { get; set; } = new();
    }
}
