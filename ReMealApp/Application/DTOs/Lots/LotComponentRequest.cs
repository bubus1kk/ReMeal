namespace Application.DTOs.Lots
{
    public sealed record LotComponentRequest(
        Guid? Id,
        string Name,
        int Quantity,
        string Unit,
        string? Composition,
        string? ImagePath,
        int SortOrder);
}
