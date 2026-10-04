using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="EventsClient"/> maps every realistic <c>api/Events/*</c> server response
/// (success, validation failure, auth failure, transport failure) into the matching closed outcome
/// case. Mirrors <see cref="MaintenanceRecordsClientTests"/>'s convention.
/// </summary>
public class EventsClientTests
{
    private static readonly Guid EventId = Guid.NewGuid();

    private static EventsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new EventsClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static EventResponse MakeEvent() => new()
    {
        Id = EventId,
        Name = "Final Copa Argentina",
        Type = WebEventType.Partido,
        Date = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
        Location = new LocationResponse
        {
            Name = "Estadio Mario Alberto Kempes",
            Address = "Av. Cardeñosa, Córdoba",
            Latitude = -31.3333,
            Longitude = -64.2333
        }
    };

    private static CreateEventRequest MakeCreateRequest() => new()
    {
        Name = "Final Copa Argentina",
        Type = WebEventType.Partido,
        Date = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
        LocationName = "Estadio Mario Alberto Kempes",
        Address = "Av. Cardeñosa, Córdoba",
        PlaceId = "place-kempes-1",
        Latitude = -31.3333,
        Longitude = -64.2333
    };

    // ----- CreateAsync -----

    [Fact]
    public async Task CreateAsync_WhenCreated_ReturnsSuccessWithDeserializedEvent()
    {
        var expected = MakeEvent();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var success = Assert.IsType<EventActionOutcome.Success>(outcome);
        Assert.Equal(EventId, success.Event.Id);
        Assert.Equal(WebEventType.Partido, success.Event.Type);
        Assert.Equal("Estadio Mario Alberto Kempes", success.Event.Location.Name);
    }

    [Fact]
    public async Task CreateAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["Name"] = ["'Name' must not be empty."]
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

        var validationFailed = Assert.IsType<EventActionOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("Name", error.Key);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<EventActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<EventActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<EventActionOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task CreateAsync_PostsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(MakeEvent())
        });
        var client = CreateClient(stub);

        await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Post, stub.CapturedRequest!.Method);
        Assert.Contains("Events", stub.CapturedRequest.RequestUri!.ToString());
    }

    // ----- GetListAsync -----

    [Fact]
    public async Task GetListAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new List<EventResponse> { MakeEvent() };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        var success = Assert.IsType<EventsListOutcome.Success>(outcome);
        Assert.Single(success.Events);
    }

    [Fact]
    public async Task GetListAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        Assert.IsType<EventsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        Assert.IsType<EventsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<EventsListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetListAsync_WhenBodyIsMalformed_ReturnsTransportErrorInsteadOfThrowing()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json", System.Text.Encoding.UTF8, "application/json")
        });
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<EventsListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }

    [Fact]
    public async Task GetListAsync_SendsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<EventResponse>())
        });
        var client = CreateClient(stub);

        await client.GetListAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains("Events", stub.CapturedRequest.RequestUri!.ToString());
    }
}
