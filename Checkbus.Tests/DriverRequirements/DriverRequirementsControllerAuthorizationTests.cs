using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.DriverRequirements;

/// <summary>
/// 401/403 coverage for <c>api/DriverRequirements</c> via <see cref="CheckbusApiFactory"/>,
/// following <c>UsersControllerAuthorizationTests</c>'s convention. Only 4 of the 5 actions
/// carry a static <c>[Authorize(Roles=...)]</c> attribute surface worth exercising here
/// (<c>expiring-count</c>) — the other 4 are <c>[Authorize]</c>-any-role with dynamic,
/// handler-level ownership/role checks, which are already covered without a real database
/// by the handler unit tests, so they are not duplicated here.
/// </summary>
public class DriverRequirementsControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/driverrequirements";

    private readonly CheckbusApiFactory _factory;

    public DriverRequirementsControllerAuthorizationTests(CheckbusApiFactory factory)
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

    [Fact]
    public async Task GetRequirements_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadDocument_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();

        var response = await client.PostAsync(
            $"{BasePath}/{Guid.NewGuid()}/LicenciaConducir/document", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidateDocument_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{Guid.NewGuid()}/LicenciaConducir/status",
            new { Approved = true },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DownloadDocument_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"{BasePath}/{Guid.NewGuid()}/LicenciaConducir/document", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetExpiringCount_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/expiring-count", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task GetExpiringCount_NonAdminRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync($"{BasePath}/expiring-count", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
