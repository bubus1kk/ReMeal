namespace Application.DTOs.Admin
{
    public sealed class AdminFoodPointDto
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Address { get; init; } = string.Empty;

        public string Phone { get; init; } = string.Empty;

        public string OwnerName { get; init; } = string.Empty;

        public string OwnerLogin { get; init; } = string.Empty;

        public int LotCount { get; init; }

        public bool IsActive { get; init; }

        public string StatusText => IsActive ? "Активна" : "Деактивирована";
    }
}
