using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="TripsClient"/> maps every realistic <c>api/Trips/*</c> server response
/// (success, validation/habilitación failure, not found, auth failure, transport failure) into
/// the matching closed outcome case. Mirrors <see cref="EventsClientTests"/>'s convention.
/// </summary>
public class TripsClientTests
{
    private static readonly Guid TripId = Guid.NewGuid();

    private static TripsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new TripsClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static TripResponse MakeTrip() => new()
    {
        Id = TripId,
        VehicleId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = WebTripStatus.Programado,
        VehiclePatent = "AB123CD",
        VehicleBrand = "Mercedes-Benz",
        VehicleModel = "O500",
        DriverName = "Juan",
        DriverSurname = "Perez",
        EventName = "Final Copa Argentina"
    };

    private static TripDetailResponse MakeTripDetail() => new()
    {
        Id = TripId,
        VehicleId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Capacity = 45,
        AvailableSeats = 45,
        Price = 15000m,
        Status = WebTripStatus.Programado,
        Route = new RouteResponse
        {
            EstimatedDuration = TimeSpan.FromHours(8),
            DistanceKm = 700m,
            Stops =
            [
                new StopResponse { Order = 0, Type = WebStopType.Origen, Name = "Terminal Retiro", Address = "Dir 1" },
                new StopResponse { Order = 1, Type = WebStopType.Destino, Name = "Estadio Kempes", Address = "Dir 2" }
            ]
        }
    };

    private static CreateTripRequest MakeCreateRequest() => new()
    {
        VehicleId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Price = 15000m,
        Stops =
        [
            new CreateTripStopRequest { Name = "Terminal Retiro", Address = "Dir 1", PlaceId = "place-1", Latitude = -34.5, Longitude = -58.3 },
            new CreateTripStopRequest { Name = "Estadio Kempes", Address = "Dir 2", PlaceId = "place-2", Latitude = -31.3, Longitude = -64.2 }
        ]
    };

    // ----- CreateAsync -----

    [Fact]
    public async Task CreateAsync_WhenCreated_ReturnsSuccessWithDeserializedTrip()
    {
        var expected = MakeTrip();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var success = Assert.IsType<TripActionOutcome.Success>(outcome);
        Assert.Equal(TripId, success.Trip.Id);
        Assert.Equal(WebTripStatus.Programado, success.Trip.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["Habilitacion"] = ["El vehículo no está habilitado: está inactivo."]
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed"
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(validationProblem)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var validationFailed = Assert.IsType<TripActionOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("Habilitacion", error.Key);
        Assert.Contains("no está habilitado", error.Value[0]);
    }

    [Fact]
    public async Task CreateAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<TripActionOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<TripActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<TripActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<TripActionOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task CreateAsync_PostsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(MakeTrip())
        });
        var client = CreateClient(stub);

        await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Post, stub.CapturedRequest!.Method);
        Assert.Contains("Trips", stub.CapturedRequest.RequestUri!.ToString());
    }

    // ----- GetListAsync -----

    [Fact]
    public async Task GetListAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new List<TripResponse> { MakeTrip() };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        var success = Assert.IsType<TripsListOutcome.Success>(outcome);
        Assert.Single(success.Trips);
    }

    [Fact]
    public async Task GetListAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        Assert.IsType<TripsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<TripsListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetListAsync_SendsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<TripResponse>())
        });
        var client = CreateClient(stub);

        await client.GetListAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains("Trips", stub.CapturedRequest.RequestUri!.ToString());
    }

    // ----- GetByIdAsync -----

    [Fact]
    public async Task GetByIdAsync_WhenOk_ReturnsSuccessWithDeserializedDetail()
    {
        var expected = MakeTripDetail();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetByIdAsync(TripId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<TripOutcome.Success>(outcome);
        Assert.Equal(TripId, success.Trip.Id);
        Assert.Equal(2, success.Trip.Route.Stops.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetByIdAsync(TripId, TestContext.Current.CancellationToken);

        Assert.IsType<TripOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetByIdAsync(TripId, TestContext.Current.CancellationToken);

        Assert.IsType<TripOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetByIdAsync(TripId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<TripOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetByIdAsync_SendsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(MakeTripDetail())
        });
        var client = CreateClient(stub);

        await client.GetByIdAsync(TripId, TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains($"Trips/{TripId}", stub.CapturedRequest.RequestUri!.ToString());
    }
}
