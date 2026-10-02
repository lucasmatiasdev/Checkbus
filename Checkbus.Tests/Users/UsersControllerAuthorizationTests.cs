using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Users;

/// <summary>
/// 401/403/400 coverage for <c>POST api/users</c> via <see cref="CheckbusApiFactory"/>.
/// Per the design's D-13, this harness runs with a dummy connection string and never
/// reaches a database, so the 201-happy-path proof is deliberately NOT here — it is the
/// single Aspire end-to-end scenario reserved for Phase 5 (Final Verification), which is
/// out of scope for this slice. Every case below short-circuits before the handler ever
/// touches a repository: 401/403 at the authorization filter, 400 at FluentValidation.
/// </summary>
public class UsersControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string RegisterPath = "/api/users";
    private const string ListPath = "/api/users";

    private readonly CheckbusApiFactory _factory;

    public UsersControllerAuthorizationTests(CheckbusApiFactory factory)
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

    private static object ValidBody() => new
    {
        Name = "Jose",
        Surname = "Diaz",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "40123456",
        Role = Role.Chofer
    };

    [Fact]
    public async Task Register_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RegisterPath, ValidBody(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task Register_NonAdminRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.PostAsJsonAsync(RegisterPath, ValidBody(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingSurname_Returns400()
    {
        var client = CreateAuthorizedClient(Role.Administrador);
        var body = new { Name = "Jose", Surname = "", DocumentType = DocumentType.DNI, DocumentNumber = "40123456", Role = Role.Chofer };

        var response = await client.PostAsJsonAsync(RegisterPath, body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingDocumentNumber_Returns400()
    {
        var client = CreateAuthorizedClient(Role.Administrador);
        var body = new { Name = "Jose", Surname = "Diaz", DocumentType = DocumentType.DNI, DocumentNumber = "", Role = Role.Chofer };

        var response = await client.PostAsJsonAsync(RegisterPath, body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidDocumentType_Returns400()
    {
        var client = CreateAuthorizedClient(Role.Administrador);
        var body = new { Name = "Jose", Surname = "Diaz", DocumentType = (DocumentType)999, DocumentNumber = "40123456", Role = Role.Chofer };

        var response = await client.PostAsJsonAsync(RegisterPath, body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = CreateAuthorizedClient(Role.Administrador);
        var body = new { Name = "Jose", Surname = "Diaz", DocumentType = DocumentType.DNI, DocumentNumber = "40123456", Role = (Role)999 };

        var response = await client.PostAsJsonAsync(RegisterPath, body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingNameWithExtraOrganizationIdProperty_Returns400()
    {
        // RegisterUserCommand has no OrganizationId field, so model binding silently
        // ignores the extra body property below rather than erroring on it; this request
        // still 400s on the invalid Name, proving the body binds against the real contract
        // and an attacker-supplied organizationId has no path into the command at all.
        var client = CreateAuthorizedClient(Role.Administrador);
        var body = new
        {
            Name = "",
            Surname = "Diaz",
            DocumentType = DocumentType.DNI,
            DocumentNumber = "40123456",
            Role = Role.Chofer,
            OrganizationId = Guid.NewGuid()
        };

        var response = await client.PostAsJsonAsync(RegisterPath, body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(ListPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task GetUsers_NonAdminRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync(ListPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
