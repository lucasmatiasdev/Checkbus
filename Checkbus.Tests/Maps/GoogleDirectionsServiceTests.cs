using System.Net;
using Checkbus.ApiService.Application.Interfaces.Maps;
using Checkbus.ApiService.Infrastructure.Implementations.Maps;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Maps;

/// <summary>
/// Proves <see cref="GoogleDirectionsService"/> builds the Google Directions REST request
/// correctly (origin/destination/waypoints/key query params) and maps a realistic response
/// (success with one or more legs, and failure statuses) into <see cref="DirectionsResult"/>
/// or a descriptive exception. Uses <see cref="StubHttpMessageHandler"/>, the same test double
/// already used by the Web project's HTTP client tests, applied here for the backend adapter.
/// </summary>
public class GoogleDirectionsServiceTests
{
    private static readonly GoogleMapsOptions Options = new() { ApiKey = "test-api-key" };

    private static (GoogleDirectionsService Service, StubHttpMessageHandler Handler) CreateService(
        HttpResponseMessage response)
    {
        var handler = new StubHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://maps.googleapis.com/") };
        var service = new GoogleDirectionsService(httpClient, Options);
        return (service, handler);
    }

    private static HttpResponseMessage MakeJsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json)
    };

    private const string SingleLegSuccessJson = """
        {
          "status": "OK",
          "routes": [
            {
              "legs": [
                { "duration": { "value": 1800 }, "distance": { "value": 15000 } }
              ]
            }
          ]
        }
        """;

    private const string MultiLegSuccessJson = """
        {
          "status": "OK",
          "routes": [
            {
              "legs": [
                { "duration": { "value": 600 }, "distance": { "value": 5000 } },
                { "duration": { "value": 900 }, "distance": { "value": 7500 } }
              ]
            }
          ]
        }
        """;

    private static readonly List<(double Latitude, double Longitude)> TwoPoints =
    [
        (-34.6037, -58.3816),
        (-34.9205, -57.9536)
    ];

    private static readonly List<(double Latitude, double Longitude)> ThreePoints =
    [
        (-34.6037, -58.3816),
        (-34.7000, -58.5000),
        (-34.9205, -57.9536)
    ];

    [Fact]
    public async Task GetDirectionsAsync_WhenSingleLegOk_ReturnsSummedDurationAndDistance()
    {
        var (service, handler) = CreateService(MakeJsonResponse(SingleLegSuccessJson));

        var result = await service.GetDirectionsAsync(TwoPoints, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(1800), result.TiempoEstimado);
        Assert.Equal(15m, result.DistanciaKm);
        Assert.NotNull(handler.CapturedRequest);
    }

    [Fact]
    public async Task GetDirectionsAsync_WhenMultiLegOk_SumsDurationAndDistanceAcrossLegs()
    {
        var (service, _) = CreateService(MakeJsonResponse(MultiLegSuccessJson));

        var result = await service.GetDirectionsAsync(ThreePoints, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(1500), result.TiempoEstimado);
        Assert.Equal(12.5m, result.DistanciaKm);
    }

    [Fact]
    public async Task GetDirectionsAsync_BuildsRequestWithOriginDestinationWaypointsAndKey()
    {
        var (service, handler) = CreateService(MakeJsonResponse(MultiLegSuccessJson));

        await service.GetDirectionsAsync(ThreePoints, CancellationToken.None);

        var requestUri = handler.CapturedRequest!.RequestUri!;
        Assert.Contains("maps/api/directions/json", requestUri.ToString());

        var query = ParseQuery(requestUri.Query);
        Assert.Equal("-34.6037,-58.3816", query["origin"]);
        Assert.Equal("-34.9205,-57.9536", query["destination"]);
        Assert.Equal("-34.7,-58.5", query["waypoints"]);
        Assert.Equal("test-api-key", query["key"]);
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>();
        var trimmed = query.TrimStart('?');
        if (trimmed.Length == 0)
        {
            return result;
        }

        foreach (var pair in trimmed.Split('&'))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    [Fact]
    public async Task GetDirectionsAsync_WhenStatusIsNotOk_ThrowsInvalidOperationException()
    {
        const string json = """
            { "status": "ZERO_RESULTS", "routes": [] }
            """;
        var (service, _) = CreateService(MakeJsonResponse(json));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDirectionsAsync(TwoPoints, CancellationToken.None));
    }

    [Fact]
    public async Task GetDirectionsAsync_WhenResponseIsMalformed_ThrowsInvalidOperationException()
    {
        const string json = "{ not valid json";
        var (service, _) = CreateService(MakeJsonResponse(json));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDirectionsAsync(TwoPoints, CancellationToken.None));
    }

    [Fact]
    public async Task GetDirectionsAsync_WhenOkButNoRoutes_ThrowsInvalidOperationException()
    {
        const string json = """
            { "status": "OK", "routes": [] }
            """;
        var (service, _) = CreateService(MakeJsonResponse(json));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetDirectionsAsync(TwoPoints, CancellationToken.None));
    }

    [Fact]
    public async Task GetDirectionsAsync_WhenFewerThanTwoPoints_ThrowsArgumentException()
    {
        var (service, _) = CreateService(MakeJsonResponse(SingleLegSuccessJson));

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetDirectionsAsync([(-34.6037, -58.3816)], CancellationToken.None));
    }
}
