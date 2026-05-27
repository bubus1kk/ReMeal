using Application.DTOs.Maps;

namespace Application.Interfaces
{
    public interface IMapService
    {
        Task<IReadOnlyList<MapFoodPointDto>> GetFoodPointsForMapAsync(
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<NearestFoodPointDto>> FindNearestFoodPointsAsync(
            string address,
            int maxResults = 5,
            CancellationToken cancellationToken = default);

        double CalculateDistanceKm(
            CoordinatesDto first,
            CoordinatesDto second);
    }
}
