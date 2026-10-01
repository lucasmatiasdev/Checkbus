using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Contracts;
using Checkbus.ApiService.Domain.Authorization;
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

    private static string CreateToken(JwtOptions options, Role role) =>
        new JwtGenerator(options).GenerateToken(TestUserFactory.CreateUser(role));

    private static string CreateValidAdministradorToken() =>
        CreateToken(CheckbusApiFactory.CreateTestJwtOptions(), Role.Administrador);

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
        var token = CreateToken(expiredOptions, Role.Administrador);
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
        var token = CreateToken(wrongIssuerOptions, Role.Administrador);
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
        var token = CreateToken(wrongAudienceOptions, Role.Administrador);
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
        var token = CreateToken(wrongSigningKeyOptions, Role.Administrador);
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

    [Fact]
    public async Task Me_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ValidToken_Returns200WithCallersOwnIdentity()
    {
        var client = _factory.CreateClient();
        var user = TestUserFactory.CreateUser(Role.Administrador);
        var token = new JwtGenerator(CheckbusApiFactory.CreateTestJwtOptions()).GenerateToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        // Anti-IDOR proof: the response must reflect exactly the token's own claims,
        // never anything else (no client-supplied override is possible here).
        Assert.Equal(user.Id, body!.UserId);
        Assert.Equal(user.OrganizationId, body.OrganizationId);
        Assert.Equal(user.Role.ToString(), body.Role);
        Assert.Equal(user.Email, body.Email);
    }
}
