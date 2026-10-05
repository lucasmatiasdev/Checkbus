using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Checkbus.ApiService.Application.Interfaces.Maps;

namespace Checkbus.ApiService.Infrastructure.Implementations.Maps
{
    /// <summary>
    /// <see cref="IDirectionsService"/> adapter calling the Google Directions REST API
    /// (https://maps.googleapis.com/maps/api/directions/json). Sums duration/distance across
    /// every leg of the first returned route. This is the server-side authoritative call —
    /// separate from the client-embedded Maps JS used only for picking/previewing points.
    /// </summary>
    public class GoogleDirectionsService(HttpClient httpClient, GoogleMapsOptions options) : IDirectionsService
    {
        private const string DirectionsPath = "maps/api/directions/json";

        public async Task<DirectionsResult> GetDirectionsAsync(
            IReadOnlyList<(double Latitude, double Longitude)> points,
            CancellationToken cancellationToken)
        {
            if (points.Count < 2)
            {
                throw new ArgumentException(
                    "At least 2 points (origin and destination) are required.", nameof(points));
            }

            var requestUri = BuildRequestUri(points);

            HttpResponseMessage response;
            string body;
            try
            {
                response = await httpClient.GetAsync(requestUri, cancellationToken);
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    "Failed to reach the Google Directions API.", ex);
            }

            GoogleDirectionsResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<GoogleDirectionsResponse>(body);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    "Could not parse the Google Directions API response.", ex);
            }

            if (parsed is null)
            {
                throw new InvalidOperationException(
                    "The Google Directions API returned an empty response.");
            }

            if (!string.Equals(parsed.Status, "OK", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The Google Directions API returned a non-OK status: {parsed.Status ?? "(none)"}.");
            }

            var route = parsed.Routes?.FirstOrDefault();
            if (route?.Legs is null || route.Legs.Count == 0)
            {
                throw new InvalidOperationException(
                    "The Google Directions API response did not include any route legs.");
            }

            var totalSeconds = route.Legs.Sum(leg => leg.Duration?.Value ?? 0);
            var totalMeters = route.Legs.Sum(leg => leg.Distance?.Value ?? 0);

            return new DirectionsResult(
                TimeSpan.FromSeconds(totalSeconds),
                totalMeters / 1000m);
        }

        private string BuildRequestUri(IReadOnlyList<(double Latitude, double Longitude)> points)
        {
            var origin = FormatPoint(points[0]);
            var destination = FormatPoint(points[^1]);
            var intermediatePoints = points.Skip(1).Take(points.Count - 2).ToList();

            var query = new List<string>
            {
                $"origin={Uri.EscapeDataString(origin)}",
                $"destination={Uri.EscapeDataString(destination)}"
            };

            if (intermediatePoints.Count > 0)
            {
                var waypoints = string.Join('|', intermediatePoints.Select(FormatPoint));
                query.Add($"waypoints={Uri.EscapeDataString(waypoints)}");
            }

            query.Add($"key={Uri.EscapeDataString(options.ApiKey)}");

            return $"{DirectionsPath}?{string.Join('&', query)}";
        }

        private static string FormatPoint((double Latitude, double Longitude) point) =>
            string.Create(
                CultureInfo.InvariantCulture,
                $"{point.Latitude},{point.Longitude}");

        private sealed class GoogleDirectionsResponse
        {
            [JsonPropertyName("status")]
            public string? Status { get; set; }

            [JsonPropertyName("routes")]
            public List<GoogleRoute>? Routes { get; set; }
        }

        private sealed class GoogleRoute
        {
            [JsonPropertyName("legs")]
            public List<GoogleLeg>? Legs { get; set; }
        }

        private sealed class GoogleLeg
        {
            [JsonPropertyName("duration")]
            public GoogleValueField? Duration { get; set; }

            [JsonPropertyName("distance")]
            public GoogleValueField? Distance { get; set; }
        }

        private sealed class GoogleValueField
        {
            [JsonPropertyName("value")]
            public long Value { get; set; }
        }
    }
}
