using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="DriverRequirementsClient"/> maps every realistic
/// <c>api/DriverRequirements/*</c> server response (success, validation failure, 404, auth
/// failure, transport failure) into the matching closed outcome case, including that the upload
/// request is actually sent as <c>multipart/form-data</c> with the expected field names.
/// </summary>
public class DriverRequirementsClientTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static DriverRequirementsClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new DriverRequirementsClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    // ----- GetRequirementsAsync -----

    [Fact]
    public async Task GetRequirementsAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new List<DriverRequirementResponse>
        {
            new()
            {
                Type = WebDriverRequirementType.LicenciaConducir,
                Status = WebDriverRequirementStatus.Pendiente,
                IssueDate = null,
                ExpirationDate = null,
                DocumentPresent = false
            },
            new()
            {
                Type = WebDriverRequirementType.CapacitacionProfesional,
                Status = WebDriverRequirementStatus.Apto,
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

        var outcome = await client.GetRequirementsAsync(UserId, TestContext.Current.CancellationToken);

        var success = Assert.IsType<DriverRequirementsListOutcome.Success>(outcome);
        Assert.Equal(2, success.Requirements.Count);
        Assert.Equal(WebDriverRequirementType.LicenciaConducir, success.Requirements[0].Type);
        Assert.Equal(WebDriverRequirementStatus.Apto, success.Requirements[1].Status);
        Assert.True(success.Requirements[1].DocumentPresent);
    }

    [Fact]
    public async Task GetRequirementsAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetRequirementsAsync(UserId, TestContext.Current.CancellationToken);

        Assert.IsType<DriverRequirementsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetRequirementsAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetRequirementsAsync(UserId, TestContext.Current.CancellationToken);

        Assert.IsType<DriverRequirementsListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetRequirementsAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.GetRequirementsAsync(UserId, TestContext.Current.CancellationToken);

        Assert.IsType<DriverRequirementsListOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task GetRequirementsAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetRequirementsAsync(UserId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<DriverRequirementsListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetRequirementsAsync_WhenBodyIsMalformed_ReturnsTransportErrorInsteadOfThrowing()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json", System.Text.Encoding.UTF8, "application/json")
        });
        var client = CreateClient(stub);

        var outcome = await client.GetRequirementsAsync(UserId, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<DriverRequirementsListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }

    // ----- UploadDocumentAsync -----

    private static async Task<DriverRequirementActionOutcome> UploadAsync(
        DriverRequirementsClient client,
        DateOnly? issueDate = null)
    {
        using var fileStream = new MemoryStream([1, 2, 3, 4]);
        return await client.UploadDocumentAsync(
            UserId,
            WebDriverRequirementType.LicenciaConducir,
            fileStream,
            "license.pdf",
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

        Assert.IsType<DriverRequirementActionOutcome.Success>(outcome);
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

        var validationFailed = Assert.IsType<DriverRequirementActionOutcome.ValidationFailed>(outcome);
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

        Assert.IsType<DriverRequirementActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await UploadAsync(client);

        Assert.IsType<DriverRequirementActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await UploadAsync(client);

        Assert.IsType<DriverRequirementActionOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task UploadDocumentAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await UploadAsync(client);

        var transportError = Assert.IsType<DriverRequirementActionOutcome.TransportError>(outcome);
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
        Assert.Equal("license.pdf", filePart.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal("application/pdf", filePart.Headers.ContentType?.MediaType);
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
            UserId, WebDriverRequirementType.LicenciaConducir, true, TestContext.Current.CancellationToken);

        Assert.IsType<DriverRequirementActionOutcome.Success>(outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.ValidateAsync(
            UserId, WebDriverRequirementType.LicenciaConducir, true, TestContext.Current.CancellationToken);

        Assert.IsType<DriverRequirementActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.ValidateAsync(
            UserId, WebDriverRequirementType.LicenciaConducir, false, TestContext.Current.CancellationToken);

        Assert.IsType<DriverRequirementActionOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenNotFound_ReturnsNotFound()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(stub);

        var outcome = await client.ValidateAsync(
            UserId, WebDriverRequirementType.LicenciaConducir, true, TestContext.Current.CancellationToken);

        Assert.IsType<DriverRequirementActionOutcome.NotFound>(outcome);
    }

    [Fact]
    public async Task ValidateAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.ValidateAsync(
            UserId, WebDriverRequirementType.LicenciaConducir, true, TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<DriverRequirementActionOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    // ----- GetExpiringCountAsync -----

    [Fact]
    public async Task GetExpiringCountAsync_WhenOk_ReturnsSuccessWithDeserializedCount()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(7)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetExpiringCountAsync(TestContext.Current.CancellationToken);

        var success = Assert.IsType<DriverRequirementCountOutcome.Success>(outcome);
        Assert.Equal(7, success.Count);
    }

    [Fact]
    public async Task GetExpiringCountAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetExpiringCountAsync(TestContext.Current.CancellationToken);

        Assert.IsType<DriverRequirementCountOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetExpiringCountAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetExpiringCountAsync(TestContext.Current.CancellationToken);

        Assert.IsType<DriverRequirementCountOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetExpiringCountAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetExpiringCountAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<DriverRequirementCountOutcome.TransportError>(outcome);
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

        var transportError = Assert.IsType<DriverRequirementCountOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }
}
