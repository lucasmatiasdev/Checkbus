using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Boarding;

/// <summary>
/// 401/403 coverage for <c>api/boarding</c> via <see cref="CheckbusApiFactory"/>, following
/// <see cref="Checkbus.Tests.DriverRequirements.DriverRequirementsControllerAuthorizationTests"/>'s
/// convention: every action is <c>[Authorize(Roles = "Chofer")]</c>, with dynamic
/// handler-level trip-ownership checks (already covered without a real database by the handler
/// unit tests in this folder) not duplicated here.
/// </summary>
public class BoardingControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/boarding";

    private readonly CheckbusApiFactory _factory;

    public BoardingControllerAuthorizationTests(CheckbusApiFactory factory)
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
    public async Task GetMyTrips_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/my-trips", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Administrador)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task GetMyTrips_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync($"{BasePath}/my-trips", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBoardingList_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Administrador)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task GetBoardingList_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync($"{BasePath}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Validate_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"{BasePath}/validate", new { ReservationId = Guid.NewGuid(), Method = 0 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Administrador)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task Validate_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.PostAsJsonAsync(
            $"{BasePath}/validate", new { ReservationId = Guid.NewGuid(), Method = 0 }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Close_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync($"{BasePath}/{Guid.NewGuid()}/close", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Administrador)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task Close_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.PostAsync($"{BasePath}/{Guid.NewGuid()}/close", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
