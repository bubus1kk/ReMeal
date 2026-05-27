using Application.DTOs.Maps;
using Application.Interfaces;

namespace Application.Services
{
    public sealed class MapService : IMapService
    {
        private const double EarthRadiusKm = 6371.0088;

        private readonly ILotService _lotService;
        private readonly IGeocodingService _geocodingService;

        public MapService(
            ILotService lotService,
            IGeocodingService geocodingService)
        {
            _lotService = lotService;
            _geocodingService = geocodingService;
        }

        public async Task<IReadOnlyList<MapFoodPointDto>> GetFoodPointsForMapAsync(
            CancellationToken cancellationToken = default)
        {
            var availableLots = await _lotService.GetAvailableLotsAsync(cancellationToken);

            return availableLots
                .Where(x =>
                    x.FoodPoint is { IsActive: true, Latitude: not null, Longitude: not null })
                .GroupBy(x => x.FoodPointId)
                .Select(group =>
                {
                    var foodPoint = group.First().FoodPoint!;
                    return new MapFoodPointDto(
                        foodPoint.Id,
                        foodPoint.Name,
                        foodPoint.Address,
                        new CoordinatesDto(foodPoint.Latitude!.Value, foodPoint.Longitude!.Value),
                        group.Count());
                })
                .OrderBy(x => x.Name)
                .ToList();
        }

        public async Task<IReadOnlyList<NearestFoodPointDto>> FindNearestFoodPointsAsync(
            string address,
            int maxResults = 5,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address) || maxResults <= 0)
                return Array.Empty<NearestFoodPointDto>();

            var geocodingResult = await _geocodingService.GeocodeAddressAsync(address, cancellationToken);
            if (geocodingResult is null)
                return Array.Empty<NearestFoodPointDto>();

            var origin = geocodingResult.Coordinates;
            var foodPoints = await GetFoodPointsForMapAsync(cancellationToken);

            return foodPoints
                .Select(point => new NearestFoodPointDto(
                    point.Id,
                    point.Name,
                    point.Address,
                    point.Coordinates,
                    CalculateDistanceKm(origin, point.Coordinates),
                    point.AvailableLotCount))
                .OrderBy(x => x.DistanceKm)
                .Take(maxResults)
                .ToList();
        }

        public double CalculateDistanceKm(
            CoordinatesDto first,
            CoordinatesDto second)
        {
            var firstLatitude = DegreesToRadians(first.Latitude);
            var secondLatitude = DegreesToRadians(second.Latitude);
            var latitudeDelta = DegreesToRadians(second.Latitude - first.Latitude);
            var longitudeDelta = DegreesToRadians(second.Longitude - first.Longitude);

            var haversine = Math.Sin(latitudeDelta / 2) * Math.Sin(latitudeDelta / 2) +
                Math.Cos(firstLatitude) * Math.Cos(secondLatitude) *
                Math.Sin(longitudeDelta / 2) * Math.Sin(longitudeDelta / 2);

            var angularDistance = 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));
            return EarthRadiusKm * angularDistance;
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
    }
}
