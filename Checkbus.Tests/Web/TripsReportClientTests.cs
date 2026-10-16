using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Features.Trips.Contracts;
using Checkbus.Web.Features.TripsReport.Contracts;
using Checkbus.Web.Features.TripsReport.Services;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="TripsReportClient"/> maps every realistic <c>api/Trips/report</c> server
/// response (success, auth failure, transport failure) into the matching closed outcome case,
/// and that filters are sent as the expected query parameters. Mirrors
/// <see cref="MaintenanceRecordsClientTests"/>'s convention.
/// </summary>
public class TripsReportClientTests
{
    private static readonly Guid DriverId = Guid.NewGuid();
    private static readonly Guid VehicleId = Guid.NewGuid();

    private static TripsReportClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new TripsReportClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static TripsReportResponse MakeReport() => new()
    {
        Trips = [],
        Statistics = new TripsReportStatisticsResponse
        {
            TotalTrips = 0,
            TripsByStatus = [],
            TotalPassengers = 0,
            EstimatedRevenue = 0m
        }
    };

    // ----- GetReportAsync -----

    [Fact]
    public async Task GetReportAsync_WhenOk_ReturnsSuccessWithDeserializedReport()
    {
        var expected = new TripsReportResponse
        {
            Trips =
            [
                new TripResponse
                {
                    Id = Guid.NewGuid(),
                    VehicleId = VehicleId,
                    DriverId = DriverId,
                    EventId = Guid.NewGuid(),
                    Status = WebTripStatus.Completado
                }
            ],
            Statistics = new TripsReportStatisticsResponse
            {
                TotalTrips = 1,
                TripsByStatus = new Dictionary<WebTripStatus, int> { [WebTripStatus.Completado] = 1 },
                TotalPassengers = 10,
                EstimatedRevenue = 1000m
            }
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetReportAsync(cancellationToken: TestContext.Current.CancellationToken);

        var success = Assert.IsType<TripsReportOutcome.Success>(outcome);
        Assert.Single(success.Report.Trips);
        Assert.Equal(1, success.Report.Statistics.TotalTrips);
        Assert.Equal(1000m, success.Report.Statistics.EstimatedRevenue);
    }

    [Fact]
    public async Task GetReportAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetReportAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.IsType<TripsReportOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetReportAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetReportAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.IsType<TripsReportOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetReportAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetReportAsync(cancellationToken: TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<TripsReportOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetReportAsync_WhenBodyIsMalformed_ReturnsTransportErrorInsteadOfThrowing()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json", System.Text.Encoding.UTF8, "application/json")
        });
        var client = CreateClient(stub);

        var outcome = await client.GetReportAsync(cancellationToken: TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<TripsReportOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }

    [Fact]
    public async Task GetReportAsync_WithFilters_SendsThemAsQueryParameters()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(MakeReport())
        });
        var client = CreateClient(stub);
        var dateFrom = new DateTime(2026, 1, 1);
        var dateTo = new DateTime(2026, 1, 31);

        await client.GetReportAsync(
            dateFrom, dateTo, WebTripStatus.EnProceso, DriverId, VehicleId,
            TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        var uri = stub.CapturedRequest!.RequestUri!.ToString();
        Assert.Contains("dateFrom=2026-01-01", uri);
        Assert.Contains("dateTo=2026-01-31", uri);
        Assert.Contains($"status={(int)WebTripStatus.EnProceso}", uri);
        Assert.Contains($"driverId={DriverId}", uri);
        Assert.Contains($"vehicleId={VehicleId}", uri);
    }

    [Fact]
    public async Task GetReportAsync_WithoutFilters_SendsNoQueryString()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(MakeReport())
        });
        var client = CreateClient(stub);

        await client.GetReportAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(string.Empty, stub.CapturedRequest!.RequestUri!.Query);
    }
}
