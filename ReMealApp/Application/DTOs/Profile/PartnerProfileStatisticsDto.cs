namespace Application.DTOs.Profile
{
    public sealed class PartnerProfileStatisticsDto
    {
        public List<ProfileKpiDto> Kpis { get; set; } = new();

        public List<PartnerFoodPointStatisticsDto> FoodPoints { get; set; } = new();
    }
}
