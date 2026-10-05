using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Events;

/// <summary>
/// 401/403/400 coverage for <c>api/events</c> via <see cref="CheckbusApiFactory"/>. Every action
/// requires Planificador OR Administrador (odd/tasks/rutas-publicacion.md Constraint 6). Per this
/// harness's documented D-13 constraint (<see cref="CheckbusApiFactory"/> runs with a dummy,
/// unreachable Postgres connection string), the "allowed role is not rejected at the auth layer"
/// proof for Create is driven to a 400 via an invalid enum value — this short-circuits inside
/// FluentValidation's ValidationBehavior, before the handler ever touches a repository, mirroring
/// MaintenanceRecordsControllerAuthorizationTests. GetAll has no request body/query parameters to
/// craft a similar shortcut with, so only 401/403 are proven there — same convention already
/// established for GetMaintenanceRecord (single, by route id) in
/// MaintenanceRecordsControllerAuthorizationTests.
/// </summary>
public class EventsControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/events";

    private readonly CheckbusApiFactory _factory;

    public EventsControllerAuthorizationTests(CheckbusApiFactory factory)
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

        var response = await client.PostAsJsonAsync(BasePath, new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Mecanico)]
    public async Task Create_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.PostAsJsonAsync(BasePath, new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Administrador)]
    public async Task Create_AllowedRole_IsNotRejectedAtAuthLayer(Role role)
    {
        var client = CreateAuthorizedClient(role);
        var body = new
        {
            Name = "Final Copa Argentina",
            Type = 999, // invalid EventType -> FluentValidation 400, never touches the DB
            Date = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
            LocationName = "Estadio Mario Alberto Kempes",
            Address = "Av. Cardeñosa, Córdoba",
            PlaceId = "place-kempes-1",
            Latitude = -31.3333,
            Longitude = -64.2333
        };

        var response = await client.PostAsJsonAsync(BasePath, body, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
}
