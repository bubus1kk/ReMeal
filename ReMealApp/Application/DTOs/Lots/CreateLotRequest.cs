namespace Application.DTOs.Lots
{
    public sealed record CreateLotRequest(
        string Title,
        string Description,
        string Composition,
        int TotalQuantity,
        decimal Price,
        DateTime PickupDeadline);
}
