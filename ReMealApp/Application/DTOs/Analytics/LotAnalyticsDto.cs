namespace Application.DTOs.Analytics
{
    public sealed class LotAnalyticsDto
    {
        public Guid LotId { get; set; }

        public string LotTitle { get; set; } = string.Empty;

        public int IssuedQuantity { get; set; }
    }
}