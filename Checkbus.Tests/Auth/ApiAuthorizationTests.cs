using System.Net.Http.Headers;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Authentication.Authorization;
using Checkbus.ApiService.Domain.Entities.Tenancy;
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

    private static User CreateUser(string role) => new()
    {
        Id = Guid.NewGuid(),
        Username = "jdoe",
        Email = "jdoe@example.com",
        PasswordHash = "hashed-password",
        DocumentNumber = "12345678",
        RoleId = Guid.NewGuid(),
        Role = new Role { Id = Guid.NewGuid(), Name = role },
        OrganizationId = Guid.NewGuid(),
        Organization = new Organization
        {
            Id = Guid.NewGuid(),
            CUIT = "20-12345678-9",
            Name = "Acme",
            Slug = "acme",
            LogoUrl = "https://example.com/logo.png",
            IsActive = true
        },
        IsActive = true
    };

    private static string CreateToken(string role) =>
        new JwtGenerator(CheckbusApiFactory.CreateTestJwtOptions()).GenerateToken(CreateUser(role));

    [Fact]
    public async Task RoleProbe_CorrectRole_Returns200()
    {
        var client = _factory.CreateClient();
        var token = CreateToken(Roles.Administrador);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(RoleProbePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RoleProbe_WrongRole_Returns403NotUnauthorized()
    {
        var client = _factory.CreateClient();
        var token = CreateToken(Roles.Chofer);
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
