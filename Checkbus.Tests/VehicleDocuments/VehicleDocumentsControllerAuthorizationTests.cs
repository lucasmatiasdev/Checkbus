using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.VehicleDocuments;

/// <summary>
/// 401/403 coverage for <c>api/vehicledocuments</c> via <see cref="CheckbusApiFactory"/>,
/// following <c>VehiclesControllerAuthorizationTests</c>'s convention: EVERY action here is
/// Administrador-only (unlike <c>DriverRequirementsController</c>, there is no Chofer
/// self-service branch at all for vehicle documents), so every case short-circuits at the
/// authorization filter before the handler ever touches a repository.
/// </summary>
public class VehicleDocumentsControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/vehicledocuments";

    private readonly CheckbusApiFactory _factory;

    public VehicleDocumentsControllerAuthorizationTests(CheckbusApiFactory factory)
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
    public async Task GetDocuments_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task GetDocuments_NonAdminRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync($"{BasePath}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadDocument_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent();

        var response = await client.PostAsync(
            $"{BasePath}/{Guid.NewGuid()}/Seguro/document", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task UploadDocument_NonAdminRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);
        using var content = new MultipartFormDataContent();

        var response = await client.PostAsync(
            $"{BasePath}/{Guid.NewGuid()}/Seguro/document", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ValidateDocument_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{Guid.NewGuid()}/Seguro/status",
            new { Approved = true },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task ValidateDocument_NonAdminRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.PutAsJsonAsync(
            $"{BasePath}/{Guid.NewGuid()}/Seguro/status",
            new { Approved = true },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DownloadDocument_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"{BasePath}/{Guid.NewGuid()}/Seguro/document", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task DownloadDocument_NonAdminRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync(
            $"{BasePath}/{Guid.NewGuid()}/Seguro/document", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetExpiringCount_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{BasePath}/expiring-count", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task GetExpiringCount_NonAdminRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync($"{BasePath}/expiring-count", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
