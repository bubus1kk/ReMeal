namespace Application.DTOs.Maps
{
    public sealed record GeocodingResultDto(
        CoordinatesDto Coordinates,
        string DisplayName);
}
