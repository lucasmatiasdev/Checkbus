using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Authentication;
using Checkbus.Tests.Infrastructure;

namespace Checkbus.Tests.Vehicles;

/// <summary>
/// 401/403/400 coverage for <c>api/vehicles</c> via <see cref="CheckbusApiFactory"/>,
/// following <c>UsersControllerAuthorizationTests</c>'s convention. <c>Create</c> is
/// Administrador-only; <c>GetVehicles</c> is Mecanico-or-Administrador (needed by the
/// vehicle-maintenance module's vehicle picker). Every forbidden case short-circuits
/// before the handler ever touches a repository (401/403 at the authorization filter,
/// 400 at FluentValidation).
/// </summary>
public class VehiclesControllerAuthorizationTests : IClassFixture<CheckbusApiFactory>
{
    private const string BasePath = "/api/vehicles";

    private readonly CheckbusApiFactory _factory;

    public VehiclesControllerAuthorizationTests(CheckbusApiFactory factory)
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
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = 45,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OwnerType = VehicleOwnerType.Organizacion
    };

    [Fact]
    public async Task Create_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(BasePath, ValidBody(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    [InlineData(Role.Mecanico)]
    public async Task Create_NonAdminRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.PostAsJsonAsync(BasePath, ValidBody(), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingBrand_Returns400()
    {
        var client = CreateAuthorizedClient(Role.Administrador);
        var body = new
        {
            Brand = "",
            Model = "O500",
            Year = 2020,
            Patent = "AB123CD",
            Capacity = 45,
            Mileage = 1000,
            Status = VehicleStatus.Activo,
            OwnerType = VehicleOwnerType.Organizacion
        };

        var response = await client.PostAsJsonAsync(BasePath, body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_ChoferOwnerTypeWithoutOwnerUserId_Returns400()
    {
        var client = CreateAuthorizedClient(Role.Administrador);
        var body = new
        {
            Brand = "Mercedes-Benz",
            Model = "O500",
            Year = 2020,
            Patent = "AB123CD",
            Capacity = 45,
            Mileage = 1000,
            Status = VehicleStatus.Activo,
            OwnerType = VehicleOwnerType.Chofer
        };

        var response = await client.PostAsJsonAsync(BasePath, body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetVehicles_MissingToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(Role.Chofer)]
    [InlineData(Role.Planificador)]
    public async Task GetVehicles_NonAdminRole_Returns403(Role role)
    {
        var client = CreateAuthorizedClient(role);

        var response = await client.GetAsync(BasePath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
