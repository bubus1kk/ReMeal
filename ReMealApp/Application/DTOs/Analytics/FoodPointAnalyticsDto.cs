namespace Application.DTOs.Analytics
{
    public sealed class FoodPointAnalyticsDto
    {
        public Guid FoodPointId { get; set; }

        public string FoodPointName { get; set; } = string.Empty;

        public int IssuedQuantity { get; set; }
    }
}