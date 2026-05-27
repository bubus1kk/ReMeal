namespace Application.DTOs.Maps
{
    public sealed record NearestFoodPointDto(
        Guid Id,
        string Name,
        string Address,
        CoordinatesDto Coordinates,
        double DistanceKm,
        int AvailableLotCount);
}
