using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.VehicleDiagnostics;

/// <summary>
/// 401/403/400 coverage for <c>api/vehiclediagnostics</c> via <see cref="CheckbusApiFactory"/>.
/// Every action requires Mecanico OR Administrador (odd/tasks/vehicle-maintenance.md Constraint 6),
/// mirroring MaintenanceRecordsControllerAuthorizationTests. Per this harness's documented D-13
/// constraint (<see cref="CheckbusApiFactory"/> runs with a dummy, unreachable Postgres connection
/// string), the "allowed role is not rejected at the auth layer" proof for Create is driven to a
/// 400 via an invalid component enum value — this short-circuits inside FluentValidation's
/// ValidationBehavior, before the handler ever touches a repository. GetVehicleDiagnostics gets
/// the same proof via a malformed maintenanceRecordId query value, which fails model binding
/// before the action runs.
/// </summary>
public class VehicleDiagnosticsControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/vehiclediagnostics";

    private readonly CheckbusApiFactory _factory;

    public VehicleDiagnosticsControllerAuthorizationTests(CheckbusApiFactory factory)
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
            MaintenanceRecordId = Guid.NewGuid(),
            DiagnosedAt = new DateOnly(2026, 1, 2),
            Notes = (string?)null,
            Components = new[]
            {
                new { Component = 999, Condition = 0, Notes = (string?)null } // invalid VehicleComponent -> FluentValidation 400, never touches the DB
            }
        };

        var response = await client.PostAsJsonAsync(BasePath, body, TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- GetVehicleDiagnostics (GET, list by maintenanceRecordId) ----

    [Fact]
    public async Task GetList_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}?maintenanceRecordId={Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    public async Task GetList_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync($"{BasePath}?maintenanceRecordId={Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Mecanico)]
    [InlineData(Role.Administrador)]
    public async Task GetList_AllowedRole_IsNotRejectedAtAuthLayer(Role role)
    {
        var client = CreateAuthorizedClient(role);

        // Malformed maintenanceRecordId fails Guid model binding -> 400 via [ApiController],
        // before the action method (and the DB) is ever reached.
        var response = await client.GetAsync($"{BasePath}?maintenanceRecordId=not-a-guid", TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
