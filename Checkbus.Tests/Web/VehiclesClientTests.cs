using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="VehiclesClient"/> maps every realistic <c>api/Vehicles</c> server response
/// (success, validation failure, conflict, auth failure, transport failure) into the matching
/// closed outcome case for both <see cref="VehiclesClient.CreateAsync"/> (mirrors
/// <see cref="UserRegistrationClient"/>'s create-with-possible-conflict shape) and
/// <see cref="VehiclesClient.GetVehiclesAsync"/> (mirrors <see cref="UsersClient"/>'s plain-list
/// shape), including that enums serialize as plain integers (no <c>JsonStringEnumConverter</c>
/// registered anywhere in this solution).
/// </summary>
public class VehiclesClientTests
{
    private static CreateVehicleRequest CreateRequest() => new()
    {
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = 45,
        Mileage = 120000,
        Status = WebVehicleStatus.Activo,
        OwnerType = WebVehicleOwnerType.Organizacion
    };

    private static VehicleResponse CreateVehicleResponse() => new()
    {
        Id = Guid.NewGuid(),
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = 45,
        Mileage = 120000,
        Status = WebVehicleStatus.Activo,
        OwnerType = WebVehicleOwnerType.Organizacion
    };

    private static VehicleRegistrationResult CreateRegistrationResult() => new()
    {
        VehicleId = Guid.NewGuid(),
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = 45,
        Mileage = 120000,
        Status = WebVehicleStatus.Activo,
        OwnerType = WebVehicleOwnerType.Organizacion,
        OrganizationId = Guid.NewGuid()
    };

    private static VehiclesClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new VehiclesClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    // ----- CreateAsync -----

    [Fact]
    public async Task CreateAsync_WhenCreated_ReturnsSuccessWithDeserializedResult()
    {
        var expected = CreateRegistrationResult();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var success = Assert.IsType<VehicleRegistrationOutcome.Success>(outcome);
        Assert.Equal(expected, success.Result);
    }

    [Fact]
    public async Task CreateAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["Patent"] = ["'Patent' must not be empty."]
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

        var outcome = await client.CreateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var validationFailed = Assert.IsType<VehicleRegistrationOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("Patent", error.Key);
        Assert.Equal("'Patent' must not be empty.", Assert.Single(error.Value));
    }

    [Fact]
    public async Task CreateAsync_WhenConflict_ReturnsConflictWithDetailMessage()
    {
        var conflictProblem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = "A vehicle with this patent is already registered."
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(conflictProblem)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<VehicleRegistrationOutcome.Conflict>(outcome);
        Assert.Equal("A vehicle with this patent is already registered.", conflict.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<VehicleRegistrationOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<VehicleRegistrationOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.CreateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehicleRegistrationOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task CreateAsync_WhenBodyIsMalformed_ReturnsTransportErrorInsteadOfThrowing()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("not valid json", System.Text.Encoding.UTF8, "application/json")
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehicleRegistrationOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }

    [Fact]
    public async Task CreateAsync_SerializesEnumsAsPlainIntegers()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(CreateRegistrationResult())
        });
        var client = CreateClient(stub);

        await client.CreateAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        var body = await stub.CapturedRequest!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Number, root.GetProperty("status").ValueKind);
        Assert.Equal((int)WebVehicleStatus.Activo, root.GetProperty("status").GetInt32());
        Assert.Equal(JsonValueKind.Number, root.GetProperty("ownerType").ValueKind);
        Assert.Equal((int)WebVehicleOwnerType.Organizacion, root.GetProperty("ownerType").GetInt32());
    }

    // ----- GetVehiclesAsync -----

    [Fact]
    public async Task GetVehiclesAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new List<VehicleResponse> { CreateVehicleResponse() };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetVehiclesAsync(TestContext.Current.CancellationToken);

        var success = Assert.IsType<VehiclesListOutcome.Success>(outcome);
        Assert.Single(success.Vehicles);
        Assert.Equal(expected[0].Patent, success.Vehicles[0].Patent);
    }

    [Fact]
    public async Task GetVehiclesAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetVehiclesAsync(TestContext.Current.CancellationToken);

        Assert.IsType<VehiclesListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetVehiclesAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetVehiclesAsync(TestContext.Current.CancellationToken);

        Assert.IsType<VehiclesListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetVehiclesAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetVehiclesAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehiclesListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetVehiclesAsync_WhenBodyIsMalformed_ReturnsTransportErrorInsteadOfThrowing()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json", System.Text.Encoding.UTF8, "application/json")
        });
        var client = CreateClient(stub);

        var outcome = await client.GetVehiclesAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehiclesListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }
}
