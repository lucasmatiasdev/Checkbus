using System.Net;
using System.Net.Http.Headers;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Auth;

/// <summary>
/// 401/403 coverage for <c>api/supporttickets</c> via <see cref="CheckbusApiFactory"/>. Every
/// action requires Administrador OR Planificador (odd/tasks/soporte-tickets.md), same raw-string
/// <c>[Authorize(Roles = "...")]</c> convention as <c>EventsController</c>. Create accepts
/// multipart <c>[FromForm]</c> (like EventsController's UploadImage) and GetAll/GetAttachment
/// have no request body to craft a FluentValidation-shortcut 400 with (like EventsController's
/// GetAll/DownloadImage), so only 401/403 are proven here — same convention established by
/// <see cref="Checkbus.Tests.Events.EventsControllerAuthorizationTests"/>.
/// </summary>
public class SupportTicketsControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/supporttickets";

    private readonly CheckbusApiFactory _factory;

    public SupportTicketsControllerAuthorizationTests(CheckbusApiFactory factory)
    {
        _factory = factory;
    }

    private static string CreateToken(Role role) =>
        new JwtGenerator(CheckbusApiFactory.CreateTestJwtOptions()).GenerateToken(TestUserFactory.CreateUser(role));

    private HttpClient CreateAuthorizedClient(Role role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(role));
        return client;
    }

    // ---- Create (POST) ----

    [Fact]
    public async Task Create_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();

        var response = await client.PostAsync(BasePath, content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Mecanico)]
    public async Task Create_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);
        using var content = new MultipartFormDataContent();

        var response = await client.PostAsync(BasePath, content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- GetAll (GET) ----

    [Fact]
    public async Task GetAll_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Mecanico)]
    public async Task GetAll_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync(BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- GetAttachment (GET {id}/attachment) ----

    [Fact]
    public async Task GetAttachment_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/{Guid.NewGuid()}/attachment", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Mecanico)]
    public async Task GetAttachment_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync($"{BasePath}/{Guid.NewGuid()}/attachment", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
