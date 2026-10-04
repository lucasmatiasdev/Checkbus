using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="EventosClient"/> maps every realistic <c>api/Eventos/*</c> server response
/// (success, validation failure, auth failure, transport failure) into the matching closed outcome
/// case. Mirrors <see cref="MaintenanceRecordsClientTests"/>'s convention.
/// </summary>
public class EventosClientTests
{
    private static readonly Guid EventoId = Guid.NewGuid();

    private static EventosClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new EventosClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static EventoResponse MakeEvento() => new()
    {
        Id = EventoId,
        Nombre = "Final Copa Argentina",
        Tipo = WebEventoTipo.Partido,
        Fecha = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
        Ubicacion = new UbicacionResponse
        {
            Nombre = "Estadio Mario Alberto Kempes",
            Direccion = "Av. Cardeñosa, Córdoba",
            Latitud = -31.3333,
            Longitud = -64.2333
        }
    };

    private static CreateEventoRequest MakeCreateRequest() => new()
    {
        Nombre = "Final Copa Argentina",
        Tipo = WebEventoTipo.Partido,
        Fecha = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
        UbicacionNombre = "Estadio Mario Alberto Kempes",
        Direccion = "Av. Cardeñosa, Córdoba",
        PlaceId = "place-kempes-1",
        Latitud = -31.3333,
        Longitud = -64.2333
    };

    // ----- CreateAsync -----

    [Fact]
    public async Task CreateAsync_WhenCreated_ReturnsSuccessWithDeserializedEvento()
    {
        var expected = MakeEvento();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var success = Assert.IsType<EventoActionOutcome.Success>(outcome);
        Assert.Equal(EventoId, success.Evento.Id);
        Assert.Equal(WebEventoTipo.Partido, success.Evento.Tipo);
        Assert.Equal("Estadio Mario Alberto Kempes", success.Evento.Ubicacion.Nombre);
    }

    [Fact]
    public async Task CreateAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["Nombre"] = ["'Nombre' must not be empty."]
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

        var validationFailed = Assert.IsType<EventoActionOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("Nombre", error.Key);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<EventoActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<EventoActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<EventoActionOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task CreateAsync_PostsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(MakeEvento())
        });
        var client = CreateClient(stub);

        await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Post, stub.CapturedRequest!.Method);
        Assert.Contains("Eventos", stub.CapturedRequest.RequestUri!.ToString());
    }

    // ----- GetListAsync -----

    [Fact]
    public async Task GetListAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new List<EventoResponse> { MakeEvento() };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        var success = Assert.IsType<EventosListOutcome.Success>(outcome);
        Assert.Single(success.Eventos);
    }

    [Fact]
    public async Task GetListAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        Assert.IsType<EventosListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        Assert.IsType<EventosListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<EventosListOutcome.TransportError>(outcome);
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

        var transportError = Assert.IsType<EventosListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }

    [Fact]
    public async Task GetListAsync_SendsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<EventoResponse>())
        });
        var client = CreateClient(stub);

        await client.GetListAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains("Eventos", stub.CapturedRequest.RequestUri!.ToString());
    }
}
