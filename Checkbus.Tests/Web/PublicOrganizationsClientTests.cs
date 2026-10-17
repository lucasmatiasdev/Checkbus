using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Features.PublicReservations.Contracts;
using Checkbus.Web.Features.PublicReservations.Services;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="PublicOrganizationsClient"/> maps every realistic
/// <c>api/public/organizations/{orgSlug}/trips</c> server response (success, not found,
/// transport failure) into the matching closed outcome case, and builds the expected
/// route/query-string. Mirrors <see cref="PublicTripsClientTests"/>'s convention.
/// </summary>
public class PublicOrganizationsClientTests
{
    private const string OrgSlug = "checkbus-demo";

    private static PublicOrganizationsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new PublicOrganizationsClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static List<PublicOrganizationTripResponse> MakeTrips() =>
    [
        new()
        {
            TripSlug = "river-vs-boca-20261101",
            EventName = "River vs Boca",
            DepartureDate = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
            Price = 15000m
        }
    ];

    [Fact]
    public async Task GetTripsAsync_WhenOk_ReturnsSuccessWithDeserializedTrips()
    {
        var expected = MakeTrips();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetTripsAsync(OrgSlug, cancellationToken: TestContext.Current.CancellationToken);

        var success = Assert.IsType<GetPublicOrganizationTripsOutcome.Success>(outcome);
        Assert.Single(success.Trips);
        Assert.Equal("river-vs-boca-20261101", success.Trips[0].TripSlug);
        Assert.Equal("River vs Boca", success.Trips[0].EventName);
    }

    [Fact]
    public async Task GetTripsAsync_WhenOkWithEmptyList_ReturnsSuccessWithEmptyList()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<PublicOrganizationTripResponse>())
        });
        var client = CreateClient(stub);

        var outcome = await client.GetTripsAsync(OrgSlug, cancellationToken: TestContext.Current.CancellationToken);

        var success = Assert.IsType<GetPublicOrganizationTripsOutcome.Success>(outcome);
        Assert.Empty(success.Trips);
    }

    [Fact]
    public async Task GetTripsAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetTripsAsync(OrgSlug, cancellationToken: TestContext.Current.CancellationToken);

        Assert.IsType<GetPublicOrganizationTripsOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetTripsAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetTripsAsync(OrgSlug, cancellationToken: TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<GetPublicOrganizationTripsOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetTripsAsync_NoFilters_GetsExpectedRouteWithNoQueryString()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(MakeTrips())
        });
        var client = CreateClient(stub);

        await client.GetTripsAsync(OrgSlug, cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Equal(
            $"https://apiservice.local/api/public/organizations/{OrgSlug}/trips",
            stub.CapturedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetTripsAsync_WithFilters_GetsExpectedQueryString()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(MakeTrips())
        });
        var client = CreateClient(stub);
        var dateFrom = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc);

        await client.GetTripsAsync(
            OrgSlug, dateFrom, dateTo, "River", TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        var query = stub.CapturedRequest!.RequestUri!.ToString();
        Assert.Contains("dateFrom=2026-11-01", query);
        Assert.Contains("dateTo=2026-11-30", query);
        Assert.Contains("eventName=River", query);
    }
}
