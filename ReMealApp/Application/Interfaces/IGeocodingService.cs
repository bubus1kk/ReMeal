using Application.DTOs.Maps;

namespace Application.Interfaces
{
    public interface IGeocodingService
    {
        Task<GeocodingResultDto?> GeocodeAddressAsync(
            string address,
            CancellationToken cancellationToken = default);

        Task<GeocodingResultDto?> ReverseGeocodeAsync(
            CoordinatesDto coordinates,
            CancellationToken cancellationToken = default);
    }
}
