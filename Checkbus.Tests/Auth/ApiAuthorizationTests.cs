using System.Net.Http.Headers;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Auth;

public class ApiAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string RoleProbePath = "/test/role-probe";

    private readonly CheckbusApiFactory _factory;

    public ApiAuthorizationTests(CheckbusApiFactory factory)
    {
        _factory = factory;
    }

    private static string CreateToken(Role role) =>
        new JwtGenerator(CheckbusApiFactory.CreateTestJwtOptions()).GenerateToken(TestUserFactory.CreateUser(role));

    [Fact]
    public async Task RoleProbe_CorrectRole_Returns200()
    {
        var client = _factory.CreateClient();
        var token = CreateToken(Role.Administrador);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(RoleProbePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RoleProbe_WrongRole_Returns403NotUnauthorized()
    {
        var client = _factory.CreateClient();
        var token = CreateToken(Role.Chofer);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(RoleProbePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RoleProbe_NoAuthentication_Returns401NotForbidden()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(RoleProbePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
