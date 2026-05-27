using Application.DTOs.Maps;

namespace Application.Interfaces
{
    public interface IGeocodingService
    {
        Task<GeocodingResultDto?> GeocodeAddressAsync(
            string address,
            CancellationToken cancellationToken = default);
    }
}
