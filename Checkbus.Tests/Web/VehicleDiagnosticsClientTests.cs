using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="VehicleDiagnosticsClient"/> maps every realistic
/// <c>api/VehicleDiagnostics/*</c> server response (success, validation failure, 404, auth
/// failure, transport failure) into the matching closed outcome case. Mirrors
/// <see cref="VehicleDocumentsClientTests"/>'s convention.
/// </summary>
public class VehicleDiagnosticsClientTests
{
    private static readonly Guid VehicleId = Guid.NewGuid();
    private static readonly Guid MaintenanceRecordId = Guid.NewGuid();
    private static readonly Guid DiagnosticId = Guid.NewGuid();

    private static VehicleDiagnosticsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new VehicleDiagnosticsClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static VehicleDiagnosticResponse MakeDiagnostic() => new()
    {
        Id = DiagnosticId,
        VehicleId = VehicleId,
        MaintenanceRecordId = MaintenanceRecordId,
        DiagnosedAt = new DateOnly(2026, 1, 3),
        Notes = "Revisión general",
        Components =
        [
            new ComponentDiagnosticResponse
            {
                Id = Guid.NewGuid(),
                Component = WebVehicleComponent.Frenos,
                Condition = WebComponentCondition.Desgastado,
                Notes = "Pastillas al 20%"
            }
        ]
    };

    private static CreateVehicleDiagnosticRequest MakeCreateRequest() => new()
    {
        MaintenanceRecordId = MaintenanceRecordId,
        DiagnosedAt = new DateOnly(2026, 1, 3),
        Notes = "Revisión general",
        Components =
        [
            new ComponentDiagnosticInputRequest
            {
                Component = WebVehicleComponent.Frenos,
                Condition = WebComponentCondition.Desgastado,
                Notes = "Pastillas al 20%"
            }
        ]
    };

    // ----- CreateAsync -----

    [Fact]
    public async Task CreateAsync_WhenCreated_ReturnsSuccessWithDeserializedDiagnostic()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(MakeDiagnostic())
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var success = Assert.IsType<VehicleDiagnosticActionOutcome.Success>(outcome);
        Assert.Equal(DiagnosticId, success.Diagnostic.Id);
        Assert.Single(success.Diagnostic.Components);
        Assert.Equal(WebVehicleComponent.Frenos, success.Diagnostic.Components[0].Component);
    }

    [Fact]
    public async Task CreateAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["Components"] = ["At least one component is required."]
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

        var validationFailed = Assert.IsType<VehicleDiagnosticActionOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("Components", error.Key);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDiagnosticActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDiagnosticActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenNotFound_ReturnsNotFound()
    {
        // The parent MaintenanceRecord doesn't exist or is out-of-org.
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDiagnosticActionOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehicleDiagnosticActionOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task CreateAsync_PostsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(MakeDiagnostic())
        });
        var client = CreateClient(stub);

        await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Post, stub.CapturedRequest!.Method);
        Assert.Contains("VehicleDiagnostics", stub.CapturedRequest.RequestUri!.ToString());
    }

    // ----- GetListAsync -----

    [Fact]
    public async Task GetListAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new List<VehicleDiagnosticResponse> { MakeDiagnostic() };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(MaintenanceRecordId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<VehicleDiagnosticsListOutcome.Success>(outcome);
        Assert.Single(success.Diagnostics);
        Assert.Equal(MaintenanceRecordId, success.Diagnostics[0].MaintenanceRecordId);
    }

    [Fact]
    public async Task GetListAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(MaintenanceRecordId, TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDiagnosticsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(MaintenanceRecordId, TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDiagnosticsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(MaintenanceRecordId, TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDiagnosticsListOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetListAsync(MaintenanceRecordId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehicleDiagnosticsListOutcome.TransportError>(outcome);
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

        var outcome = await client.GetListAsync(MaintenanceRecordId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehicleDiagnosticsListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }

    [Fact]
    public async Task GetListAsync_SendsMaintenanceRecordIdAsQueryParameter()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<VehicleDiagnosticResponse>())
        });
        var client = CreateClient(stub);

        await client.GetListAsync(MaintenanceRecordId, TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Contains(
            $"maintenanceRecordId={MaintenanceRecordId}",
            stub.CapturedRequest!.RequestUri!.ToString());
    }
}
