using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="ViajesClient"/> maps every realistic <c>api/Viajes/*</c> server response
/// (success, validation/habilitación failure, not found, auth failure, transport failure) into
/// the matching closed outcome case. Mirrors <see cref="EventosClientTests"/>'s convention.
/// </summary>
public class ViajesClientTests
{
    private static readonly Guid ViajeId = Guid.NewGuid();

    private static ViajesClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new ViajesClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static ViajeResponse MakeViaje() => new()
    {
        Id = ViajeId,
        VehicleId = Guid.NewGuid(),
        ChoferId = Guid.NewGuid(),
        EventoId = Guid.NewGuid(),
        FechaSalida = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        FechaLlegada = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Capacidad = 45,
        AsientosDisponibles = 45,
        Precio = 15000m,
        Estado = WebViajeEstado.Programado,
        VehiclePatent = "AB123CD",
        VehicleBrand = "Mercedes-Benz",
        VehicleModel = "O500",
        ChoferName = "Juan",
        ChoferSurname = "Perez",
        EventoNombre = "Final Copa Argentina"
    };

    private static ViajeDetailResponse MakeViajeDetail() => new()
    {
        Id = ViajeId,
        VehicleId = Guid.NewGuid(),
        ChoferId = Guid.NewGuid(),
        EventoId = Guid.NewGuid(),
        FechaSalida = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        FechaLlegada = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Capacidad = 45,
        AsientosDisponibles = 45,
        Precio = 15000m,
        Estado = WebViajeEstado.Programado,
        Ruta = new RutaResponse
        {
            TiempoEstimado = TimeSpan.FromHours(8),
            DistanciaKm = 700m,
            Stops =
            [
                new StopResponse { Orden = 0, Tipo = WebStopTipo.Origen, Nombre = "Terminal Retiro", Direccion = "Dir 1" },
                new StopResponse { Orden = 1, Tipo = WebStopTipo.Destino, Nombre = "Estadio Kempes", Direccion = "Dir 2" }
            ]
        }
    };

    private static CreateViajeRequest MakeCreateRequest() => new()
    {
        VehicleId = Guid.NewGuid(),
        ChoferId = Guid.NewGuid(),
        EventoId = Guid.NewGuid(),
        FechaSalida = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        FechaLlegada = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Precio = 15000m,
        Stops =
        [
            new CreateViajeStopRequest { Nombre = "Terminal Retiro", Direccion = "Dir 1", PlaceId = "place-1", Latitud = -34.5, Longitud = -58.3 },
            new CreateViajeStopRequest { Nombre = "Estadio Kempes", Direccion = "Dir 2", PlaceId = "place-2", Latitud = -31.3, Longitud = -64.2 }
        ]
    };

    // ----- CreateAsync -----

    [Fact]
    public async Task CreateAsync_WhenCreated_ReturnsSuccessWithDeserializedViaje()
    {
        var expected = MakeViaje();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var success = Assert.IsType<ViajeActionOutcome.Success>(outcome);
        Assert.Equal(ViajeId, success.Viaje.Id);
        Assert.Equal(WebViajeEstado.Programado, success.Viaje.Estado);
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

        var validationFailed = Assert.IsType<ViajeActionOutcome.ValidationFailed>(outcome);
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

        Assert.IsType<ViajeActionOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<ViajeActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<ViajeActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<ViajeActionOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task CreateAsync_PostsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(MakeViaje())
        });
        var client = CreateClient(stub);

        await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Post, stub.CapturedRequest!.Method);
        Assert.Contains("Viajes", stub.CapturedRequest.RequestUri!.ToString());
    }

    // ----- GetListAsync -----

    [Fact]
    public async Task GetListAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new List<ViajeResponse> { MakeViaje() };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        var success = Assert.IsType<ViajesListOutcome.Success>(outcome);
        Assert.Single(success.Viajes);
    }

    [Fact]
    public async Task GetListAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        Assert.IsType<ViajesListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetListAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<ViajesListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetListAsync_SendsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<ViajeResponse>())
        });
        var client = CreateClient(stub);

        await client.GetListAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains("Viajes", stub.CapturedRequest.RequestUri!.ToString());
    }

    // ----- GetByIdAsync -----

    [Fact]
    public async Task GetByIdAsync_WhenOk_ReturnsSuccessWithDeserializedDetail()
    {
        var expected = MakeViajeDetail();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetByIdAsync(ViajeId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<ViajeOutcome.Success>(outcome);
        Assert.Equal(ViajeId, success.Viaje.Id);
        Assert.Equal(2, success.Viaje.Ruta.Stops.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetByIdAsync(ViajeId, TestContext.Current.CancellationToken);

        Assert.IsType<ViajeOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetByIdAsync(ViajeId, TestContext.Current.CancellationToken);

        Assert.IsType<ViajeOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetByIdAsync(ViajeId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<ViajeOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetByIdAsync_SendsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(MakeViajeDetail())
        });
        var client = CreateClient(stub);

        await client.GetByIdAsync(ViajeId, TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Get, stub.CapturedRequest!.Method);
        Assert.Contains($"Viajes/{ViajeId}", stub.CapturedRequest.RequestUri!.ToString());
    }
}
