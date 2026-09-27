using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Authentication.Authorization;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Auth;

public class ApiAuthenticationTests : IClassFixture<CheckbusApiFactory>
{
    private const string RoleProbePath = "/test/role-probe";

    private readonly CheckbusApiFactory _factory;

    public ApiAuthenticationTests(CheckbusApiFactory factory)
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

    private static string CreateToken(JwtOptions options, string role) =>
        new JwtGenerator(options).GenerateToken(CreateUser(role));

    private static string CreateValidAdministradorToken() =>
        CreateToken(CheckbusApiFactory.CreateTestJwtOptions(), Roles.Administrador);

    [Fact]
    public async Task RoleProbe_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(RoleProbePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RoleProbe_ExpiredToken_Returns401()
    {
        var client = _factory.CreateClient();
        var expiredOptions = new JwtOptions
        {
            SigningKey = CheckbusApiFactory.TestSigningKey,
            Issuer = CheckbusApiFactory.TestIssuer,
            Audience = CheckbusApiFactory.TestAudience,
            ExpirationMinutes = -10
        };
        var token = CreateToken(expiredOptions, Roles.Administrador);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(RoleProbePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RoleProbe_WrongIssuer_Returns401()
    {
        var client = _factory.CreateClient();
        var wrongIssuerOptions = new JwtOptions
        {
            SigningKey = CheckbusApiFactory.TestSigningKey,
            Issuer = "some-other-issuer",
            Audience = CheckbusApiFactory.TestAudience,
            ExpirationMinutes = CheckbusApiFactory.TestExpirationMinutes
        };
        var token = CreateToken(wrongIssuerOptions, Roles.Administrador);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(RoleProbePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RoleProbe_WrongAudience_Returns401()
    {
        var client = _factory.CreateClient();
        var wrongAudienceOptions = new JwtOptions
        {
            SigningKey = CheckbusApiFactory.TestSigningKey,
            Issuer = CheckbusApiFactory.TestIssuer,
            Audience = "some-other-audience",
            ExpirationMinutes = CheckbusApiFactory.TestExpirationMinutes
        };
        var token = CreateToken(wrongAudienceOptions, Roles.Administrador);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(RoleProbePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RoleProbe_BadSignature_Returns401()
    {
        var client = _factory.CreateClient();
        var wrongSigningKeyOptions = new JwtOptions
        {
            SigningKey = "a-completely-different-signing-key-0123456789",
            Issuer = CheckbusApiFactory.TestIssuer,
            Audience = CheckbusApiFactory.TestAudience,
            ExpirationMinutes = CheckbusApiFactory.TestExpirationMinutes
        };
        var token = CreateToken(wrongSigningKeyOptions, Roles.Administrador);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(RoleProbePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RoleProbe_ValidToken_Returns200()
    {
        var client = _factory.CreateClient();
        var token = CreateValidAdministradorToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(RoleProbePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_RemainsReachable_WithoutAnyToken()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { Email = "not-an-email", Password = "password123" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
