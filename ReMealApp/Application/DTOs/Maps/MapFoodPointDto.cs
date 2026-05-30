namespace Application.DTOs.Maps
{
    public sealed record MapFoodPointDto(
        Guid Id,
        string Name,
        string Address,
        CoordinatesDto Coordinates,
        int AvailableLotCount);
}
