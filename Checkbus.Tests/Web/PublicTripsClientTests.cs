using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Features.PublicReservations.Contracts;
using Checkbus.Web.Features.PublicReservations.Services;
using Checkbus.Web.Features.Trips.Contracts;
using Checkbus.Web.Features.Users.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        OccupiedSeatNumbers = [2, 5],
        Seats = Enumerable.Range(1, 45)
            .Select(number => new SeatMapSeat { SeatId = Guid.NewGuid(), Number = number })
            .ToList()
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

    private static PublicTripDetailResponse MakeDetail() => new()
    {
        TripId = TripId,
        OrganizationName = "Brahman",
        EventId = Guid.NewGuid(),
        EventName = "Coldplay en River",
        EventHasImage = true,
        Price = 15000m,
        AvailableSeats = 40,
        DepartureDate = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        ArrivalDate = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Stops =
        [
            new PublicTripStop
            {
                Order = 0,
                Type = WebStopType.Origen,
                Name = "Terminal",
                Address = "Av. Siempre Viva 123",
                Latitude = -34.6,
                Longitude = -58.4
            }
        ]
    };

    [Fact]
    public async Task GetDetailsAsync_WhenOk_ReturnsSuccessWithDeserializedDetail()
    {
        var expected = MakeDetail();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetDetailsAsync(TripId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<PublicTripDetailOutcome.Success>(outcome);
        Assert.Equal(TripId, success.Detail.TripId);
        Assert.Equal("Brahman", success.Detail.OrganizationName);
        Assert.True(success.Detail.EventHasImage);
        Assert.Single(success.Detail.Stops);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetDetailsAsync(TripId, TestContext.Current.CancellationToken);

        Assert.IsType<PublicTripDetailOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetDetailsAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetDetailsAsync(TripId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<PublicTripDetailOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetDetailsAsync_GetsExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(MakeDetail())
        });
        var client = CreateClient(stub);

        await client.GetDetailsAsync(TripId, TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains($"public/trips/{TripId}/details", stub.CapturedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetEventImageAsync_WhenOk_ReturnsSuccessWithBytesAndContentType()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        var stub = new StubHttpMessageHandler(response);
        var client = CreateClient(stub);

        var outcome = await client.GetEventImageAsync(TripId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<PublicTripEventImageOutcome.Success>(outcome);
        Assert.Equal(bytes, success.Content);
        Assert.Equal("image/png", success.ContentType);
    }

    [Fact]
    public async Task GetEventImageAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetEventImageAsync(TripId, TestContext.Current.CancellationToken);

        Assert.IsType<PublicTripEventImageOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetEventImageAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetEventImageAsync(TripId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<PublicTripEventImageOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetEventImageAsync_GetsExpectedRoute()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3])
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        var stub = new StubHttpMessageHandler(response);
        var client = CreateClient(stub);

        await client.GetEventImageAsync(TripId, TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains($"public/trips/{TripId}/event-image", stub.CapturedRequest.RequestUri!.ToString());
    }

    private static CreateReservationRequest MakeCreateRequest() => new()
    {
        Passengers =
        [
            new PassengerSeatRequest
            {
                SeatId = Guid.NewGuid(),
                Name = "Juan",
                Surname = "Perez",
                DocumentType = WebDocumentType.DNI,
                DocumentNumber = "30111222",
                Email = "juan@checkbus.test",
                Phone = "+54 11 5555-5555"
            }
        ]
    };

    [Fact]
    public async Task CreateReservationAsync_WhenCreated_ReturnsSuccessWithDeserializedResult()
    {
        var expected = new CreateReservationResponse
        {
            Reservations =
            [
                new CreatedReservation
                {
                    ReservationId = Guid.NewGuid(),
                    SeatNumber = 1,
                    PassengerName = "Juan",
                    PassengerSurname = "Perez",
                    Price = 15000m
                }
            ]
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateReservationAsync(TripId, MakeCreateRequest(), TestContext.Current.CancellationToken);

        var success = Assert.IsType<CreateReservationOutcome.Success>(outcome);
        Assert.Single(success.Result.Reservations);
        Assert.Equal(1, success.Result.Reservations[0].SeatNumber);
    }

    [Fact]
    public async Task CreateReservationAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var problem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["Passengers[0].Email"] = ["Email must be a valid email address."]
        })
        {
            Status = StatusCodes.Status400BadRequest
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(problem)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateReservationAsync(TripId, MakeCreateRequest(), TestContext.Current.CancellationToken);

        var validationFailed = Assert.IsType<CreateReservationOutcome.ValidationFailed>(outcome);
        Assert.True(validationFailed.Errors.ContainsKey("Passengers[0].Email"));
    }

    [Fact]
    public async Task CreateReservationAsync_WhenConflict_ReturnsConflictWithServerMessage()
    {
        var problem = new ProblemDetails { Status = 409, Title = "Conflict", Detail = "Seat already taken." };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(problem)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateReservationAsync(TripId, MakeCreateRequest(), TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<CreateReservationOutcome.Conflict>(outcome);
        Assert.Equal("Seat already taken.", conflict.Message);
    }

    [Fact]
    public async Task CreateReservationAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.CreateReservationAsync(TripId, MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<CreateReservationOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task CreateReservationAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.CreateReservationAsync(TripId, MakeCreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<CreateReservationOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task CreateReservationAsync_PostsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new CreateReservationResponse { Reservations = [] })
        });
        var client = CreateClient(stub);

        await client.CreateReservationAsync(TripId, MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Post, stub.CapturedRequest!.Method);
        Assert.Contains($"public/trips/{TripId}/reservations", stub.CapturedRequest.RequestUri!.ToString());
    }
}
