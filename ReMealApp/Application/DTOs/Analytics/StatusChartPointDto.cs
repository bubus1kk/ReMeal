namespace Application.DTOs.Analytics
{
    public sealed class StatusChartPointDto
    {
        public string StatusName { get; set; } = string.Empty;

        public int Count { get; set; }
    }
}