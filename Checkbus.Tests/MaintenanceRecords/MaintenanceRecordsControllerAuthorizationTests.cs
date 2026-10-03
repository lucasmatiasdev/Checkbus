using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.MaintenanceRecords;

/// <summary>
/// 401/403/400 coverage for <c>api/maintenancerecords</c> via <see cref="CheckbusApiFactory"/>.
/// Every action requires Mecanico OR Administrador (two roles together, unlike
/// VehicleDocumentsController's Administrador-only posture) — odd/tasks/vehicle-maintenance.md
/// Constraint 6. Per this harness's documented D-13 constraint (<see cref="CheckbusApiFactory"/>
/// runs with a dummy, unreachable Postgres connection string), the "allowed role is not
/// rejected at the auth layer" proof for Create/Update is driven to a 400 via an invalid enum
/// value — this short-circuits inside FluentValidation's ValidationBehavior, before the
/// handler ever touches a repository, exactly like Register_InvalidRole_Returns400 in
/// UsersControllerAuthorizationTests. GetMaintenanceRecords gets the same proof via a
/// malformed vehicleId query value, which fails model binding before the action runs.
/// GetMaintenanceRecord (single, by route id) has no such shortcut available — a valid guid
/// always binds — so only 401/403 are proven there, matching the established convention for
/// GET actions elsewhere in this test suite (e.g. UsersControllerAuthorizationTests.GetUsers).
/// </summary>
public class MaintenanceRecordsControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/maintenancerecords";

    private readonly CheckbusApiFactory _factory;

    public MaintenanceRecordsControllerAuthorizationTests(CheckbusApiFactory factory)
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
    [InlineData(Role.Planificador)]
    public async Task Create_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.PostAsJsonAsync(BasePath, new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Mecanico)]
    [InlineData(Role.Administrador)]
    public async Task Create_AllowedRole_IsNotRejectedAtAuthLayer(Role role)
    {
        var client = CreateAuthorizedClient(role);
        var body = new
        {
            VehicleId = Guid.NewGuid(),
            Type = 999, // invalid MaintenanceType -> FluentValidation 400, never touches the DB
            ScheduledDate = new DateOnly(2026, 1, 1),
            Description = "Cambio de aceite"
        };

        var response = await client.PostAsJsonAsync(BasePath, body, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Update (PUT {id}) ----

    [Fact]
    public async Task Update_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync($"{BasePath}/{Guid.NewGuid()}", new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    public async Task Update_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.PutAsJsonAsync($"{BasePath}/{Guid.NewGuid()}", new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Mecanico)]
    [InlineData(Role.Administrador)]
    public async Task Update_AllowedRole_IsNotRejectedAtAuthLayer(Role role)
    {
        var client = CreateAuthorizedClient(role);
        var body = new { Status = 999 }; // invalid MaintenanceStatus -> FluentValidation 400, never touches the DB

        var response = await client.PutAsJsonAsync($"{BasePath}/{Guid.NewGuid()}", body, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- GetMaintenanceRecords (GET, list) ----

    [Fact]
    public async Task GetList_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    public async Task GetList_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync(BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Mecanico)]
    [InlineData(Role.Administrador)]
    public async Task GetList_AllowedRole_IsNotRejectedAtAuthLayer(Role role)
    {
        var client = CreateAuthorizedClient(role);

        // Malformed vehicleId fails Guid? model binding -> 400 via [ApiController], before
        // the action method (and the DB) is ever reached.
        var response = await client.GetAsync($"{BasePath}?vehicleId=not-a-guid", TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- GetMaintenanceRecord (GET {id}, single) ----

    [Fact]
    public async Task GetSingle_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    public async Task GetSingle_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync($"{BasePath}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
