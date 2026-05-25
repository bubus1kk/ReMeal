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

    }
}
