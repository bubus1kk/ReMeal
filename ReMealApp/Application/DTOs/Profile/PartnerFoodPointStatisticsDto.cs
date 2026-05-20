namespace Application.DTOs.Profile
{
    public sealed class PartnerFoodPointStatisticsDto
    {
        public Guid FoodPointId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public int TotalLots { get; set; }

        public int ActiveLots { get; set; }

        public int SoldOutLots { get; set; }

        public int ExpiredLots { get; set; }

        public int CancelledLots { get; set; }

        public int AvailableSets { get; set; }
    }
}
