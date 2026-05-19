using Domain.Enums;

namespace Application.DTOs.Lots
{
    public sealed record LotListItemDto(
        Guid Id,
        Guid FoodPointId,
        string FoodPointName,
        string Title,
        int AvailableQuantity,
        int TotalQuantity,
        decimal Price,
        DateTime PickupDeadline,
        LotStatus Status);
}
