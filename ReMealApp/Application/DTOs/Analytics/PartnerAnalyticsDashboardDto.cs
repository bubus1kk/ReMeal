namespace Application.DTOs.Analytics
{
    public sealed class PartnerAnalyticsDashboardDto
    {
        public int IssuedBookingsCount { get; set; }

        public int SavedPortionsCount { get; set; }

        public int CancelledBookingsCount { get; set; }

        public int ActiveBookingsCount { get; set; }

        public double PreventedWasteKg { get; set; }

        public List<DateChartPointDto> SavedPortionsByDay { get; set; } = new();

        public List<StatusChartPointDto> BookingStatusDistribution { get; set; } = new();

        public List<LotAnalyticsDto> TopLotsByIssuedQuantity { get; set; } = new();

        public List<FoodPointAnalyticsDto> IssuedByFoodPoint { get; set; } = new();

        public List<DateChartPointDto> PreventedWasteByDay { get; set; } = new();
    }
}