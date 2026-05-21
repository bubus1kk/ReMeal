namespace Application.DTOs.Lots
{
    public sealed record UpdateLotRequest(
        Guid Id,
        string Title,
        string Description,
        string Composition,
        decimal Price,
        DateTime PickupDeadline,
        IReadOnlyList<LotComponentRequest>? Components = null,
        string? ImagePath = null);
}
