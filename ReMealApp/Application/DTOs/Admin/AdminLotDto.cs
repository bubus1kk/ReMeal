using Domain.Enums;

namespace Application.DTOs.Admin
{
    public sealed class AdminLotDto
    {
        public Guid Id { get; init; }

        public string Title { get; init; } = string.Empty;

        public string FoodPointName { get; init; } = string.Empty;

        public string OwnerName { get; init; } = string.Empty;

        public decimal Price { get; init; }

        public int TotalQuantity { get; init; }

        public int AvailableQuantity { get; init; }

        public DateTime PickupDeadline { get; init; }

        public LotStatus Status { get; init; }

        public bool CanCancel => Status is not LotStatus.Cancelled and not LotStatus.Expired;

        public string StatusText => Status switch
        {
            LotStatus.Active => "Активен",
            LotStatus.SoldOut => "Распродан",
            LotStatus.Expired => "Истек",
            LotStatus.Cancelled => "Отменен",
            _ => Status.ToString()
        };
    }
}
