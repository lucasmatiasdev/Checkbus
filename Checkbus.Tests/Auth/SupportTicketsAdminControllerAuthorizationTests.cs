using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Auth;

/// <summary>
/// 401/403/400 coverage for <c>api/support-tickets-admin</c> via <see cref="CheckbusApiFactory"/>.
/// Both actions require exactly CheckbusSoporte (odd/tasks/panel-soporte-checkbus.md) — unlike
/// every other controller in this API, which allows two or more roles, every other existing Role
/// (including Administrador) must get 403 here, since this is the Checkbus internal staff panel,
/// not a client-organization endpoint. This suite only proves the
/// <c>[Authorize(Roles = "CheckbusSoporte")]</c> attribute gate at the controller — the separate,
/// additional "caller's OrganizationId resolves to the sentinel Organization" check lives in the
/// handlers and is covered by GetAllSupportTicketsQueryHandlerTests/
/// UpdateSupportTicketStatusCommandHandlerTests, not here. Same raw-string convention as
/// EventsControllerAuthorizationTests/SupportTicketsControllerAuthorizationTests. Per this
/// harness's documented D-13 constraint (CheckbusApiFactory runs with a dummy, unreachable
/// Postgres connection string), the "allowed role is not rejected at the auth layer" proof for
/// UpdateStatus is driven to a 400 via an invalid enum value, mirroring
/// EventsControllerAuthorizationTests.Create_AllowedRole_IsNotRejectedAtAuthLayer. GetAll has no
/// request body/query parameters to craft a similar shortcut with, so only 401/403 are proven
/// there, same convention already established for GetAll in both sibling suites above.
/// </summary>
public class SupportTicketsAdminControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/support-tickets-admin";

    private readonly CheckbusApiFactory _factory;

    public SupportTicketsAdminControllerAuthorizationTests(CheckbusApiFactory factory)
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

    // ---- GetAll (GET) ----

    [Fact]
    public async Task GetAll_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Administrador)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Mecanico)]
    public async Task GetAll_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync(BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- UpdateStatus (PATCH {id}/status) ----

    [Fact]
    public async Task UpdateStatus_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PatchAsync(
            $"{BasePath}/{Guid.NewGuid()}/status", JsonContent.Create(new { NewStatus = 0 }), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Administrador)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Mecanico)]
    public async Task UpdateStatus_DisallowedRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.PatchAsync(
            $"{BasePath}/{Guid.NewGuid()}/status", JsonContent.Create(new { NewStatus = 0 }), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_AllowedRole_IsNotRejectedAtAuthLayer()
    {
        var client = CreateAuthorizedClient(Role.CheckbusSoporte);

        var response = await client.PatchAsync(
            $"{BasePath}/{Guid.NewGuid()}/status",
            JsonContent.Create(new { NewStatus = 999 }), // invalid SupportTicketStatus -> FluentValidation 400, never touches the DB
            TestContext.Current.CancellationToken);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
