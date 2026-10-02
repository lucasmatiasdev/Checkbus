using System.Net;
using System.Net.Http.Json;
using Checkbus.Tests.Infrastructure;
using Checkbus.Web.Contracts;
using Checkbus.Web.Services;

namespace Checkbus.Tests.Web;

/// <summary>
/// Proves <see cref="UsersClient"/> maps every realistic <c>GET /api/Users</c> server response
/// (success, auth failure, transport failure) into the matching <see cref="UsersListOutcome"/>
/// case, including that <see cref="WebRole"/> deserializes from the plain-integer wire format
/// (no <c>JsonStringEnumConverter</c> registered anywhere in this solution).
/// </summary>
public class UsersClientTests
{
    private static UsersClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://apiservice.local/api/") };
        return new UsersClient(httpClient);
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure");
    }

    [Fact]
    public async Task GetUsersAsync_WhenOk_ReturnsSuccessWithDeserializedList()
    {
        var expected = new List<UserListItemResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "John",
                Surname = "Doe",
                Email = "jdoe@checkbus.local",
                Role = WebRole.Chofer
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Jane",
                Surname = "Smith",
                Email = "jsmith@checkbus.local",
                Role = WebRole.Administrador
            }
        };
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var client = CreateClient(stub);

        var outcome = await client.GetUsersAsync(TestContext.Current.CancellationToken);

        var success = Assert.IsType<UsersListOutcome.Success>(outcome);
        Assert.Equal(2, success.Users.Count);
        Assert.Equal("John", success.Users[0].Name);
        Assert.Equal(WebRole.Chofer, success.Users[0].Role);
        Assert.Equal(WebRole.Administrador, success.Users[1].Role);
    }

    [Fact]
    public async Task GetUsersAsync_WhenOkWithEmptyArray_ReturnsSuccessWithEmptyList()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<UserListItemResponse>())
        });
        var client = CreateClient(stub);

        var outcome = await client.GetUsersAsync(TestContext.Current.CancellationToken);

        var success = Assert.IsType<UsersListOutcome.Success>(outcome);
        Assert.Empty(success.Users);
    }

    [Fact]
    public async Task GetUsersAsync_WhenUnauthorized_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(stub);

        var outcome = await client.GetUsersAsync(TestContext.Current.CancellationToken);

        Assert.IsType<UsersListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetUsersAsync_WhenForbidden_ReturnsForbidden()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.Forbidden));
        var client = CreateClient(stub);

        var outcome = await client.GetUsersAsync(TestContext.Current.CancellationToken);

        Assert.IsType<UsersListOutcome.Forbidden>(outcome);
    }

    [Fact]
    public async Task GetUsersAsync_WhenHandlerThrows_ReturnsTransportErrorWithGenericMessage()
    {
        var client = CreateClient(new ThrowingHttpMessageHandler());

        var outcome = await client.GetUsersAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<UsersListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
        Assert.DoesNotContain("HttpRequestException", transportError.Message);
    }

    [Fact]
    public async Task GetUsersAsync_WhenBodyIsMalformed_ReturnsTransportErrorInsteadOfThrowing()
    {
        var stub = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json", System.Text.Encoding.UTF8, "application/json")
        });
        var client = CreateClient(stub);

        var outcome = await client.GetUsersAsync(TestContext.Current.CancellationToken);

        var transportError = Assert.IsType<UsersListOutcome.TransportError>(outcome);
        Assert.NotEmpty(transportError.Message);
    }
}
