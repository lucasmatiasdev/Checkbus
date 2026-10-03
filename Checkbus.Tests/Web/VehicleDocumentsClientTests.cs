using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="VehicleDocumentsClient"/> maps every realistic <c>api/VehicleDocuments/*</c>
/// server response (success, validation failure, 404, auth failure, transport failure) into the
/// matching closed outcome case, including that the upload request is actually sent as
/// <c>multipart/form-data</c> with the expected field names. Mirrors
/// <see cref="DriverRequirementsClientTests"/>'s convention.
/// </summary>
public class VehicleDocumentsClientTests
{
    private static readonly Guid VehicleId = Guid.NewGuid();

    private static VehicleDocumentsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new VehicleDocumentsClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    // ----- GetDocumentsAsync -----

    [Fact]
    public async Task GetDocumentsAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new List<VehicleDocumentResponse>
        {
            new()
            {
                Type = WebVehicleDocumentType.Seguro,
                Status = WebVehicleDocumentStatus.Pendiente,
                IssueDate = null,
                ExpirationDate = null,
                DocumentPresent = false
            },
            new()
            {
                Type = WebVehicleDocumentType.RTO_VTV,
                Status = WebVehicleDocumentStatus.Apto,
                IssueDate = new DateOnly(2025, 1, 1),
                ExpirationDate = new DateOnly(2026, 1, 1),
                DocumentPresent = true
            }
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetDocumentsAsync(VehicleId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<VehicleDocumentsListOutcome.Success>(outcome);
        Assert.Equal(2, success.Documents.Count);
        Assert.Equal(WebVehicleDocumentType.Seguro, success.Documents[0].Type);
        Assert.Equal(WebVehicleDocumentStatus.Apto, success.Documents[1].Status);
        Assert.True(success.Documents[1].DocumentPresent);
    }

    [Fact]
    public async Task GetDocumentsAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetDocumentsAsync(VehicleId, TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDocumentsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetDocumentsAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetDocumentsAsync(VehicleId, TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDocumentsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetDocumentsAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetDocumentsAsync(VehicleId, TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDocumentsListOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetDocumentsAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetDocumentsAsync(VehicleId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehicleDocumentsListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetDocumentsAsync_WhenBodyIsMalformed_ReturnsTransportErrorInsteadOfThrowing()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json", System.Text.Encoding.UTF8, "application/json")
        });
        var client = CreateClient(stub);

        var outcome = await client.GetDocumentsAsync(VehicleId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehicleDocumentsListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }

    // ----- UploadDocumentAsync -----

    private static async Task<VehicleDocumentActionOutcome> UploadAsync(
        VehicleDocumentsClient client,
        DateOnly? issueDate = null)
    {
        using var fileStream = new MemoryStream([1, 2, 3, 4]);
        return await client.UploadDocumentAsync(
            VehicleId,
            WebVehicleDocumentType.Seguro,
            fileStream,
            "seguro.pdf",
            "application/pdf",
            new DateOnly(2027, 1, 1),
            issueDate,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenNoContent_ReturnsSuccess()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(stub);

        var outcome = await UploadAsync(client);

        Assert.IsType<VehicleDocumentActionOutcome.Success>(outcome);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["ExpirationDate"] = ["'Expiration Date' must be in the future."]
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

        var outcome = await UploadAsync(client);

        var validationFailed = Assert.IsType<VehicleDocumentActionOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("ExpirationDate", error.Key);
        Assert.Equal("'Expiration Date' must be in the future.", Assert.Single(error.Value));
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await UploadAsync(client);

        Assert.IsType<VehicleDocumentActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await UploadAsync(client);

        Assert.IsType<VehicleDocumentActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await UploadAsync(client);

        Assert.IsType<VehicleDocumentActionOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await UploadAsync(client);

        var transportError = Assert.IsType<VehicleDocumentActionOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task UploadDocumentAsync_SendsMultipartFormDataWithExpectedFields()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(stub);

        await UploadAsync(client, issueDate: new DateOnly(2026, 1, 1));

        Assert.NotNull(stub.CapturedRequest);
        var content = Assert.IsType<MultipartFormDataContent>(stub.CapturedRequest!.Content);
        Assert.Contains("multipart/form-data", content.Headers.ContentType!.MediaType);

        var parts = content.ToList();
        bool HasField(string name) => parts.Any(p =>
            p.Headers.ContentDisposition?.Name?.Trim('"') == name);

        Assert.True(HasField("File"), "Expected a 'File' part in the multipart request.");
        Assert.True(HasField("ExpirationDate"), "Expected an 'ExpirationDate' part in the multipart request.");
        Assert.True(HasField("IssueDate"), "Expected an 'IssueDate' part in the multipart request.");

        var filePart = parts.Single(p => p.Headers.ContentDisposition?.Name?.Trim('"') == "File");
        Assert.Equal("seguro.pdf", filePart.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal("application/pdf", filePart.Headers.ContentType?.MediaType);

        Assert.Contains(
            $"VehicleDocuments/{VehicleId}/{WebVehicleDocumentType.Seguro}/document",
            stub.CapturedRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task UploadDocumentAsync_WithoutIssueDate_OmitsIssueDateField()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(stub);

        await UploadAsync(client, issueDate: null);

        var content = Assert.IsType<MultipartFormDataContent>(stub.CapturedRequest!.Content);
        var parts = content.ToList();
        Assert.DoesNotContain(parts, p => p.Headers.ContentDisposition?.Name?.Trim('"') == "IssueDate");
    }

    // ----- ValidateAsync -----

    [Fact]
    public async Task ValidateAsync_WhenNoContent_ReturnsSuccess()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(stub);

        var outcome = await client.ValidateAsync(
            VehicleId, WebVehicleDocumentType.Seguro, true, TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDocumentActionOutcome.Success>(outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.ValidateAsync(
            VehicleId, WebVehicleDocumentType.Seguro, true, TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDocumentActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.ValidateAsync(
            VehicleId, WebVehicleDocumentType.Seguro, false, TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDocumentActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenNotFound_ReturnsNotFound()
    {
        // Covers both server-side 404 causes (vehicle not found/out-of-org, or this document
        // type was never uploaded) — they are indistinguishable from the response alone.
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.ValidateAsync(
            VehicleId, WebVehicleDocumentType.TituloPropiedad, true, TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDocumentActionOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.ValidateAsync(
            VehicleId, WebVehicleDocumentType.Seguro, true, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehicleDocumentActionOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task ValidateAsync_SendsApprovedFieldAsJsonBody()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(stub);

        await client.ValidateAsync(
            VehicleId, WebVehicleDocumentType.Seguro, true, TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        var body = await stub.CapturedRequest!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = System.Text.Json.JsonDocument.Parse(body);
        Assert.True(document.RootElement.GetProperty("approved").GetBoolean());
    }

    // ----- GetExpiringCountAsync -----

    [Fact]
    public async Task GetExpiringCountAsync_WhenOk_ReturnsSuccessWithDeserializedCount()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(4)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetExpiringCountAsync(TestContext.Current.CancellationToken);

        var success = Assert.IsType<VehicleDocumentCountOutcome.Success>(outcome);
        Assert.Equal(4, success.Count);
    }

    [Fact]
    public async Task GetExpiringCountAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetExpiringCountAsync(TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDocumentCountOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetExpiringCountAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetExpiringCountAsync(TestContext.Current.CancellationToken);

        Assert.IsType<VehicleDocumentCountOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetExpiringCountAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetExpiringCountAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehicleDocumentCountOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetExpiringCountAsync_WhenBodyIsMalformed_ReturnsTransportErrorInsteadOfThrowing()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json", System.Text.Encoding.UTF8, "application/json")
        });
        var client = CreateClient(stub);

        var outcome = await client.GetExpiringCountAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<VehicleDocumentCountOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }
}
