using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Features.PublicReservations.Contracts;
using Checkbus.Web.Features.PublicReservations.Services;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="PublicTripsClient"/> maps every realistic
/// <c>api/public/trips/{id}/seatmap</c> server response (success, not found, transport
/// failure) into the matching closed outcome case. Mirrors <see cref="TripsClientTests"/>'s
/// convention. No Forbidden case to test — this endpoint is anonymous, so the API never
/// returns 401/403 for it.
/// </summary>
public class PublicTripsClientTests
{
    private static readonly Guid TripId = Guid.NewGuid();

    private static PublicTripsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new PublicTripsClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static PublicTripSeatMapResponse MakeSeatMap() => new()
    {
        TripId = TripId,
        Capacity = 45,
        Price = 15000m,
        OccupiedSeatNumbers = [2, 5]
    };

    [Fact]
    public async Task GetSeatMapAsync_WhenOk_ReturnsSuccessWithDeserializedSeatMap()
    {
        var expected = MakeSeatMap();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetSeatMapAsync(TripId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<PublicTripSeatMapOutcome.Success>(outcome);
        Assert.Equal(TripId, success.SeatMap.TripId);
        Assert.Equal(45, success.SeatMap.Capacity);
        Assert.Equal([2, 5], success.SeatMap.OccupiedSeatNumbers);
    }

    [Fact]
    public async Task GetSeatMapAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetSeatMapAsync(TripId, TestContext.Current.CancellationToken);

        Assert.IsType<PublicTripSeatMapOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetSeatMapAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetSeatMapAsync(TripId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<PublicTripSeatMapOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetSeatMapAsync_GetsExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(MakeSeatMap())
        });
        var client = CreateClient(stub);

        await client.GetSeatMapAsync(TripId, TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains($"public/trips/{TripId}/seatmap", stub.CapturedRequest.RequestUri!.ToString());
    }
}
