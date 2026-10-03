using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="MaintenanceRecordsClient"/> maps every realistic
/// <c>api/MaintenanceRecords/*</c> server response (success, validation failure, 404, auth
/// failure, transport failure) into the matching closed outcome case. Mirrors
/// <see cref="VehicleDocumentsClientTests"/>'s convention.
/// </summary>
public class MaintenanceRecordsClientTests
{
    private static readonly Guid VehicleId = Guid.NewGuid();
    private static readonly Guid RecordId = Guid.NewGuid();

    private static MaintenanceRecordsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new MaintenanceRecordsClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    private static MaintenanceRecordResponse MakeRecord() => new()
    {
        Id = RecordId,
        VehicleId = VehicleId,
        Type = WebMaintenanceType.Preventivo,
        ScheduledDate = new DateOnly(2026, 1, 1),
        StartDate = null,
        EndDate = null,
        Status = WebMaintenanceStatus.Programado,
        Description = "Cambio de aceite",
        MechanicNotes = null,
        Cost = null,
        VehiclePatent = "ABC123",
        VehicleBrand = "Mercedes-Benz",
        VehicleModel = "O500"
    };

    private static CreateMaintenanceRecordRequest MakeCreateRequest() => new()
    {
        VehicleId = VehicleId,
        Type = WebMaintenanceType.Preventivo,
        ScheduledDate = new DateOnly(2026, 1, 1),
        Description = "Cambio de aceite"
    };

    private static UpdateMaintenanceRecordRequest MakeUpdateRequest() => new()
    {
        Status = WebMaintenanceStatus.EnProceso,
        StartDate = new DateOnly(2026, 1, 2),
        EndDate = null,
        MechanicNotes = "Revisando frenos",
        Cost = null
    };

    // ----- CreateAsync -----

    [Fact]
    public async Task CreateAsync_WhenCreated_ReturnsSuccessWithDeserializedRecord()
    {
        var expected = MakeRecord();
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var success = Assert.IsType<MaintenanceRecordActionOutcome.Success>(outcome);
        Assert.Equal(RecordId, success.Record.Id);
        Assert.Equal(WebMaintenanceType.Preventivo, success.Record.Type);
        Assert.Equal("ABC123", success.Record.VehiclePatent);
    }

    [Fact]
    public async Task CreateAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["Description"] = ["'Description' must not be empty."]
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

        var validationFailed = Assert.IsType<MaintenanceRecordActionOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("Description", error.Key);
    }

    [Fact]
    public async Task CreateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<MaintenanceRecordActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<MaintenanceRecordActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<MaintenanceRecordActionOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task CreateAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<MaintenanceRecordActionOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task CreateAsync_PostsToExpectedRoute()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(MakeRecord())
        });
        var client = CreateClient(stub);

        await client.CreateAsync(MakeCreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Post, stub.CapturedRequest!.Method);
        Assert.Contains("MaintenanceRecords", stub.CapturedRequest.RequestUri!.ToString());
    }

    // ----- UpdateAsync -----

    [Fact]
    public async Task UpdateAsync_WhenNoContent_ReturnsSuccess()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(stub);

        var outcome = await client.UpdateAsync(RecordId, MakeUpdateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<MaintenanceRecordUpdateOutcome.Success>(outcome);
    }

    [Fact]
    public async Task UpdateAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["Status"] = ["Invalid status transition."]
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

        var outcome = await client.UpdateAsync(RecordId, MakeUpdateRequest(), TestContext.Current.CancellationToken);

        var validationFailed = Assert.IsType<MaintenanceRecordUpdateOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("Status", error.Key);
    }

    [Fact]
    public async Task UpdateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.UpdateAsync(RecordId, MakeUpdateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<MaintenanceRecordUpdateOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.UpdateAsync(RecordId, MakeUpdateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<MaintenanceRecordUpdateOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task UpdateAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.UpdateAsync(RecordId, MakeUpdateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<MaintenanceRecordUpdateOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task UpdateAsync_PutsToRouteContainingId()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(stub);

        await client.UpdateAsync(RecordId, MakeUpdateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(HttpMethod.Put, stub.CapturedRequest!.Method);
        Assert.Contains($"MaintenanceRecords/{RecordId}", stub.CapturedRequest.RequestUri!.ToString());
    }

    // ----- GetListAsync -----

    [Fact]
    public async Task GetListAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new List<MaintenanceRecordResponse> { MakeRecord() };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(cancellationToken: TestContext.Current.CancellationToken);

        var success = Assert.IsType<MaintenanceRecordsListOutcome.Success>(outcome);
        Assert.Single(success.Records);
    }

    [Fact]
    public async Task GetListAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.IsType<MaintenanceRecordsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.IsType<MaintenanceRecordsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetListAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetListAsync(cancellationToken: TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<MaintenanceRecordsListOutcome.TransportError>(outcome);
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

        var outcome = await client.GetListAsync(cancellationToken: TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<MaintenanceRecordsListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }

    [Fact]
    public async Task GetListAsync_WithFilters_SendsThemAsQueryParameters()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<MaintenanceRecordResponse>())
        });
        var client = CreateClient(stub);

        await client.GetListAsync(
            VehicleId, WebMaintenanceStatus.EnProceso, WebMaintenanceType.Reactivo,
            TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        var uri = stub.CapturedRequest!.RequestUri!.ToString();
        Assert.Contains($"vehicleId={VehicleId}", uri);
        Assert.Contains($"status={(int)WebMaintenanceStatus.EnProceso}", uri);
        Assert.Contains($"type={(int)WebMaintenanceType.Reactivo}", uri);
    }

    [Fact]
    public async Task GetListAsync_WithoutFilters_SendsNoQueryString()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<MaintenanceRecordResponse>())
        });
        var client = CreateClient(stub);

        await client.GetListAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        Assert.Equal(string.Empty, stub.CapturedRequest!.RequestUri!.Query);
    }

    // ----- GetByIdAsync -----

    [Fact]
    public async Task GetByIdAsync_WhenOk_ReturnsSuccessWithDeserializedRecord()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(MakeRecord())
        });
        var client = CreateClient(stub);

        var outcome = await client.GetByIdAsync(RecordId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<MaintenanceRecordOutcome.Success>(outcome);
        Assert.Equal(RecordId, success.Record.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetByIdAsync(RecordId, TestContext.Current.CancellationToken);

        Assert.IsType<MaintenanceRecordOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetByIdAsync(RecordId, TestContext.Current.CancellationToken);

        Assert.IsType<MaintenanceRecordOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetByIdAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetByIdAsync(RecordId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<MaintenanceRecordOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }
}
