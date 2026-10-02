using System.Net.Http.Json;
using System.Text.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="UserRegistrationClient"/> maps every realistic <c>POST /api/Users</c>
/// server response (success, validation failure, conflict, auth failure, transport failure) into
/// the matching <see cref="UserRegistrationOutcome"/> case, and that the outgoing request body
/// serializes <see cref="WebRole"/>/<see cref="WebDocumentType"/> as plain integers (no
/// <c>JsonStringEnumConverter</c> registered anywhere in this solution).
/// </summary>
public class UserRegistrationClientTests
{
    private static RegisterUserRequest CreateRequest() => new()
    {
        Name = "John",
        Surname = "Doe",
        DocumentType = WebDocumentType.Pasaporte,
        DocumentNumber = "X1234567",
        Role = WebRole.Chofer
    };

    private static UserRegistrationClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new UserRegistrationClient(httpClient);
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
        var expected = new RegisterUserResult
        {
            UserId = Guid.NewGuid(),
            Email = "jdoe@checkbus.local",
            Name = "John",
            Surname = "Doe",
            Role = "Chofer",
            OrganizationId = Guid.NewGuid(),
            MustChangePassword = true
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var success = Assert.IsType<UserRegistrationOutcome.Success>(outcome);
        Assert.Equal(expected, success.Result);
    }

    [Fact]
    public async Task RegisterAsync_WhenBadRequest_ReturnsValidationFailedWithErrors()
    {
        var validationProblem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["DocumentNumber"] = ["'Document Number' must not be empty."]
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

        var validationFailed = Assert.IsType<UserRegistrationOutcome.ValidationFailed>(outcome);
        var error = Assert.Single(validationFailed.Errors);
        Assert.Equal("DocumentNumber", error.Key);
        Assert.Equal("'Document Number' must not be empty.", Assert.Single(error.Value));
    }

    [Fact]
    public async Task RegisterAsync_WhenConflict_ReturnsConflictWithDetailMessage()
    {
        var conflictProblem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = "A user with this document number is already registered."
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(conflictProblem)
        });
        var client = CreateClient(stub);

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var conflict = Assert.IsType<UserRegistrationOutcome.Conflict>(outcome);
        Assert.Equal("A user with this document number is already registered.", conflict.Message);
    }

    [Fact]
    public async Task RegisterAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<UserRegistrationOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task RegisterAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.IsType<UserRegistrationOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task RegisterAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<UserRegistrationOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task RegisterAsync_SerializesEnumsAsPlainIntegers()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new RegisterUserResult
            {
                UserId = Guid.NewGuid(),
                Email = "jdoe@checkbus.local",
                Name = "John",
                Surname = "Doe",
                Role = "Chofer",
                OrganizationId = Guid.NewGuid(),
                MustChangePassword = false
            })
        });
        var client = CreateClient(stub);

        await client.RegisterAsync(CreateRequest(), TestContext.Current.CancellationToken);

        Assert.NotNull(stub.CapturedRequest);
        var body = await stub.CapturedRequest!.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Number, root.GetProperty("documentType").ValueKind);
        Assert.Equal((int)WebDocumentType.Pasaporte, root.GetProperty("documentType").GetInt32());
        Assert.Equal(JsonValueKind.Number, root.GetProperty("role").ValueKind);
        Assert.Equal((int)WebRole.Chofer, root.GetProperty("role").GetInt32());
    }
}
