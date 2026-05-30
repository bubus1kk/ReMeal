using Application.DTOs.Maps;
using Application.Interfaces;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Services
{
    public sealed class NominatimGeocodingService : IGeocodingService
    {
        private static readonly Uri SearchEndpoint = new("https://nominatim.openstreetmap.org/search");
        private static readonly Uri ReverseEndpoint = new("https://nominatim.openstreetmap.org/reverse");
        private static readonly TimeSpan MinimumRequestInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private const string UserAgent = "ReMealApp/1.0 (educational Avalonia desktop application)";

        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _requestGate = new(1, 1);
        private DateTimeOffset _lastRequestAt = DateTimeOffset.MinValue;

        public NominatimGeocodingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = RequestTimeout;

            _httpClient.DefaultRequestHeaders.UserAgent.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("ReMealApp", "1.0"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("(educational Avalonia desktop application)"));
        }

        public async Task<GeocodingResultDto?> GeocodeAddressAsync(
            string address,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            await _requestGate.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await WaitForRateLimitAsync(cancellationToken).ConfigureAwait(false);
                _lastRequestAt = DateTimeOffset.UtcNow;

                using var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    BuildSearchUri(address));
                request.Headers.UserAgent.ParseAdd(UserAgent);

                using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return null;

                await using var responseStream = await response.Content
                    .ReadAsStreamAsync(cancellationToken)
                    .ConfigureAwait(false);
                var results = await JsonSerializer.DeserializeAsync<List<NominatimSearchResult>>(
                    responseStream,
                    JsonOptions,
                    cancellationToken).ConfigureAwait(false);

                var result = results?.FirstOrDefault();
                if (result is null ||
                    !TryParseCoordinate(result.Latitude, out var latitude) ||
                    !TryParseCoordinate(result.Longitude, out var longitude))
                {
                    return null;
                }

                return new GeocodingResultDto(
                    new CoordinatesDto(latitude, longitude),
                    result.DisplayName ?? string.Empty);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsExpectedGeocodingError(ex))
            {
                Trace.TraceWarning($"Nominatim geocoding failed: {ex.Message}");
                return null;
            }
            finally
            {
                _requestGate.Release();
            }
        }

        public async Task<GeocodingResultDto?> ReverseGeocodeAsync(
            CoordinatesDto coordinates,
            CancellationToken cancellationToken = default)
        {
            if (coordinates.Latitude is < -90 or > 90 ||
                coordinates.Longitude is < -180 or > 180)
            {
                return null;
            }

            await _requestGate.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await WaitForRateLimitAsync(cancellationToken).ConfigureAwait(false);
                _lastRequestAt = DateTimeOffset.UtcNow;

                foreach (var zoom in new[] { 18, 17, 16, 15, 14 })
                {
                    var result = await TryReverseGeocodeAsync(
                        coordinates,
                        zoom,
                        cancellationToken).ConfigureAwait(false);

                    if (result is not null)
                        return result;
                }

                return null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsExpectedGeocodingError(ex))
            {
                Trace.TraceWarning($"Nominatim reverse geocoding failed: {ex.Message}");
                return null;
            }
            finally
            {
                _requestGate.Release();
            }
        }

        private async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRequestAt;
            if (elapsed >= MinimumRequestInterval)
                return;

            await Task.Delay(MinimumRequestInterval - elapsed, cancellationToken).ConfigureAwait(false);
        }

        private static Uri BuildSearchUri(string address)
        {
            var query = string.Join(
                "&",
                "format=jsonv2",
                "limit=1",
                "addressdetails=0",
                "q=" + Uri.EscapeDataString(address.Trim()));

            return new Uri(SearchEndpoint + "?" + query);
        }

        private static Uri BuildReverseUri(CoordinatesDto coordinates)
        {
            return BuildReverseUri(coordinates, zoom: 18);
        }

        private static Uri BuildReverseUri(CoordinatesDto coordinates, int zoom)
        {
            var query = string.Join(
                "&",
                "format=jsonv2",
                "addressdetails=1",
                "accept-language=ru",
                "zoom=" + zoom.ToString(CultureInfo.InvariantCulture),
                "lat=" + coordinates.Latitude.ToString(CultureInfo.InvariantCulture),
                "lon=" + coordinates.Longitude.ToString(CultureInfo.InvariantCulture));

            return new Uri(ReverseEndpoint + "?" + query);
        }

        private async Task<GeocodingResultDto?> TryReverseGeocodeAsync(
            CoordinatesDto coordinates,
            int zoom,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                BuildReverseUri(coordinates, zoom));
            request.Headers.UserAgent.ParseAdd(UserAgent);

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            await using var responseStream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<NominatimReverseResult>(
                responseStream,
                JsonOptions,
                cancellationToken).ConfigureAwait(false);

            if (result is null)
                return null;

            var displayName = BuildReverseDisplayName(result);
            if (string.IsNullOrWhiteSpace(displayName))
                return null;

            return new GeocodingResultDto(
                coordinates,
                displayName);
        }

        private static string BuildReverseDisplayName(NominatimReverseResult result)
        {
            var parts = new List<string>();

            if (TryGetAddressPart(result.Address, "road", out var road))
            {
                if (TryGetAddressPart(result.Address, "house_number", out var houseNumber))
                    parts.Add($"{road}, {houseNumber}");
                else
                    parts.Add(road);
            }

            if (TryGetAddressPart(result.Address, "city", out var city) ||
                TryGetAddressPart(result.Address, "town", out city) ||
                TryGetAddressPart(result.Address, "village", out city) ||
                TryGetAddressPart(result.Address, "municipality", out city))
            {
                parts.Add(city);
            }

            if (TryGetAddressPart(result.Address, "state", out var state))
                parts.Add(state);

            if (TryGetAddressPart(result.Address, "country", out var country))
                parts.Add(country);

            if (parts.Count > 0)
                return string.Join(", ", parts.Distinct()).Trim();

            return result.DisplayName?.Trim() ?? string.Empty;
        }

        private static bool TryGetAddressPart(
            Dictionary<string, string>? address,
            string key,
            out string value)
        {
            value = string.Empty;
            return address is not null &&
                address.TryGetValue(key, out var rawValue) &&
                !string.IsNullOrWhiteSpace(rawValue) &&
                (value = rawValue.Trim()).Length > 0;
        }

        private static bool TryParseCoordinate(string? value, out double coordinate)
        {
            return double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out coordinate);
        }

        private static bool IsExpectedGeocodingError(Exception exception)
        {
            return exception is HttpRequestException
                or TaskCanceledException
                or JsonException
                or FormatException
                or OverflowException
                or InvalidOperationException;
        }

        private sealed class NominatimSearchResult
        {
            [JsonPropertyName("lat")]
            public string? Latitude { get; init; }

            [JsonPropertyName("lon")]
            public string? Longitude { get; init; }

            [JsonPropertyName("display_name")]
            public string? DisplayName { get; init; }
        }

        private sealed class NominatimReverseResult
        {
            [JsonPropertyName("display_name")]
            public string? DisplayName { get; init; }

            [JsonPropertyName("address")]
            public Dictionary<string, string>? Address { get; init; }
        }
    }
}
