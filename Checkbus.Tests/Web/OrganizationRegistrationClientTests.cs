using System.Net.Http.Json;
using System.Text.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Features.Organizations.Contracts;
using Checkbus.Web.Features.Organizations.Services;
using Checkbus.Web.Features.Users.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="OrganizationRegistrationClient"/> maps every realistic
/// <c>POST /api/Organizations</c> server response (success, validation failure, conflict, auth
/// failure, transport failure) into the matching <see cref="OrganizationRegistrationOutcome"/>
/// case, and that the outgoing request body serializes <see cref="WebDocumentType"/> as a plain
/// integer (no <c>JsonStringEnumConverter</c> registered anywhere in this solution). Mirrors
/// <c>UserRegistrationClientTests</c> exactly.
/// </summary>
public class OrganizationRegistrationClientTests
{
    private static RegisterOrganizationRequest CreateRequest() => new()
    {
        OrganizationName = "Acme Transportes",
        CUIT = "20-12345678-9",
        AdminName = "John",
        AdminSurname = "Doe",
        AdminDocumentType = WebDocumentType.DNI,
        AdminDocumentNumber = "12345678"
    };

    private static OrganizationRegistrationClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new OrganizationRegistrationClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    [Fact]
    public async Task RegisterAsync_WhenCreated_ReturnsSuccessWithDeserializedResult()
    {
        var expected = new RegisterOrganizationResult
        {
            OrganizationId = Guid.NewGuid(),
            OrganizationName = "Acme Transportes",
            Slug = "acme-transportes",
            AdminUserId = Guid.NewGuid(),
            AdminEmail = "john.doe@acme-transportes.com",
            AdminName = "John",
            AdminSurname = "Doe"
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var success = Assert.IsType<OrganizationRegistrationOutcome.Success>(outcome);
        Assert.Equal(expected, success.Result);
    }

    [Fact]
    public async Task RegisterAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["CUIT"] = ["CUIT must match the format NN-NNNNNNNN-N."]
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

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var validationFailed = Assert.IsType<OrganizationRegistrationOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("CUIT", error.Key);
        Assert.Equal("CUIT must match the format NN-NNNNNNNN-N.", Assert.Single(error.Value));
    }

    [Fact]
    public async Task RegisterAsync_WhenConflict_ReturnsConflictWithDetailMessage()
    {
        var conflictProblem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = "An organization with this CUIT is already registered."
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(conflictProblem)
        });
        var client = CreateClient(stub);

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<OrganizationRegistrationOutcome.Conflict>(outcome);
        Assert.Equal("An organization with this CUIT is already registered.", conflict.Message);
    }

    [Fact]
    public async Task RegisterAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<OrganizationRegistrationOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task RegisterAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<OrganizationRegistrationOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task RegisterAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<OrganizationRegistrationOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task RegisterAsync_SerializesEnumAsPlainInteger()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new RegisterOrganizationResult
            {
                OrganizationId = Guid.NewGuid(),
                OrganizationName = "Acme Transportes",
                Slug = "acme-transportes",
                AdminUserId = Guid.NewGuid(),
                AdminEmail = "john.doe@acme-transportes.com",
                AdminName = "John",
                AdminSurname = "Doe"
            })
        });
        var client = CreateClient(stub);

        await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        var body = await stub.CapturedRequest!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Number, root.GetProperty("adminDocumentType").ValueKind);
        Assert.Equal((int)WebDocumentType.DNI, root.GetProperty("adminDocumentType").GetInt32());
    }
}
